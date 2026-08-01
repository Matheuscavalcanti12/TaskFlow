using System.Net.Mail;
using System.Security.Claims;
using Models;
using MySql.Data.MySqlClient;
using JwtService = JWT.JWT;

namespace Rotas;

public static class Cadastro
{
    public static void cadastro(this WebApplication app)
    {
        app.MapPost("/cadastro", (UsuarioCadastroRequest usuario) =>
        {
            if (string.IsNullOrWhiteSpace(usuario.nome) ||
                string.IsNullOrWhiteSpace(usuario.email) ||
                string.IsNullOrWhiteSpace(usuario.senha))
            {
                return Results.BadRequest(new { erro = "Nome, email e senha sao obrigatorios." });
            }

            var nome = usuario.nome.Trim();
            var email = usuario.email.Trim().ToLowerInvariant();

            if (nome.Length is < 3 or > 100)
            {
                return Results.BadRequest(new { erro = "Nome deve possuir entre 3 e 100 caracteres." });
            }

            if (email.Length > 100 || !MailAddress.TryCreate(email, out _))
            {
                return Results.BadRequest(new { erro = "Email invalido." });
            }

            if (usuario.senha.Length is < 8 or > 72)
            {
                return Results.BadRequest(new { erro = "Senha deve possuir entre 8 e 72 caracteres." });
            }

            var senhaHash = BCrypt.Net.BCrypt.HashPassword(usuario.senha);

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                const string sql = """
                    INSERT INTO usuarios (nome, email, senha_hash)
                    VALUES (@nome, @email, @senha_hash);
                    SELECT LAST_INSERT_ID();
                    """;

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@nome", nome);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@senha_hash", senhaHash);

                var usuarioId = Convert.ToInt32(cmd.ExecuteScalar());
                var token = JwtService.GenerateToken(usuarioId, email, "user");

                return Results.Ok(new { token, usuarioId });
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict(new { erro = "Email ja cadastrado." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO CADASTRO: " + ex.Message);
                return Results.Problem("Erro ao cadastrar usuario.");
            }
        })
        .RequireRateLimiting("cadastro");
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

public static class Login
{
    public static void login(this WebApplication app)
    {
        app.MapPost("/login", (UsuarioLoginRequest usuario) =>
        {
            if (string.IsNullOrWhiteSpace(usuario.email) ||
                string.IsNullOrWhiteSpace(usuario.senha))
            {
                return Results.BadRequest(new { erro = "Email e senha sao obrigatorios." });
            }

            var email = usuario.email.Trim().ToLowerInvariant();

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                const string sql = """
                    SELECT id_usuario, senha_hash
                    FROM usuarios
                    WHERE email = @email
                    LIMIT 1;
                    """;

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@email", email);
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return Results.Unauthorized();
                }

                var senhaHash = reader.GetString("senha_hash");
                if (!BCrypt.Net.BCrypt.Verify(usuario.senha, senhaHash))
                {
                    return Results.Unauthorized();
                }

                var usuarioId = reader.GetInt32("id_usuario");
                var token = JwtService.GenerateToken(usuarioId, email, "user");

                return Results.Ok(new { token, usuarioId });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO LOGIN: " + ex.Message);
                return Results.Problem("Erro ao realizar login.");
            }
        })
        .RequireRateLimiting("login");
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

public static class Perfil
{
    public static void perfil(this WebApplication app)
    {
        app.MapGet("/perfil", (ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                const string sql = """
                    SELECT id_usuario, nome, email, criado_em
                    FROM usuarios
                    WHERE id_usuario = @usuarioId
                    LIMIT 1;
                    """;

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return Results.NotFound(new { erro = "Usuario nao encontrado." });
                }

                var perfil = new UsuarioPerfilResponse
                {
                    id = reader.GetInt32("id_usuario"),
                    nome = reader.GetString("nome"),
                    email = reader.GetString("email"),
                    criadoEm = reader.GetDateTime("criado_em")
                };

                return Results.Ok(perfil);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO PERFIL: " + ex.Message);
                return Results.Problem("Erro ao consultar perfil.");
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

public static class CadastroAtividade
{
    public static void cadastroAtividade(this WebApplication app)
    {
        app.MapPost("/criandoAtividade", (CriarAtividadeRequest atividade, ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(atividade.titulo) ||
                string.IsNullOrWhiteSpace(atividade.prioridade))
            {
                return Results.BadRequest(new { erro = "Titulo e prioridade sao obrigatorios." });
            }

            var titulo = atividade.titulo.Trim();
            var descricao = atividade.descricao?.Trim();
            var prioridade = atividade.prioridade.Trim().ToLowerInvariant();

            if (titulo.Length is < 3 or > 100)
            {
                return Results.BadRequest(new { erro = "Titulo deve possuir entre 3 e 100 caracteres." });
            }

            if (descricao?.Length > 1000)
            {
                return Results.BadRequest(new { erro = "Descricao deve possuir no maximo 1000 caracteres." });
            }

            if (prioridade is not ("baixa" or "media" or "alta" or "urgente"))
            {
                return Results.BadRequest(new { erro = "Prioridade deve ser baixa, media, alta ou urgente." });
            }

            if (atividade.prazo.HasValue && atividade.prazo.Value.Date < DateTime.Today)
            {
                return Results.BadRequest(new { erro = "Prazo nao pode estar no passado." });
            }

            try
            {
                using var conn = CreateConnection();
                conn.Open();

                if (atividade.categoriaId.HasValue)
                {
                    const string categoriaSql = """
                        SELECT id_categorias
                        FROM categorias
                        WHERE id_categorias = @categoriaId
                          AND usuario_id = @usuarioId
                        LIMIT 1;
                        """;

                    using var categoriaCmd = new MySqlCommand(categoriaSql, conn);
                    categoriaCmd.Parameters.AddWithValue("@categoriaId", atividade.categoriaId.Value);
                    categoriaCmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                    if (categoriaCmd.ExecuteScalar() is null)
                    {
                        return Results.BadRequest(new { erro = "Categoria nao encontrada para este usuario." });
                    }
                }

                const string sql = """
                    INSERT INTO atividades (titulo, descricao, status, prioridade, prazo, usuario_id, categoria_id)
                    VALUES (@titulo, @descricao, @status, @prioridade, @prazo, @usuarioId, @categoriaId);
                    SELECT LAST_INSERT_ID();
                    """;

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@titulo", titulo);
                cmd.Parameters.AddWithValue("@descricao", string.IsNullOrWhiteSpace(descricao) ? DBNull.Value : descricao);
                cmd.Parameters.AddWithValue("@status", "pendente");
                cmd.Parameters.AddWithValue("@prioridade", prioridade);
                cmd.Parameters.AddWithValue("@prazo", atividade.prazo.HasValue ? atividade.prazo.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);
                cmd.Parameters.AddWithValue("@categoriaId", atividade.categoriaId.HasValue ? atividade.categoriaId.Value : DBNull.Value);

                var atividadeId = Convert.ToInt32(cmd.ExecuteScalar());

                return Results.Ok(new { atividadeId });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO CADASTRO ATIVIDADE: " + ex.Message);
                return Results.Problem("Erro ao criar atividade.");
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

public static class Listar
{
    public static void listarAtividades(this WebApplication app)
    {
        app.MapGet("/ListarAtividades", (ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                const string sql = """
                    SELECT
                        id,
                        titulo,
                        descricao,
                        status,
                        prioridade,
                        prazo,
                        criada_em,
                        concluida_em,
                        categoria_id
                    FROM atividades
                    WHERE usuario_id = @usuarioId
                    ORDER BY criada_em DESC;
                    """;

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                using var reader = cmd.ExecuteReader();
                //retornar varias atividades 
                var atividades = new List<ListarAtividades>();

                while (reader.Read())
                {
                    atividades.Add(new ListarAtividades
                    {
                        id = reader.GetInt32("id"),
                        titulo = reader.GetString("titulo"),
                        descricao = reader.IsDBNull(reader.GetOrdinal("descricao"))
                            ? null
                            : reader.GetString("descricao"),
                        status = reader.GetString("status"),
                        prioridade = reader.GetString("prioridade"),
                        prazo = reader.IsDBNull(reader.GetOrdinal("prazo"))
                            ? null
                            : reader.GetDateTime("prazo"),
                        criadaEm = reader.GetDateTime("criada_em"),
                        concluidaEm = reader.IsDBNull(reader.GetOrdinal("concluida_em"))
                            ? null
                            : reader.GetDateTime("concluida_em"),
                        categoriaId = reader.IsDBNull(reader.GetOrdinal("categoria_id"))
                            ? null
                            : reader.GetInt32("categoria_id")
                    });
                }

                return Results.Ok(atividades);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO LISTAR ATIVIDADES: " + ex.Message);
                return Results.Problem("Erro ao listar atividades.");
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

public static class BuscarAtividades
{
    public static void buscarAtividade(this WebApplication app)
    {
        app.MapGet("/BuscandoAtividade/{id:int}", (int id, ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                 const string sql = """
                        SELECT
                            id,
                            titulo,
                            descricao,
                            status,
                            prioridade,
                            prazo,
                            criada_em,
                            atualizada_em,
                            concluida_em,
                            categoria_id,
                            usuario_id
                        FROM atividades
                        WHERE id = @id
                        AND usuario_id = @usuarioId
                        LIMIT 1;
                        """;

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return Results.NotFound(new { erro = "Atividade nao encontrada." });
                }

                var atividade = new BuscarAtividade
                {
                    id = reader.GetInt32("id"),
                    titulo = reader.GetString("titulo"),
                    descricao = reader.IsDBNull(reader.GetOrdinal("descricao"))
                        ? null
                        : reader.GetString("descricao"),

                    status = reader.GetString("status"),

                    prioridade = reader.GetString("prioridade"),

                    prazo = reader.IsDBNull(reader.GetOrdinal("prazo"))
                        ? null
                        : reader.GetDateTime("prazo"),

                    criadaEm = reader.GetDateTime("criada_em"),

                    atualizadaEm = reader.GetDateTime("atualizada_em"),

                    concluidaEm = reader.IsDBNull(reader.GetOrdinal("concluida_em"))
                        ? null
                        : reader.GetDateTime("concluida_em"),

                    categoriaId = reader.IsDBNull(reader.GetOrdinal("categoria_id"))
                        ? null
                        : reader.GetInt32("categoria_id"),

                    usuarioId = reader.GetInt32("usuario_id")
                };

                return Results.Ok(atividade);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO BUSCAR ATIVIDADE: " + ex.Message);
                return Results.Problem("Erro ao consultar atividade.");
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

public static class AtualizarAtividade
{
    public static void atualizarAtividade(this WebApplication app)
    {
        app.MapPut("/AtualizarAtividade/{id:int}", (int id, AtualizarAtividadeRequest atividade, ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(atividade.titulo) ||
                string.IsNullOrWhiteSpace(atividade.status) ||
                string.IsNullOrWhiteSpace(atividade.prioridade))
            {
                return Results.BadRequest(new { erro = "Titulo, status e prioridade sao obrigatorios." });
            }

            var titulo = atividade.titulo.Trim();
            var descricao = atividade.descricao?.Trim();
            var status = atividade.status.Trim().ToLowerInvariant();
            var prioridade = atividade.prioridade.Trim().ToLowerInvariant();

            if (titulo.Length is < 3 or > 100)
            {
                return Results.BadRequest(new { erro = "Titulo deve possuir entre 3 e 100 caracteres." });
            }

            if (descricao?.Length > 1000)
            {
                return Results.BadRequest(new { erro = "Descricao deve possuir no maximo 1000 caracteres." });
            }

            if (status is not ("pendente" or "em andamento" or "concluida" or "cancelada"))
            {
                return Results.BadRequest(new { erro = "Status deve ser pendente, em andamento, concluida ou cancelada." });
            }

            if (prioridade is not ("baixa" or "media" or "alta" or "urgente"))
            {
                return Results.BadRequest(new { erro = "Prioridade deve ser baixa, media, alta ou urgente." });
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                if (atividade.categoriaId.HasValue)
                {
                    const string categoriaSql = """
                        SELECT id_categorias
                        FROM categorias
                        WHERE id_categorias = @categoriaId
                          AND usuario_id = @usuarioId
                        LIMIT 1;
                        """;

                    using var categoriaCmd = new MySqlCommand(categoriaSql, connection);
                    categoriaCmd.Parameters.AddWithValue("@categoriaId", atividade.categoriaId.Value);
                    categoriaCmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                    if (categoriaCmd.ExecuteScalar() is null)
                    {
                        return Results.BadRequest(new { erro = "Categoria nao encontrada para este usuario." });
                    }
                }

                const string sql = """
                    UPDATE atividades
                    SET titulo = @titulo,
                        descricao = @descricao,
                        status = @status,
                        prioridade = @prioridade,
                        prazo = @prazo,
                        categoria_id = @categoriaId,
                        concluida_em =
                            CASE
                                WHEN @status = 'concluida'
                                THEN COALESCE(concluida_em, CURRENT_TIMESTAMP)
                                ELSE NULL
                            END
                    WHERE id = @id
                      AND usuario_id = @usuarioId;
                    """;

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@titulo", titulo);
                cmd.Parameters.AddWithValue("@descricao", string.IsNullOrWhiteSpace(descricao) ? DBNull.Value : descricao);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@prioridade", prioridade);
                cmd.Parameters.AddWithValue("@prazo", atividade.prazo.HasValue ? atividade.prazo.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@categoriaId", atividade.categoriaId.HasValue ? atividade.categoriaId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                var rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return Results.NotFound(new { erro = $"Atividade com id {id} nao encontrada." });
                }

                return Results.Ok(new { mensagem = "Atividade atualizada com sucesso." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO ATUALIZAR ATIVIDADE: " + ex.Message);
                return Results.Problem("Erro ao atualizar atividade.");
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

public static class RemoverAtividade
{
    public static void removerAtividade(this WebApplication app)
    {
        app.MapPut("/RemoverAtividade/{id:int}", (int id, ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                const string sql = """
                    UPDATE atividades
                    SET status = @status
                    WHERE id = @id
                      AND usuario_id = @usuarioId;
                    """;

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@status", "cancelada");
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                var rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return Results.NotFound(new { erro = $"Atividade com id {id} nao encontrada." });
                }

                return Results.Ok(new { mensagem = "Atividade removida da visualizacao com sucesso." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO REMOVER ATIVIDADE: " + ex.Message);
                return Results.Problem("Erro ao remover atividade.");
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}
public static class AtualizarStatus
{
    public static void atualizarStatus(this WebApplication app)
    {
        app.MapPut("/AtualizarStatus/{id:int}",
        (int id,
        AtualizarStatusRequest request,
        ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.status))
            {
                return Results.BadRequest(new
                {
                    erro = "Status é obrigatório."
                });
            }

            var status = request.status.Trim().ToLowerInvariant();

            if (status is not ("pendente"
                or "em andamento"
                or "concluida"
                or "cancelada"))
            {
                return Results.BadRequest(new
                {
                    erro = "Status inválido."
                });
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                const string sql = """
                    UPDATE atividades
                    SET
                        status = @status,
                        concluida_em =
                            CASE
                                WHEN @status = 'concluida'
                                THEN CURRENT_TIMESTAMP
                                ELSE NULL
                            END
                    WHERE id = @id
                      AND usuario_id = @usuarioId;
                    """;

                using var cmd = new MySqlCommand(sql, connection);

                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                var linhas = cmd.ExecuteNonQuery();

                if (linhas == 0)
                {
                    return Results.NotFound(new
                    {
                        erro = "Atividade não encontrada."
                    });
                }

                return Results.Ok(new
                {
                    mensagem = "Status atualizado com sucesso."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO ATUALIZAR STATUS: " + ex.Message);

                return Results.Problem("Erro ao atualizar status.");
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection()
    {
        return new MySqlConnection(
            "server=localhost;database=TaskFlow;user=root;password=;");
    }
}
//endpoints de categorias, Ex: Trablho,estudos 
public static class Categoria
{
    public static void Categorias(this WebApplication app)
    {
        app.MapPost("/categorias", (CategoriasCriadas categorias, ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(categorias.nome))
            {
                return Results.BadRequest(new
                {
                    mensagem = "Nome da categoria é obrigatório."
                });
            }

            var nome = categorias.nome.Trim();
            var cor = string.IsNullOrWhiteSpace(categorias.cor)
                ? "#6366f1"
                : categorias.cor.Trim();

            if (nome.Length is < 2 or > 100)
            {
                return Results.BadRequest(new
                {
                    mensagem = "Nome da categoria deve possuir entre 2 e 100 caracteres."
                });
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                const string sql = """
                    INSERT INTO categorias (nome, cor, usuario_id)
                    VALUES (@nome, @cor, @usuarioId);
                    SELECT LAST_INSERT_ID();
                    """;

                using var cmd = new MySqlCommand(sql, connection);

                cmd.Parameters.AddWithValue("@nome", nome);
                cmd.Parameters.AddWithValue("@cor", cor);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                var categoriaId = Convert.ToInt32(cmd.ExecuteScalar());

                return Results.Ok(new
                {
                    categoriaId,
                    mensagem = "Categoria cadastrada com sucesso."
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new
                {
                    mensagem = ex.Message
                });
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

public static class ListarCategorias
{
    public static void listarCategorias(this WebApplication app)
    {
        app.MapGet("/categorias", (ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                const string sql = """
                    SELECT id_categorias, nome, cor, usuario_id
                    FROM categorias
                    WHERE usuario_id = @usuarioId
                    ORDER BY nome;
                    """;

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                using var reader = cmd.ExecuteReader();
                var categorias = new List<object>();

                while (reader.Read())
                {
                    categorias.Add(new
                    {
                        id = reader.GetInt32("id_categorias"),
                        nome = reader.GetString("nome"),
                        cor = reader.GetString("cor"),
                        usuarioId = reader.GetInt32("usuario_id")
                    });
                }

                return Results.Ok(categorias);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new
                {
                    mensagem = ex.Message
                });
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}


public static class BuscarCategorias
{
    public static void BuscarCategoria(this WebApplication app)
    {
        app.MapGet("/buscarCategorias/{id_categorias:int}",
        (int id_categorias, ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                const string sql = """
                    SELECT id_categorias, nome, cor, usuario_id
                    FROM categorias
                    WHERE id_categorias = @id_categorias
                    AND usuario_id = @usuarioId;
                    """;

                using var cmd = new MySqlCommand(sql, connection);

                cmd.Parameters.AddWithValue("@id_categorias", id_categorias);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return Results.NotFound(new
                    {
                        mensagem = "Categoria não encontrada."
                    });
                }

                return Results.Ok(new
                {
                    id = reader.GetInt32("id_categorias"),
                    nome = reader.GetString("nome"),
                    cor = reader.GetString("cor"),
                    usuarioId = reader.GetInt32("usuario_id")
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new
                {
                    mensagem = ex.Message
                });
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

public static class AtualizarCategoria
{
    public static void AtualizarCategorias(this WebApplication app)
    {
        app.MapPut("/categorias/{id_categorias:int}", (int id_categorias, CategoriasCriadas categorias, ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(categorias.nome))
            {
                return Results.BadRequest(new
                {
                    mensagem = "Nome da categoria é obrigatório."
                });
            }

            var nome = categorias.nome.Trim();
            var cor = string.IsNullOrWhiteSpace(categorias.cor)
                ? "#6366f1"
                : categorias.cor.Trim();

            if (nome.Length is < 2 or > 100)
            {
                return Results.BadRequest(new
                {
                    mensagem = "Nome da categoria deve possuir entre 2 e 100 caracteres."
                });
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                const string sql = """
                    UPDATE categorias
                    SET nome = @nome,
                        cor = @cor
                    WHERE id_categorias = @id_categorias
                    AND usuario_id = @usuarioId;
                    """;

                using var cmd = new MySqlCommand(sql, connection);

                cmd.Parameters.AddWithValue("@nome", nome);
                cmd.Parameters.AddWithValue("@cor", cor);
                cmd.Parameters.AddWithValue("@id_categorias", id_categorias);
                cmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                int linhasAfetadas = cmd.ExecuteNonQuery();

                if (linhasAfetadas == 0)
                {
                    return Results.NotFound(new
                    {
                        mensagem = "Categoria não encontrada."
                    });
                }

                return Results.Ok(new
                {
                    mensagem = "Categoria atualizada com sucesso."
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new
                {
                    mensagem = ex.Message
                });
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

public static class RemoverCategoria
{
    public static void RemoverCategorias(this WebApplication app)
    {
        app.MapDelete("/categorias/{id_categorias:int}", (int id_categorias, string? opcao, int? destinoId, ClaimsPrincipal usuarioAutenticado) =>
        {
            var idClaim = usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idClaim, out var usuarioId))
            {
                return Results.Unauthorized();
            }

            var opcaoEscolhida = string.IsNullOrWhiteSpace(opcao)
                ? "remover"
                : opcao.Trim().ToLowerInvariant();

            if (opcaoEscolhida is not ("remover" or "mover"))
            {
                return Results.BadRequest(new
                {
                    mensagem = "Opcao deve ser remover ou mover."
                });
            }

            if (opcaoEscolhida == "mover" && !destinoId.HasValue)
            {
                return Results.BadRequest(new
                {
                    mensagem = "Informe a categoria de destino."
                });
            }

            if (opcaoEscolhida == "mover" && destinoId == id_categorias)
            {
                return Results.BadRequest(new
                {
                    mensagem = "Categoria de destino deve ser diferente da categoria removida."
                });
            }

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                using var transaction = connection.BeginTransaction();

                const string categoriaSql = """
                    SELECT id_categorias
                    FROM categorias
                    WHERE id_categorias = @id_categorias
                    AND usuario_id = @usuarioId;
                    """;

                using var categoriaCmd = new MySqlCommand(categoriaSql, connection, transaction);
                categoriaCmd.Parameters.AddWithValue("@id_categorias", id_categorias);
                categoriaCmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                if (categoriaCmd.ExecuteScalar() is null)
                {
                    transaction.Rollback();
                    return Results.NotFound(new
                    {
                        mensagem = "Categoria não encontrada."
                    });
                }

                if (opcaoEscolhida == "mover")
                {
                    using var destinoCmd = new MySqlCommand(categoriaSql, connection, transaction);
                    destinoCmd.Parameters.AddWithValue("@id_categorias", destinoId!.Value);
                    destinoCmd.Parameters.AddWithValue("@usuarioId", usuarioId);

                    if (destinoCmd.ExecuteScalar() is null)
                    {
                        transaction.Rollback();
                        return Results.BadRequest(new
                        {
                            mensagem = "Categoria de destino não encontrada."
                        });
                    }
                }

                const string atualizarAtividadesSql = """
                    UPDATE atividades
                    SET categoria_id = @destinoId
                    WHERE categoria_id = @id_categorias
                    AND usuario_id = @usuarioId;
                    """;

                using var atualizarCmd = new MySqlCommand(atualizarAtividadesSql, connection, transaction);
                atualizarCmd.Parameters.AddWithValue("@destinoId", opcaoEscolhida == "mover" ? destinoId!.Value : DBNull.Value);
                atualizarCmd.Parameters.AddWithValue("@id_categorias", id_categorias);
                atualizarCmd.Parameters.AddWithValue("@usuarioId", usuarioId);
                atualizarCmd.ExecuteNonQuery();

                const string removerSql = """
                    DELETE FROM categorias
                    WHERE id_categorias = @id_categorias
                    AND usuario_id = @usuarioId;
                    """;

                using var removerCmd = new MySqlCommand(removerSql, connection, transaction);
                removerCmd.Parameters.AddWithValue("@id_categorias", id_categorias);
                removerCmd.Parameters.AddWithValue("@usuarioId", usuarioId);
                removerCmd.ExecuteNonQuery();

                transaction.Commit();

                return Results.Ok(new
                {
                    mensagem = "Categoria removida com sucesso."
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new
                {
                    mensagem = ex.Message
                });
            }
        })
        .RequireAuthorization();
    }

    private static MySqlConnection CreateConnection() =>
        new("server=localhost;database=TaskFlow;user=root;password=;");
}

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

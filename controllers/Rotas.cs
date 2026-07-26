using MySql.Data.MySqlClient;
using Models;
using JwtService = JWT.JWT;


namespace Rotas;

//3 endpoints de autenticação

public static class Cadastro {
    public static void cadastro(this WebApplication app)
    {
        app.MapPost("/cadastro", (UsuarioCadastroRequest usuario) =>
        {
            //A classe Usuarios representa o usuário como ele existe no banco
            //Já UsuarioCadastroRequest representa o que o cliente envia no cadastro:
            if (string.IsNullOrWhiteSpace(usuario.nome) ||
                string.IsNullOrWhiteSpace(usuario.email) ||
                string.IsNullOrWhiteSpace(usuario.senha))
            {
                return Results.BadRequest(new { erro = "Nome, email e senha sao obrigatorios." });
            }

            var nome = usuario.nome.Trim();
            var email = usuario.email.Trim().ToLowerInvariant();
            var senhaHash = BCrypt.Net.BCrypt.HashPassword(usuario.senha);

            try
            {
                using var connection = new MySqlConnection("server=localhost;database=TaskFlow;user=root;password=;");
                connection.Open();

                string sql = """
                    INSERT INTO usuarios (nome, email, senha_hash)
                    VALUES (@nome, @email, @senha_hash);
                    SELECT LAST_INSERT_ID();
                    """;
                using var cmd = new MySqlCommand(sql, connection);

                cmd.Parameters.AddWithValue("@nome", nome);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@senha_hash", senhaHash);

                var usuarioId = Convert.ToInt32(cmd.ExecuteScalar());
                var token = JwtService.GenerateToken(email, "user");

                return Results.Ok(new { token, usuarioId });
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict(new { erro = "Email ja cadastrado." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO CADASTRO: " + ex.Message);
                return Results.Problem("Erro ao cadastrar usuario");
            }
        });
    }
}

namespace Models;

public class UsuarioCadastroRequest
{
    public string nome { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string senha { get; set; } = string.Empty;
}

public class UsuarioLoginRequest
{
    public string email { get; set; } = string.Empty;
    public string senha { get; set; } = string.Empty;
}

public class UsuarioPerfilResponse
{
    public int id { get; set; }
    public string nome { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public DateTime criadoEm { get; set; }
}

public class CriarAtividadeRequest
{
    public string titulo { get; set; }
    public string? descricao { get; set; }
    public string prioridade { get; set; }
    public DateTime? prazo { get; set; }
    public int? categoriaId { get; set; }
}

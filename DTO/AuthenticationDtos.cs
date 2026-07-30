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
    public string titulo { get; set; } = string.Empty;
    public string? descricao { get; set; }
    public string prioridade { get; set; } = string.Empty;
    public DateTime? prazo { get; set; }
    public int? categoriaId { get; set; }
}

public class BuscarAtividade
{
    public int id { get; set; }
    public string titulo { get; set; } = string.Empty;
    public string? descricao { get; set; }
    public string status { get; set; } = string.Empty;
    public string prioridade { get; set; } = string.Empty;
    public DateTime? prazo { get; set; }
    public DateTime criadaEm { get; set; }
    public DateTime? concluidaEm { get; set; }
    public int? categoriaId { get; set; }
}

public class AtualizarAtividadeRequest
{
    public string titulo { get; set; } = string.Empty;
    public string? descricao { get; set; }
    public string status { get; set; } = string.Empty;
    public string prioridade { get; set; } = string.Empty;
}

public class CategoriasCriadas
{
    public string nome {get; set; } = string.Empty;
}
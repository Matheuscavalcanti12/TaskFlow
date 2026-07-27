namespace Models;

public class Usuario
{
    public int id_usuario { get; set; }
    public string nome { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string senha_hash { get; set; } = string.Empty;
    public DateTime criado_em { get; set; }
}

public class Categoria
{
    public int id_categoria { get; set; }
    public string nome { get; set; } = string.Empty;
    public int usuario_id { get; set; }
}

public class Atividade
{
    public int id { get; set; }
    public string titulo { get; set; } = string.Empty;
    public string? descricao { get; set; }
    public string status { get; set; } = string.Empty;
    public string prioridade { get; set; } = string.Empty;
    public DateTime? prazo { get; set; }
    public DateTime criada_em { get; set; }
    public DateTime? concluida_em { get; set; }
    public int usuario_id { get; set; }
    public int? categoria_id { get; set; }
}

public class HistoricoAtividade
{
    public int id { get; set; }
    public int atividade_id { get; set; }
    public int usuario_id { get; set; }
    public string acao { get; set; } = string.Empty;
    public string? dados_anteriores { get; set; }
    public string? dados_novos { get; set; }
    public DateTime criado_em { get; set; }
}

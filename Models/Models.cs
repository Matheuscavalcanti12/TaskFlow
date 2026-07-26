using System;

namespace Models;

public class Usuarios
{
    public int id_usuario { get; set; }
    public string nome { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string senha_hash { get; set; } = string.Empty;
    public DateTime criado_em { get; set; }
}

public class UsuarioCadastroRequest
{
    public string nome { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string senha { get; set; } = string.Empty;
}

public class Categorias
{
    public int id_categorias { get; set; }
    public string nome { get; set; } = string.Empty;
    public int usuario_id { get; set; }
}

public class Tarefas
{
    public int id { get; set; }
    public string titulo { get; set; } = string.Empty;
    public string? descricao { get; set; }
    public string status { get; set; } = string.Empty;
    public string prioridade { get; set; } = string.Empty;
    public DateTime? prazo { get; set; }
    public DateTime criada_em { get; set; }
    public DateTime concluida_em { get; set; }
    public int usuario_id { get; set; }
    public int? categoria_id { get; set; }
}

public class HistoricoAtividades
{
    public int id { get; set; }
    public int atividade_id { get; set; }
    public int usuario_id { get; set; }
    public string acao { get; set; } = string.Empty;
    public string? dados_anteriores { get; set; }
    public string? dados_novos { get; set; }
    public DateTime criado_em { get; set; }
}

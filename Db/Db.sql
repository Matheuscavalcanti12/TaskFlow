CREATE DATABASE IF NOT EXISTS TaskFlow;

USE TaskFlow;

-- Guarda os usuarios do sistema.
CREATE TABLE IF NOT EXISTS usuarios (
    -- O backend cria usuario sem enviar id, por isso o banco precisa gerar o id automaticamente.
    id_usuario INTEGER PRIMARY KEY AUTO_INCREMENT NOT NULL,

    -- O backend valida nome entre 3 e 100 caracteres.
    nome VARCHAR(100) NOT NULL,

    -- O backend valida email ate 100 caracteres e trata erro 1062 para email duplicado.
    email VARCHAR(100) NOT NULL UNIQUE,

    -- BCrypt gera hash grande, por isso VARCHAR(255) e suficiente.
    senha_hash VARCHAR(255) NOT NULL,

    -- Data de criacao usada pelo endpoint /perfil.
    criado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tabela criada para o usuario organizar as atividades por categoria.
CREATE TABLE IF NOT EXISTS categorias (
    -- O backend cria categoria sem enviar id, entao o banco gera automaticamente.
    id_categorias INTEGER PRIMARY KEY AUTO_INCREMENT NOT NULL,

    -- O endpoint de categoria grava somente o nome.
    nome VARCHAR(100) NOT NULL,

    -- O front mostra uma cor para cada categoria, entao o banco precisa guardar essa informacao.
    cor VARCHAR(20) NOT NULL DEFAULT '#6366f1',

    -- Garante que a categoria pertence a um usuario existente.
    usuario_id INTEGER NOT NULL,

    FOREIGN KEY (usuario_id) REFERENCES usuarios(id_usuario)
);

CREATE TABLE IF NOT EXISTS atividades (
    -- O backend faz INSERT sem id e depois usa LAST_INSERT_ID(), entao precisa ser AUTO_INCREMENT.
    id INTEGER PRIMARY KEY AUTO_INCREMENT NOT NULL,

    -- O backend e o front limitam titulo a 100 caracteres.
    titulo VARCHAR(100) NOT NULL,

    -- O backend aceita descricao nula e limita o tamanho antes de salvar.
    descricao TEXT NULL,

    -- O front usa pendente, em_andamento, concluida e cancelada; o service converte em_andamento para em andamento no backend.
    status VARCHAR(30) NOT NULL,

    -- O front envia baixa, media, alta ou urgente; o backend valida esses mesmos valores.
    prioridade VARCHAR(20) NOT NULL,

    -- O front pode criar atividade sem prazo, entao o campo precisa aceitar NULL.
    prazo TIMESTAMP NULL DEFAULT NULL,

    -- Data de criacao gerada pelo banco e devolvida pelos endpoints de atividade.
    criada_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Atividade pendente nao deve nascer concluida; por isso esse campo precisa aceitar NULL.
    concluida_em TIMESTAMP NULL DEFAULT NULL,

    -- Atividade sempre pertence ao usuario autenticado.
    usuario_id INTEGER NOT NULL,

    -- Categoria e opcional no front, por isso pode ser NULL.
    categoria_id INTEGER NULL,

    -- Atualiza automaticamente quando uma atividade e editada.
    atualizada_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    FOREIGN KEY (usuario_id) REFERENCES usuarios(id_usuario),

    FOREIGN KEY (categoria_id) REFERENCES categorias(id_categorias)
);

CREATE TABLE IF NOT EXISTS historico_atividades (
    -- Historico tambem deve gerar id sozinho quando for usado pelo backend.
    id INTEGER PRIMARY KEY AUTO_INCREMENT NOT NULL,

    -- Relaciona o historico com uma atividade existente.
    atividade_id INTEGER NOT NULL,

    -- Relaciona o historico com o usuario dono da acao.
    usuario_id INTEGER NOT NULL,

    -- Nome da acao realizada, como criada ou status_alterado.
    acao VARCHAR(50) NOT NULL,

    -- Campo livre para guardar dados anteriores quando necessario.
    dados_anteriores TEXT NULL,

    -- Campo livre para guardar dados novos quando necessario.
    dados_novos TEXT NULL,

    -- Data em que o registro de historico foi criado.
    criado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (atividade_id) REFERENCES atividades(id),

    FOREIGN KEY (usuario_id) REFERENCES usuarios(id_usuario)
);

SHOW TABLES;

SELECT * FROM usuarios;

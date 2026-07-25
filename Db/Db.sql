
create database TaskFlow;
use TaskFlow;
create table usuarios(
    id_usuario integer primary key auto_increment not null,
    nome varchar(100) not null,
    email varchar(100) not null,
    senha_hash varchar (255) not null,
    criado_em timestamp default current_timestamp
);

##tabela criada para o usuario o tipo de atividades que ele devera ter
create table categorias (
  id_categorias integer primary key auto_increment  not null,
    nome varchar(100) not null,
    usuario_id integer not null,
    foreign key (usuario_id) references usuarios(id_usuario)
);


CREATE TABLE atividades (
    id INTEGER PRIMARY KEY,
    titulo VARCHAR(100) NOT NULL,
    descricao TEXT,
    status VARCHAR(30) NOT NULL,
    prioridade VARCHAR(20) NOT NULL,
    prazo TIMESTAMP,
     criada_em TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP,
    concluida_em TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP,
    usuario_id INTEGER NOT NULL,
    categoria_id INTEGER,
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id_usuario),
    FOREIGN KEY (categoria_id) REFERENCES categorias(id_categorias)
);


CREATE TABLE historico_atividades (
    id INTEGER PRIMARY KEY,
    atividade_id INTEGER NOT NULL,
    usuario_id INTEGER NOT NULL,
    acao VARCHAR(50) NOT NULL,
    dados_anteriores TEXT,
    dados_novos TEXT,
    criado_em TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (atividade_id) REFERENCES atividades(id)
);

show tables;
# TaskFlow — Sistema de Gerenciamento de Atividades

O **TaskFlow** é uma aplicação web para gerenciamento de atividades pessoais ou profissionais. O sistema permite criar, organizar, acompanhar e concluir tarefas, utilizando recursos como categorias, prioridades, prazos, subtarefas, filtros e histórico de alterações.

O projeto foi desenvolvido com o objetivo de praticar conceitos de desenvolvimento full stack, incluindo criação de interfaces, validações, APIs, regras de negócio, autenticação, modelagem de banco de dados e arquitetura em camadas.

> Status do projeto: em desenvolvimento.

---

## Funcionalidades

### Autenticação

* Cadastro de usuários;
* Login e logout;
* Proteção de rotas;
* Senhas armazenadas de forma segura;
* Cada usuário acessa apenas as próprias informações.

### Atividades

* Criar atividade;
* Listar atividades;
* Visualizar detalhes;
* Editar atividade;
* Excluir atividade;
* Alterar status;
* Definir prioridade;
* Definir prazo;
* Associar uma categoria;
* Adicionar descrição;
* Marcar atividade como concluída;
* Reabrir atividade concluída;
* Cancelar atividade;
* Identificar atividades atrasadas.

### Organização

* Pesquisa por título ou descrição;
* Filtro por status;
* Filtro por prioridade;
* Filtro por categoria;
* Filtro por prazo;
* Ordenação por data, prioridade ou criação;
* Visualização em lista;
* Visualização em Kanban.

### Categorias

* Criar categoria;
* Editar categoria;
* Excluir categoria;
* Associar atividades a categorias;
* Impedir categorias duplicadas para o mesmo usuário.

### Subtarefas

* Adicionar subtarefas;
* Editar subtarefas;
* Excluir subtarefas;
* Marcar subtarefas como concluídas;
* Calcular o progresso da atividade.

### Recursos futuros

* Histórico de alterações;
* Atividades recorrentes;
* Notificações;
* Compartilhamento de atividades;
* Comentários;
* Anexos;
* Dashboard com estatísticas;
* Recuperação de senha.

---

## Regras de negócio

O sistema possui regras para manter a consistência das informações.

### Status disponíveis

Uma atividade pode ter os seguintes status:

```text
pendente
em_andamento
concluida
cancelada
```

### Transições de status

* Uma atividade pendente pode ser iniciada, concluída ou cancelada;
* Uma atividade em andamento pode voltar para pendente, ser concluída ou cancelada;
* Uma atividade concluída pode ser reaberta;
* Uma atividade cancelada não pode ser concluída diretamente;
* Para concluir uma atividade cancelada, ela deve primeiro ser reaberta;
* Ao concluir uma atividade, o sistema registra a data de conclusão;
* Ao reabrir uma atividade, a data de conclusão é removida.

### Atividade atrasada

Uma atividade é considerada atrasada quando:

* possui um prazo;
* o prazo é anterior à data e hora atuais;
* não está concluída;
* não está cancelada.

O estado de atraso não precisa ser armazenado diretamente no banco. Ele pode ser calculado utilizando o prazo e o status da atividade.

### Validações

* O título é obrigatório;
* O título deve possuir entre 3 e 100 caracteres;
* A descrição pode possuir no máximo 500 caracteres;
* O status deve ser válido;
* A prioridade deve ser válida;
* O prazo deve possuir uma data válida;
* Uma nova atividade não pode ser criada com prazo no passado;
* Uma categoria associada deve pertencer ao mesmo usuário da atividade;
* O usuário só pode editar ou excluir as próprias atividades.

---

## Prioridades disponíveis

```text
baixa
media
alta
urgente
```

---

## Tecnologias

### Frontend

* React;
* TypeScript;
* Vite;
* Tailwind CSS;
* shadcn/ui;
* React Router;
* React Hook Form;
* Zod;
* Axios ou Fetch API.

### Backend

* Node.js;
* TypeScript;
* Express;
* JWT para autenticação;
* bcrypt para proteção das senhas;
* Zod para validação;
* MySQL.

### Banco de dados

* MySQL;
* Chaves primárias;
* Chaves estrangeiras;
* Restrições de integridade;
* Relacionamentos entre usuários, categorias e atividades.

> As tecnologias podem ser adaptadas conforme a evolução do projeto.

---



## Autor

Desenvolvido por **[Dev Matheus Cavalcanti]** como projeto de estudo e prática de desenvolvimento full stack.

---


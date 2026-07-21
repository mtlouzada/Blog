# Blog API

API RESTful para gerenciamento de conteúdo de blog, com autenticação via JWT, controle de acesso via API Key, envio de e-mails via SMTP e integração com SQL Server.

## Tecnologias Utilizadas

- ASP.NET Core 8 (LTS)
- Entity Framework Core 8
- SQL Server
- JWT Authentication
- Swagger
- User Secrets
- xUnit + Respawn (testes de integração)

## Funcionalidades

- Autenticação de usuários
- Cadastro e login com geração de token JWT
- CRUD de usuários, categorias e posts
- Envio de e-mails
- Documentação automática via Swagger

## Configurando Secrets Locais

- Antes de rodar a API localmente, execute:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "sua_string_conexao"
dotnet user-secrets set "JwtKey" "sua_jwt_key"
dotnet user-secrets set "ApiKey" "sua_api_key"
dotnet user-secrets set "SmtpConfiguration:Host" "smtp.exemplo.com"
dotnet user-secrets set "SmtpConfiguration:Port" "587"
dotnet user-secrets set "SmtpConfiguration:UserName" "seu_email"
dotnet user-secrets set "SmtpConfiguration:Password" "sua_senha"
```

## Banco de dados

```bash
dotnet ef database update
```

## Testes

A suíte de integração sobe a aplicação inteira em memória e conversa com ela por HTTP,
contra um SQL Server real — schema, índices, constraints e migrations de verdade.

```bash
dotnet test tests/Blog.IntegrationTests/Blog.IntegrationTests.csproj
```

No Windows não é preciso configurar nada: por padrão usa o LocalDB. Para apontar para
outro servidor (é o que o CI faz, com um container do SQL Server), defina
`BLOG_TESTS_SQLSERVER` com a connection string do servidor.

Cada execução cria um banco descartável, aplica as migrations e o derruba ao final;
nenhum banco de desenvolvimento é tocado.

Detalhes das decisões de projeto da suíte — e os defeitos que ela encontrou — em
[docs/testes-de-integracao.md](docs/testes-de-integracao.md).

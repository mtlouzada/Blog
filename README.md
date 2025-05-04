# Bog API

API RESTful para gerenciamento de conteúdo de blog, com autenticação via JWT, controle de acesso via API Key, envio de e-mails via SMTP e integração com SQL Server.

## Tecnologias Utilizadas

- ASP.NET Core
- Entity Framework Core
- SQL Server
- JWT Authentication
- Swagger
- User Secrets 

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
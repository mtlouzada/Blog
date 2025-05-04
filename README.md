# Bog API

API RESTful para gerenciamento de conte�do de blog, com autentica��o via JWT, controle de acesso via API Key, envio de e-mails via SMTP e integra��o com SQL Server.

## Tecnologias Utilizadas

- ASP.NET Core
- Entity Framework Core
- SQL Server
- JWT Authentication
- Swagger
- User Secrets 

## Funcionalidades

- Autentica��o de usu�rios
- Cadastro e login com gera��o de token JWT
- CRUD de usu�rios, categorias e posts
- Envio de e-mails
- Documenta��o autom�tica via Swagger

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
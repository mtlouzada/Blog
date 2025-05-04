# Bog API

## Configurando Secrets Locais

 - Antes de rodar a API localmente, execute:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "sua_string_conexao"
dotnet user-secrets set "JwtKey" "sua_jwt_key"
dotnet user-secrets set "ApiKey" "sua_api_key"
dotnet user-secrets set "SmtpConfiguration:Password" "sua_senha_smtp"
```
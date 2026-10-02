# teste-b2c

Aplicação ASP.NET Core MVC com autenticação OpenID Connect para Microsoft External ID.

## Configuração

1. Defina o segredo do cliente no ambiente local ou em user-secrets:

```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "<client-secret>" --project .\teste-b2c.csproj
```

2. No App Registration, adicione o redirect URI web:

```text
https://localhost:8002/signin-oidc
```

3. Se necessário, adicione também o logout callback:

```text
https://localhost:8002/signout-callback-oidc
```

4. Execute a aplicação:

```powershell
dotnet run --project .\teste-b2c.csproj
```

## Gerar Certificado para Signing Key (Linux)

Para gerar um certificado auto-assinado em Linux que será usado como signing key no Azure B2C External ID:

```bash
# Criar diretório para certificados (se não existir)
mkdir -p certs

# Gerar chave privada (2048 bits)
openssl genrsa -out certs/teste-b2c-externalid-signing.key 2048

# Gerar certificado auto-assinado válido por 2 anos
openssl req -new -x509 -key certs/teste-b2c-externalid-signing.key \
  -out certs/teste-b2c-externalid-signing.cer \
  -days 730 \
  -subj "/CN=teste-b2c-externalid"
```

**Resultado:**
- `certs/teste-b2c-externalid-signing.cer` - Certificado público (use para registrar no Azure B2C)
- `certs/teste-b2c-externalid-signing.key` - Chave privada (guarde com segurança)

**Para registrar no Azure B2C External ID:**
1. Acesse o portal do Azure B2C
2. Navegue até External Identities > Signing Keys
3. Faça upload do arquivo `.cer` gerado

## Observações

- O `appsettings.json` versionado é um template sem IDs; substitua pelo arquivo do seu tenant.
- O `ClientSecret` não foi armazenado no código-fonte; ele precisa ser fornecido localmente.



Para testar:

> Importante: a atualizacao de custom attribute deve ser feita EXCLUSIVAMENTE com o client ID da aplicacao interna criada pelo B2C (`b2c-extensions-app`).
> Nao use o client ID da aplicacao MVC para atualizar atributos `extension_*` no Microsoft Graph.

```bash
USER_ID="<user-object-id>"
CUSTOM_ATTR="extension_<b2c-extensions-app-id-sem-hifens>_cpf"
TENANT_ID="<tenant-id>"
# Client ID da b2c-extensions-app (obrigatorio para atualizar extension_*)
CLIENT_ID="<b2c-extensions-app-client-id>"
CLIENT_SECRET="<client-secret>"

TOKEN=$(curl -X POST "https://login.microsoftonline.com/$TENANT_ID/oauth2/v2.0/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=$CLIENT_ID" \
  -d "client_secret=$CLIENT_SECRET" \
  -d "scope=https://graph.microsoft.com/.default" \
  -d "grant_type=client_credentials" | jq -r '.access_token')

# Listar usuarios
curl -sG "https://graph.microsoft.com/v1.0/users" \
  -H "Authorization: Bearer $TOKEN" \
  --data-urlencode "\$select=id,displayName,userPrincipalName,mail,accountEnabled,identities,$CUSTOM_ATTR" \
  --data-urlencode "\$top=999" | jq --arg custom "$CUSTOM_ATTR" '.value[] | { id, displayName, userPrincipalName, mail, accountEnabled, identities, profile: .[$custom] }'

# Atualizar custom propertie
curl -i -X PATCH \
  "https://graph.microsoft.com/v1.0/users/$USER_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"$CUSTOM_ATTR\":\"<cpf>\"}"
  
curl -sG "https://graph.microsoft.com/v1.0/users/$USER_ID" \
-H "Authorization: Bearer $TOKEN" \
--data-urlencode "\$select=id,displayName,userPrincipalName,extension_<app-id>_Profile,$CUSTOM_ATTR" | jq
# Output
{
  "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#users(id,displayName,userPrincipalName,extension_<app-id>_Profile,extension_<app-id>_cpf)/$entity",
  "id": "<user-object-id>",
  "displayName": "<nome>",
  "userPrincipalName": "<usuario>#EXT#@<tenant>.onmicrosoft.com",
  "extension_<app-id>_Profile": "Investidor"
}

```
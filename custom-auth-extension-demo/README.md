# Demo: Microsoft Entra External ID + Custom Authentication Extension

Quando o usuário faz login, o tenant External ID dispara o evento **OnTokenIssuanceStart**. Esse evento chama a nossa API hospedada no Azure App Service. A API devolve claims extras, que entram no `id_token` da aplicação web OpenID Connect.

```
 Browser ──(1) login──▶ <tenant>.ciamlogin.com  (user flow de signup/signin)
                               │
                               │ (2) OnTokenIssuanceStart  POST + Bearer token (azp 99045fe1-...)
                               ▼
                    <api-da-extensao>  ──(3) { loyaltyTier, customerSegment, correlationId, ... }
                               │
                               ▼ (4) claimsMappingPolicy (Source=CustomClaimsProvider) → id_token
 Browser ◀──(5) code+PKCE── <web-demo>  (exibe o token; sessão só em cookies)
```

## Objetos no tenant External ID

- **App registration da API**: tem o identifier URI `api://<host-da-api>/<appId>` e a permissão `CustomAuthenticationExtension.Receive.Payload`, com admin consent.
- **Custom authentication extension** (`onTokenIssuanceStartCustomExtension`), apontando para `/api/token-issuance-start`.
- **App OIDC da web**: tem `acceptMappedClaims = true`, redirect URIs de localhost e do Azure, e um client secret.
- **Claims mapping policy** atribuída ao service principal da web. Mapeia `loyaltyTier`→`loyalty_tier`, `customerSegment`→`customer_segment`, `correlationId`→`correlation_id`, `extensionCalledAt`→`extension_called_at` e `demoRoles`→`demo_roles`.
- **Listener OnTokenIssuanceStart**: executa a extensão para o app web.
- **User flow** (email + senha) vinculado ao app web.

## Estrutura

```
CustomAuthExtension.Api/        Minimal API .NET 10: POST /api/token-issuance-start (JWT validado) e GET /health
CustomAuthExtension.Functions/  Mesma API como Azure Functions (isolated worker, ReadyToRun)
CustomAuthExtension.Web/        MVC .NET 10 (baseado em ../openid): login, página de detalhes do token, sem banco
```

## Configuração

Os `appsettings.json` versionados são templates, sem IDs nem secrets. Substitua cada um pelo arquivo do seu tenant (enviado separadamente):

| Projeto | Arquivo | Seção |
|---|---|---|
| `CustomAuthExtension.Api` | `appsettings.json` | `ExternalId` (`TenantId`, `ApiClientId`, `ApiIdentifierUri`) |
| `CustomAuthExtension.Web` | `appsettings.json` | `AzureAd` (`Instance`, `Domain`, `TenantId`, `Authority`, `ClientId`, `ClientSecret`) |
| `CustomAuthExtension.Functions` | `local.settings.json` (fora do git) | modelo em `local.settings.example.json` |

No Azure, use os mesmos nomes como app settings (`ExternalId__TenantId`, `AzureAd__ClientId`, ...). Não faça commit dos arquivos preenchidos.

## Rodar a web demo localmente

```powershell
cd CustomAuthExtension.Web
dotnet run --launch-profile https      # https://localhost:7100
```

Rodando localmente, a web continua usando a API da extensão no Azure, porque é o serviço do Entra que chama a URL pública.

## Versão Azure Functions: `CustomAuthExtension.Functions`

É a mesma extensão como Azure Functions (.NET 10, isolated worker, HTTP trigger). As rotas são iguais às da Minimal API: `POST /api/token-issuance-start` e `GET /health`. O projeto compila os mesmos `Models.cs` e `DemoClaims.cs` da API (arquivos linkados). Uma mudança de regra vale para os dois.

- **Autenticação**: o trigger é `Anonymous` e a função valida o Bearer token do Entra no próprio código (`EntraEventTokenValidator`), com as mesmas regras da API: metadata do `ciamlogin.com`, issuers, audiences e `azp`/`appid` = `99045fe1-...`. Sem token ou com token inválido, a resposta é 401. Se o token não vier do Entra, a resposta é 403.
- **Configuração**: `ExternalId__TenantId`, `ExternalId__ApiClientId`, `ExternalId__ApiIdentifierUri`, `ExternalId__ApiClientSecret` e `ExternalId__AuthenticationEventsAppId`. Localmente, copie `local.settings.example.json` para `local.settings.json` (está no `.gitignore`) e preencha.
- **AOT**: o projeto usa **ReadyToRun**, não Native AOT. Com `PublishAot`, o binário nativo é gerado, mas cai no startup. O `Microsoft.Azure.Functions.Worker.Core` chama `FileVersionInfo.GetVersionInfo(Assembly.Location)`, que é vazio em AOT (warning IL3000). Isso continua igual no `main` do worker. O código já é AOT-safe: JSON via source generator (`AppJsonContext`) e `HttpRequestData`, sem a integração ASP.NET Core. Quando o worker suportar AOT, basta trocar `PublishReadyToRun` por `PublishAot`.
- **Cold start**: o Entra espera no máximo 2s. Use Flex Consumption com instâncias always ready, Premium ou um App Service Plan com Always On. Não use Consumption sem instância quente.

```bash
cd CustomAuthExtension.Functions
func start                                                 # local: http://localhost:7300
dotnet publish -c Release -r linux-x64 -o ./publish        # ReadyToRun precisa de RID
cd publish && zip -r ../functions.zip . && cd ..
az functionapp deployment source config-zip -g <rg> -n <function-app> --src functions.zip
```

Depois do deploy, aponte o `targetUrl` da extensão para `https://<function-app>.azurewebsites.net/api/token-issuance-start` e ajuste o identifier URI do app da API (`api://<host>/<appId>`). Atualize o `ExternalId__ApiIdentifierUri` com o novo valor.

## Roteiro da demo

1. Abra a web e clique em **Entrar com External ID**. Entre com um usuário de teste, ou cadastre um novo usuário pelo user flow.
2. Em **Detalhes do token**, mostre as claims destacadas (`loyalty_tier`, `demo_roles`, ...), o payload decodificado e o `id_token` bruto.
3. Mostre a chamada chegando na API:
   `az webapp log tail -g <rg> -n <api-webapp>`
   Procure as linhas `OnTokenIssuanceStart recebido` e `Claims devolvidas`. O `correlation_id` do token é o mesmo do log.
4. Altere a regra em `DemoClaims.Build` (`CustomAuthExtension.Api/DemoClaims.cs`), publique de novo e faça login.

## Aprendizados da configuração

- O token que o Entra envia para a extensão é assinado com chaves que estão **só** no metadata de `https://{tenantId}.ciamlogin.com/{tenantId}/v2.0`. Se a API usar `login.microsoftonline.com`, a assinatura é rejeitada (IDX10503) e o login falha com `AADSTS1100001 / 1003002`.
- O Entra espera no máximo **2 segundos** pela resposta da API. Por isso a API carrega o metadata/JWKS no startup, o Always On está ligado e o App Service fica numa região perto do tenant.
- A claims mapping policy precisa ser atribuída ao service principal com a sessão delegada do admin. Com token de aplicação, o Graph responde "Unable to read the company information".
- O token delegado do az CLI não consegue criar extensões, listeners nem user flows. Para automatizar, use um app com permissões de aplicação no Graph.
- O `countryCode` do tenant External ID precisa bater com a localização de dados. Para United States, é `US`.
- O listener `OnTokenIssuanceStart` só deve ser criado quando o `targetUrl` estiver respondendo. Se ele não responder, o login do app quebra.

## Custo / limpeza

Um App Service B1 custa cerca de US$13/mês. O External ID é gratuito até 50 mil MAU. Para remover tudo:

```bash
az group delete -n <rg> --yes
```

Se o resource group contiver o tenant External ID, é preciso antes excluir os objetos do tenant, ou excluir o tenant pelo portal do Entra.

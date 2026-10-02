# Guia Completo: Criar e Configurar Nova Aplicação no Azure External ID

Este documento descreve o passo a passo para criar e configurar uma nova aplicação no Azure External ID com suporte a custom attributes e certificados de assinatura.

---

## ⚠️ Pré-requisitos

Antes de começar, certifique-se de que:

- ✅ Você tem um **tenant do External ID** já criado no Azure
- ✅ Você tem acesso ao **Microsoft Entra admin center** (https://entra.microsoft.com)
- ✅ Você tem permissões de **Application Administrator** ou superior
- ✅ Você tem **OpenSSL** instalado (para gerar certificados em Linux/Mac) ou **PowerShell** (para Windows)

---

## Passo 1: Registrar a Aplicação no Portal Entra

1. Acesse [Microsoft Entra admin center](https://entra.microsoft.com/)
2. Navegue até **Identity > Applications > App registrations**
3. Clique em **New registration**
4. Preencha os dados:
   - **Name**: Nome da aplicação (ex: `Teste B2C MVC`)
   - **Supported account types**: Selecione `Accounts in this organizational directory only`
5. Clique em **Register**

**Guarde os dados gerados:**
- `Application (client) ID`
- `Directory (tenant) ID`

---

## Passo 2: Configurar URIs de Redirecionamento

1. Na página de registro da aplicação, acesse **Authentication** (no menu esquerdo)
2. Na seção **Platform configurations**, clique em **Add a platform**
3. Selecione **Web**
4. Em **Redirect URIs**, adicione:
   ```
   https://localhost:8002/signin-oidc
   https://localhost:8002/signout-callback-oidc
   ```
5. Marque as opções:
   - ✅ **Access tokens (used for implicit flows)**
   - ✅ **ID tokens (used for implicit and hybrid flows)**
6. Clique em **Configure**

---

## Passo 3: Criar Certificado de Assinatura

### Opção A: Windows (PowerShell)

```powershell
# Gerar certificado auto-assinado
$cert = New-SelfSignedCertificate `
  -CertStoreLocation cert:\CurrentUser\My `
  -DnsName "seu-app-nome" `
  -FriendlyName "Seu App Signing Key" `
  -NotAfter (Get-Date).AddYears(2) `
  -KeyLength 2048

$thumbprint = $cert.Thumbprint
Write-Host "Thumbprint: $thumbprint"

# Exportar certificado (.cer)
$cerPath = ".\certs\seu-app-signing.cer"
Export-Certificate -Cert $cert -FilePath $cerPath -Type CERT

# Exportar com chave privada (.pfx)
$pfxPath = ".\certs\seu-app-signing.pfx"
$password = ConvertTo-SecureString -String "SuaSenha123!" -AsPlainText -Force
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password

Write-Host "Certificado .cer: $cerPath"
Write-Host "Certificado .pfx: $pfxPath"
Write-Host "Senha PFX: SuaSenha123!"
```

### Opção B: Linux/Mac (OpenSSL)

```bash
# Criar diretório para certificados
mkdir -p certs

# Gerar chave privada (2048 bits)
openssl genrsa -out certs/seu-app-signing.key 2048

# Gerar certificado auto-assinado válido por 2 anos
openssl req -new -x509 \
  -key certs/seu-app-signing.key \
  -out certs/seu-app-signing.cer \
  -days 730 \
  -subj "/CN=seu-app-nome"

echo "Certificado criado: certs/seu-app-signing.cer"
echo "Chave privada criada: certs/seu-app-signing.key"
```

---

## Passo 4: Registrar Certificado nos Certificates & Secrets

1. Na página de registro da aplicação, acesse **Certificates & secrets** (no menu esquerdo)
2. Clique na aba **Certificates**
3. Clique em **Upload certificate**
4. Selecione o arquivo `.cer` gerado na etapa anterior
5. Adicione uma **Description** (ex: `Signing Certificate 2026`)
6. Clique em **Add**

**Guarde:**
- `Thumbprint` do certificado

---

## Passo 5: Criar Client Secret

1. Ainda em **Certificates & secrets**, clique na aba **Client secrets**
2. Clique em **New client secret**
3. Adicione uma **Description** (ex: `API Access Secret`)
4. Selecione **Expires**: `6 months` (ou conforme sua política)
5. Clique em **Add**

**⚠️ IMPORTANTE:** Copie o valor do secret IMEDIATAMENTE, pois ele não será exibido novamente
- Guarde em um local seguro (Azure Key Vault é recomendado)

---

## Passo 6: Acessar a Enterprise Application (Service Principal)

1. Na página de registro da aplicação, localize a seção **Essentials**
2. Procure por **Managed application in local directory**
3. Clique no link com o nome da sua aplicação

Agora você está na página da **Enterprise Application** (Service Principal)

---

## Passo 7: Criar/Verificar Custom Attributes no Tenant

Antes de configurar o manifest, você precisa que os custom attributes existam no tenant.

1. Acesse **Identity > External Identities > User attributes**
2. Verifique se o custom attribute que você quer usar já existe
3. Se não existir, clique em **Add custom attribute** e crie

Exemplo de custom attribute:
- **Attribute name**: `Profile` (ex: será `extension_xxx_Profile`)
- **Data type**: `String`
- **Description**: `User profile type`

---

## Passo 8: Criar User Flow (Sign-up and Sign-in)

1. Acesse **Identity > External Identities > User flows**
2. Clique em **New user flow**
3. Selecione **Sign up and sign in**
4. Preencha:
   - **Name**: `susi-flow` (ou conforme preferência)
   - **Identity providers**: Selecione os desejados (Email, Google, Microsoft, etc)
5. Em **Sign-up attributes**: Selecione os atributos que quer coletar durante sign-up
   - ✅ Email Address
   - ✅ Display Name
   - ✅ Custom attributes (se criou)
6. Clique em **Create**

---

## Passo 9: Configurar Single Sign-on - Adicionar Claims

1. Na página da **Enterprise Application**, clique em **Single sign-on** (no menu esquerdo)
2. Selecione **SAML** (ou **OpenID Connect** conforme sua arquitetura)
3. Procure pela seção **Attributes & Claims**
4. Clique no ícone de **Edit**

### Adicionar Built-in Attributes:

1. Clique em **Add new claim**
2. Preencha:
   - **Name**: `email`
   - **Source**: `Attribute`
   - **Source attribute**: `user.mail`
3. Clique em **Save**

Repita para: `name`, `given_name`, `family_name`, etc.

### Adicionar Custom Attributes:

1. Clique em **Add new claim**
2. Preencha:
   - **Name**: `extension_Profile` (ou o nome do seu atributo)
3. Próximo a **Source**: Selecione **Directory schema extension**
4. Na janela **Select Application**:
   - Selecione **b2c-extensions-app**
   - Clique em **Select**
5. Na janela **Add Extension Attributes**:
   - Procure e selecione o custom attribute que criou
   - Clique em **Add**
6. Clique em **Save**

---

## Passo 10: Atualizar o Application Manifest

1. Na página de registro da aplicação (não é a Enterprise App), acesse **Manifest** (no menu esquerdo)
2. Localize a propriedade `acceptMappedClaims` e altere o valor para `true`:
   ```json
   "acceptMappedClaims": true,
   ```

3. Localize a propriedade `isFallbackPublicClient` e altere para `true`:
   ```json
   "isFallbackPublicClient": true,
   ```

4. Clique em **Save**

---

## Passo 11: Configurar Permissões de API (Opcional)

Se sua aplicação precisar acessar a Microsoft Graph ou outras APIs:

1. Na página de registro, acesse **API permissions**
2. Clique em **Add a permission**
3. Selecione a API desejada (ex: **Microsoft Graph**)
4. Selecione **Application permissions** ou **Delegated permissions** conforme necessário
5. Selecione as permissões específicas
6. Clique em **Add permissions**

Para usar essas permissões em aplicações daemon/backend, você pode precisar de **admin consent**:
- Clique em **Grant admin consent for [seu tenant]**

---

## Passo 12: Atualizar Configurações da Aplicação

Atualize seus arquivos de configuração da aplicação com:

### `appsettings.json`

```json
{
  "AzureAd": {
    "Instance": "https://[seu-tenant].ciamlogin.com/",
    "Domain": "[seu-tenant].onmicrosoft.com",
    "TenantId": "[Directory (tenant) ID]",
    "Authority": "https://[Directory (tenant) ID].ciamlogin.com/[Directory (tenant) ID]/v2.0",
    "ValidIssuer": "https://[Directory (tenant) ID].ciamlogin.com/[Directory (tenant) ID]/v2.0",
    "ClientId": "[Application (client) ID]",
    "ClientSecret": "[Client Secret Value]",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "MetadataAddress": "https://[seu-tenant].ciamlogin.com/[Directory (tenant) ID]/v2.0/.well-known/openid-configuration?appid=[Application (client) ID]",
    "CertificateThumbprint": "[Thumbprint do Certificado]"
  }
}
```

---

## Checklist de Conclusão

- [ ] Aplicação registrada no Entra
- [ ] URIs de redirecionamento configurados
- [ ] Certificado gerado e registrado
- [ ] Client Secret criado e armazenado com segurança
- [ ] Custom attributes criados (se necessário)
- [ ] User flow criado
- [ ] Enterprise Application Single Sign-on configurado
- [ ] Claims adicionados (built-in e custom)
- [ ] Application manifest atualizado (`acceptMappedClaims` e `isFallbackPublicClient`)
- [ ] Permissões de API configuradas (se necessário)
- [ ] Configurações da aplicação atualizadas

---

## Links Úteis

- [Documentação Official - Add user attributes to token claims](https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-add-attributes-to-token)
- [Microsoft Entra admin center](https://entra.microsoft.com/)
- [Quickstart: Register an application](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app)
- [Create a sign-up and sign-in user flow](https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-user-flow-sign-up-sign-in-customers)
- [Create custom attributes](https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-define-custom-attributes)

---

## Troubleshooting

### ❌ "acceptMappedClaims not found in manifest"
Procure por `acceptMappedClaims` e `isFallbackPublicClient` - eles devem estar no nível raiz do JSON do manifest.

### ❌ "Custom attribute não aparece na lista"
Certifique-se de que:
1. O atributo foi criado em **Identity > External Identities > User attributes**
2. Aguarde alguns minutos para propagação
3. Verifique se a aplicação `b2c-extensions-app` está presente no tenant

### ❌ "Erro ao fazer login - claims não aparecem no token"
1. Verifique se os claims foram adicionados na seção **Attributes & Claims**
2. Confirme que o manifest tem `acceptMappedClaims: true`
3. Verifique os logs no **Application Insights** ou **Azure Monitor**

---

**Última atualização:** 19/06/2026

# secrets/

Pasta **local e privada**: tudo aqui (exceto este README) está no `.gitignore` e nunca vai para o GitHub.
O repositório público tem só o código; os `appsettings.json` versionados são templates vazios.

Organização: uma pasta por tenant/cliente, espelhando os projetos do repositório.

```
secrets/
  <tenant>/
    README.md                         instruções específicas do tenant
    .state.env  ou  infra/.state.env  IDs e secrets gravados pelos scripts
    configure*.sh, infra/, e2e/       scripts que provisionam/configuram o ambiente
    <Projeto>/appsettings.json        arquivo completo para enviar ao cliente
    <Projeto>/local.settings.json     (Azure Functions)
```

Para usar localmente, copie o `appsettings.json` do tenant por cima do template no projeto — e **não faça commit** dele.
Os scripts leem o código a partir de `../../<projeto>` (ou `../../../`, dentro de `infra/`) e escrevem só dentro da própria pasta do tenant.

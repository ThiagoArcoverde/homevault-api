# Copilot Instructions

## Contexto

- O Homevault é uma API para centralizar automações residenciais.
- A API será consumida futuramente por aplicações mobile e web.
- O projeto utiliza .NET 10, ASP.NET Core, Entity Framework Core, SQLite, Swagger e FluentValidation.

## Arquitetura

- Seguir arquitetura hexagonal.
- Manter o domínio independente de ASP.NET Core, Entity Framework Core, SQLite e outros detalhes de infraestrutura.
- Manter casos de uso e portas na camada `Homevault.Application`.
- Manter entidades e regras de negócio na camada `Homevault.Domain`.
- Manter persistência e integrações externas na camada `Homevault.Infrastructure`.
- Manter controllers e composição da aplicação em `Homevault-api`.
- Controllers devem ser finos e delegar regras de negócio aos casos de uso.
- Não expor `DbContext` diretamente nos controllers.

## API

- Usar versionamento por URL no formato `/api/v1/...`.
- Usar `ProblemDetails` para respostas de erro.
- Usar FluentValidation para validar entradas.
- Documentar endpoints públicos no Swagger.
- Manter o endpoint `/health` como endpoint operacional não versionado.

## Persistência

- Usar Entity Framework Core somente na camada `Homevault.Infrastructure`.
- Usar SQLite como persistência padrão enquanto o projeto permanecer pequeno.
- Criar migrations sempre que o modelo persistido for alterado.
- Não versionar arquivos de banco SQLite (`*.db`, `*.db-shm` e `*.db-wal`).
- Evitar acoplar regras de negócio ao provedor de banco, permitindo uma futura troca do driver.

## Segurança

- Nunca adicionar senhas, tokens, chaves privadas ou connection strings sensíveis ao código ou ao repositório.
- Usar User Secrets, variáveis de ambiente ou um gerenciador de segredos para valores sensíveis.
- Não incluir caminhos locais, dados pessoais ou credenciais nas instruções, logs ou exemplos.
- Verificar o `.gitignore` antes de adicionar arquivos de configuração ou banco.

## Qualidade e testes

- Fazer alterações pequenas, focadas e compatíveis com os padrões existentes.
- Reutilizar abstrações existentes antes de criar novas dependências.
- Criar testes para regras de domínio e casos de uso.
- Criar testes de integração para endpoints e persistência quando aplicável.
- Executar `dotnet build` após alterações.
- Executar `dotnet test` quando houver testes relacionados à alteração.
- Não ignorar erros ou avisos de compilação sem justificativa.
- Atualizar o README quando uma funcionalidade pública ou um comando de desenvolvimento for alterado.

## Git

- Usar Conventional Commits sempre que criar commits neste projeto Homevault.
- Usar tipos como `feat`, `fix`, `docs`, `refactor`, `test`, `build` e `chore` de acordo com a natureza da alteração.
- Escrever mensagens de commit claras, curtas e no imperativo.

## Comandos úteis

```powershell
dotnet build
dotnet test
dotnet ef migrations add NomeDaMigration `
  --project Homevault.Infrastructure `
  --startup-project Homevault-api
```
- Seguir arquitetura hexagonal; manter domínio e aplita 49 gracação desacoplados de ASP.NET Core, EF Core e SQLite; usar ProblemDetails, FluentValidation e versionamento por URL.
# Homevault API

API central para automações residenciais. O projeto será utilizado como hub de integração para futuros aplicativos mobile e web, dispositivos IoT e regras de automação da casa.

## Tecnologias

- .NET 10
- ASP.NET Core
- Entity Framework Core
- SQLite
- Swagger / OpenAPI
- FluentValidation
- Arquitetura hexagonal

## Requisitos

- .NET SDK 10
- Git

Para confirmar a versão instalada:

```powershell
dotnet --version
```

## Executando localmente

Clone o repositório e entre na pasta do projeto:

```powershell
git clone https://github.com/ThiagoArcoverde/homevault-api.git
cd homevault-api
```

Restaure as dependências e execute a API no perfil de desenvolvimento:

```powershell
dotnet restore
dotnet run --project Homevault-api/Homevault-api.csproj --launch-profile dev
```

A documentação do Swagger ficará disponível em:

```text
https://localhost:<porta>/swagger
```

## Banco de dados

A aplicação utiliza SQLite. O perfil `dev` usa o banco `homevault-dev.db`,
configurado em `Homevault-api/appsettings.Development.json`. O perfil `local`
usa `homevault.db`, configurado em `Homevault-api/appsettings.json`.

Para executar a API usando o banco local:

```powershell
dotnet run --project Homevault-api/Homevault-api.csproj --launch-profile local
```

A configuração base do SQLite é:

```json
{
  "ConnectionStrings": {
	"Homevault": "Data Source=homevault.db"
  }
}
```

Para criar ou atualizar o banco usando as migrations:

```powershell
Push-Location Homevault-api
dotnet ef database update `
  --project ..\Homevault.Infrastructure\Homevault.Infrastructure.csproj `
  --startup-project .\Homevault-api.csproj `
  --connection "Data Source=homevault-dev.db"
Pop-Location
```

O comando acima atualiza o banco de desenvolvimento. Para atualizar o banco
usado pelo perfil `local`, execute sem o argumento de ambiente:

```powershell
Push-Location Homevault-api
dotnet ef database update `
  --project ..\Homevault.Infrastructure\Homevault.Infrastructure.csproj `
  --startup-project .\Homevault-api.csproj `
  --connection "Data Source=homevault.db"
Pop-Location
```

O arquivo do banco e seus arquivos auxiliares são ignorados pelo Git.

## Coleta de clima

A API consulta a temperatura e a umidade atuais de Maringá/PR usando o Open-Meteo
e armazena uma medição no SQLite a cada 15 minutos. A primeira coleta é feita
quando a API inicia. Se o provedor estiver indisponível, a falha é registrada e
o coletor continua tentando no próximo intervalo.

A localização, o intervalo e o timeout podem ser alterados em
`Homevault-api/appsettings.json`, na seção `Weather`.

Endpoints disponíveis:

```http
GET /api/v1/weather/current
GET /api/v1/weather/history?page=1&pageSize=50
GET /api/v1/weather/history?from=2026-01-01T00:00:00Z&to=2026-01-31T23:59:59Z
```

## Endpoints atuais

### Health check

```http
GET /health
```

Retorna `200 OK` quando a API está saudável.

### Criar uma casa

```http
POST /api/v1/homes
Content-Type: application/json
```

Exemplo de requisição:

```json
{
  "name": "Minha casa"
}
```

A validação exige um nome não vazio com no máximo 200 caracteres.

## Arquitetura

O projeto está organizado em camadas baseadas em arquitetura hexagonal:

```text
Homevault-api           Adaptador de entrada HTTP e composição
Homevault.Application   Casos de uso e portas
Homevault.Domain        Entidades e regras de domínio
Homevault.Infrastructure Adaptadores de persistência e integrações externas
```

As regras de negócio não dependem diretamente do ASP.NET Core ou do SQLite. A infraestrutura implementa as portas definidas pela aplicação, permitindo trocar o mecanismo de persistência no futuro.

## Configuração segura

Não armazene credenciais, tokens ou senhas no repositório. Para configurações locais, utilize:

- `appsettings.Local.json`;
- User Secrets;
- variáveis de ambiente;
- um gerenciador de segredos em ambientes de produção.

Arquivos de banco SQLite e configurações locais/produção estão incluídos no `.gitignore`.

## Status

O projeto está em desenvolvimento. A base inicial da API, persistência SQLite, health check, versionamento, validação e tratamento global de exceções já estão configurados.

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

Na inicialização, a API aplica as migrations **somente** ao banco configurado em
`ConnectionStrings:Homevault`. A migration `AddShoppingList` cria uma casa padrão
de ID fixo (configurado em `ShoppingList:HomeId`, podendo ser sobrescrito por
configuração segura da instalação); a migration `AddShoppingCategoriesFriosAndPet`
completa suas onze categorias com IDs estáveis e ordem de exibição definida:
Hortifruti, Mercearia, Açougue e Peixaria, Higiene e Beleza, Laticínios,
Limpeza, Bebida, Padaria e Confeitaria, Frios e embutidos, Pet e Outros.
Não são criados produtos de exemplo.
Categorias usadas por itens devem ser desativadas (não excluídas); os itens
existentes continuam exibindo o nome da categoria desativada.

## Lista de compras

Todos os endpoints usam JSON e o prefixo `/api/v1/shopping-list`:

| Método | Rota | Resultado |
| --- | --- | --- |
| GET | `/categories` | Categorias ativas em ordem de exibição |
| GET | `/items?search=&categoryId=&page=1&pageSize=4` | Página filtrada, metadados e resumo global |
| GET | `/items/{id}` | Item individual (destino do `Location` de criação) |
| POST | `/items` | Cria item; retorna 201 e `Location` |
| PATCH | `/items/{id}` | Atualiza parcialmente nome, quantidade, categoria e/ou comprado |
| DELETE | `/items/{id}` | Remove item; retorna 204 |
| DELETE | `/items?purchased=true` | Remove apenas comprados; retorna quantidade removida e resumo |
| GET | `/export-data` | Snapshot JSON completo, resumo e `generatedAtUtc` |

O POST recebe `name` (texto de 1 a 80 caracteres após trim), `quantity` (inteiro
de 1 a 999) e `categoryId` (UUID de categoria ativa). O PATCH aceita um ou mais
dos campos `name`, `quantity`, `categoryId` e `purchased`; campos omitidos
permanecem inalterados. IDs, estado comprado inicial e timestamps são gerados
pelo servidor. Nomes duplicados não são mesclados.

A busca por nome ignora maiúsculas/minúsculas, mas distingue acentos. A ordem é
`createdAtUtc`, depois `id`; `page` começa em 1, `pageSize` vai de 1 a 100,
e páginas acima do final retornam a última página. Mesmo com filtros ou sem
resultados, `summary` descreve toda a casa. A resposta de `/export-data`
inclui todos os itens, independentemente de filtro/página: CSV, PDF e Markdown
continuam sendo gerados no navegador. Entradas inválidas retornam 400 em
`application/problem+json`; itens ausentes ou de outra casa retornam 404.
Os contratos e respostas também estão publicados no Swagger do perfil dev.

O frontend deve carregar `/categories` para o formulário **e** o filtro, usar
`categoryId` nas requisições e carregar `/items` ao entrar/voltar à tela e
após mutações. Não utilizar fixtures se a API estiver indisponível. Este
repositório contém apenas o backend: a integração React deve ser realizada
no repositório do frontend.

### Uso privado e CORS

Ainda não há autenticação nem vínculo de usuários a casas. A lista usa apenas
a casa configurada no backend; o cliente não informa `householdId`. O valor
padrão corresponde à casa criada na migration; outra casa configurada precisa
existir no banco e possuir suas próprias categorias.
**Não exponha esta API à internet nem configure encaminhamento de portas no
roteador.** Autenticação/autorização por vínculo com a casa são pré-requisitos
para uso público ou multiusuário/multicasa.

As origens permitidas para o frontend estão em `Cors:FrontendOrigins` nos
arquivos `appsettings.json` e `appsettings.Development.json` (Vite local e
nomes LAN existentes). Configure apenas origens efetivamente usadas pela
instalação, ou use proxy reverso de mesma origem; não habilite `*` com
credenciais. A API não determina endereço nem porta do frontend.

Para validar o backend:

```powershell
dotnet build
dotnet test Homevault.Tests/Homevault.Tests.csproj
dotnet format Homevault.slnx --verify-no-changes
```

## Afazeres pendentes

Os endpoints usam JSON camelCase e o prefixo `/api/v1/tasks`:

| Método | Rota | Resultado |
| --- | --- | --- |
| GET | `/` | 200 `{ "items": [...] }`, em ordem de criação/seed |
| POST | `/` | 201 com o afazer criado e `Location: /api/v1/tasks/{id}` |
| PATCH | `/{id}` | 200 com o afazer atualizado parcialmente |
| DELETE | `/{id}` | 204 sem corpo |

Cada afazer tem exatamente `id` (UUID), `title`, `meta`, `assignee`, `done` e
`repeatsDaily` (boolean). O POST recebe `title`, `meta`, `assignee` e
`repeatsDaily`, sendo os três últimos opcionais. `repeatsDaily` usa `false`
por padrão; `meta` e `assignee` omitidos ou nulos usam `"Sem prazo"` e
`"Todos"`. O servidor gera o ID e inicia `done=false`.
Os textos são aparados com trim: `title` deve ter de
1 a 120 caracteres, `meta` de 1 a 60 e `assignee` de 1 a 40.

O PATCH aceita apenas `title`, `meta`, `assignee`, `done` e `repeatsDaily`;
campos omitidos permanecem inalterados. Patch vazio, campos desconhecidos, valores nulos
explícitos e textos inválidos retornam 400. IDs malformados também retornam
400; PATCH e DELETE de afazeres inexistentes ou de outra casa retornam 404.
Os erros usam ProblemDetails/ValidationProblem em `application/problem+json`.
O `Location` identifica o recurso para PATCH/DELETE; não há GET individual.

### Repetição diária

Com `repeatsDaily=false`, a conclusão continua persistente. Com
`repeatsDaily=true`, `done` é calculado na leitura: só é `true` se a conclusão
ocorreu na mesma data local de agora. Ao mudar o dia, o afazer volta a pendente,
sem job em background e sem alterar o banco durante o GET. O fuso é obtido de
`Weather:Location:TimeZone` (configuração padrão `America/Sao_Paulo`); um fuso
inválido impede a inicialização. A conversão usa as regras de horário de verão,
não um deslocamento UTC fixo nem uma janela de 24 horas. Se a meia-noite for
pulada pelo horário de verão, o reset ocorre no primeiro instante da nova data.

PATCH `done=true` grava o instante UTC atual em `CompletedAtUtc`;
`done=false` limpa esse instante. Ele é interno e não aparece no JSON.
`repeatsDaily` aceita apenas booleanos; `null`, strings e números retornam 400
no POST e no PATCH.

Ao **alterar** `repeatsDaily` sem informar `done`, preserva-se o estado visível:
ativar a recorrência de um afazer concluído (mesmo há vários dias) mantém a
conclusão, registrando agora como conclusão do dia; desativá-la em um afazer
diário concluído mantém a conclusão persistente. Desativá-la em um afazer já
vencido mantém pendente e limpa a conclusão antiga. Reenviar o mesmo valor
de `repeatsDaily` não renova a conclusão. Se ambos vierem no PATCH, `done`
tem precedência. Alterar apenas textos nunca renova a conclusão.

A migration `AddTaskRecurrence` adiciona `RepeatsDaily=false` e
`CompletedAtUtc` opcional. Conclusões anteriores recebem o instante UTC da
migration; afazeres pendentes ficam com o instante nulo. IDs, ordem e conteúdo
do seed e dos afazeres existentes são preservados.

A migration `AddHomeTasks` cria a tabela e insere uma única vez os oito
afazeres iniciais, com UUIDs fixos, na ordem da tela; apenas regar as plantas
começa concluído. O seed não usa `HasData`, portanto migrations posteriores
não reescrevem edições do usuário. A ordem usa um `SortOrder` interno, não
exposto no JSON; novos afazeres são acrescentados ao final e atualizações
não mudam a posição. A atribuição da posição ocorre em transação SQLite.

Os afazeres usam a mesma casa configurada em `ShoppingList:HomeId` e as mesmas
restrições de uso privado descritas acima. O seed pertence à casa padrão;
outras casas começam sem afazeres. A política CORS existente permite PATCH
e DELETE nas origens autorizadas. Os contratos estão disponíveis no Swagger.

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

# PapirFly

REST API for registering and retrieving shop articles, based on [NET Developer.pdf](NET%20Developer.pdf).
The solution uses .NET 10, ASP.NET Core controllers, EF Core InMemory, Mapster, Serilog, and Alba + xUnit tests.

## Getting started

Install the .NET 10 SDK. `global.json` allows the latest installed feature band within SDK version 10.0.
No database server, Docker installation, or credentials are required.

```sh
dotnet restore PapirFly.sln --locked-mode
dotnet build PapirFly.sln --configuration Release --no-restore
dotnet run --project src/PapirFly.Api --configuration Release --no-build --urls http://localhost:5000
```

- Swagger UI: <http://localhost:5000/swagger/index.html>
- OpenAPI document: <http://localhost:5000/swagger/v1/swagger.json>
- Articles API: <http://localhost:5000/api/articles>

Requests within the same application instance share their data. All data is lost when the instance stops.

## Solution design

```text
src/
  PapirFly.Domain/            Article entity, field limits, ISO currency codes
  PapirFly.Application/
    Articles/                Use case handlers, shared validation and exceptions
      Commands/              One command per file
      Queries/               One query per file
    DTOs/                    ArticleInput and ArticleResponse
    Interfaces/              IArticleRepository
  PapirFly.Infrastructure/    EF Core context and repository implementation
  PapirFly.Api/               JSON, Problem Details, Serilog, Swagger and DI configuration
    Controllers/             ArticlesController and its HTTP actions
tests/
  PapirFly.UnitTests/         Validation rules and boundary cases
  PapirFly.IntegrationTests/  Full HTTP pipeline, persistence, OpenAPI and logging
```

### Clean Architecture

Project dependencies point towards the application and domain:

```mermaid
flowchart LR
    Api --> Application
    Api --> Infrastructure
    Infrastructure --> Application
    Application --> Domain
```

`Domain` has no project or package dependencies. `Application` does not depend on HTTP, ASP.NET Core,
EF Core, or a particular database. It uses `IArticleRepository`, which `Infrastructure` implements.
`Api` is the composition root: it registers implementations and translates HTTP requests into handler calls.
Its infrastructure reference is used for registration; the controller does not access the context.

The domain has one simple entity. Additional aggregate hierarchies, domain events, and service layers
are unnecessary for its current behavior.

DTOs live in `PapirFly.Application.DTOs`; persistence interfaces live in `PapirFly.Application.Interfaces`.
Command and query models have separate `PapirFly.Application.Articles.Commands` and
`PapirFly.Application.Articles.Queries` namespaces. Each command and query has its own file named after
the type. Handlers remain in `PapirFly.Application.Articles`.

### CQRS

Reads and writes have separate models and handlers:

| Operation | Model | Handler |
| --- | --- | --- |
| Create one article | `CreateArticleCommand` | `CreateArticleHandler` |
| Create a batch concurrently | `CreateArticlesCommand` | `CreateArticlesHandler` |
| Update an article | `UpdateArticleCommand` | `UpdateArticleHandler` |
| Read by ID | `GetArticleQuery` | `GetArticleHandler` |
| Search | `FindArticlesQuery` | `FindArticlesHandler` |

Create and update commands also serve as the write request contracts. The update ID is supplied
separately from the route. Queries read detached entities with `AsNoTracking` and return
`ArticleResponse` objects. Commands validate their inputs, write through the repository, and return
the stored representation.

CQRS separates responsibilities over a single store; it does not require separate databases or event
sourcing. The controller receives concrete handlers through constructor injection. No mediator,
reflection-based dispatcher, or generic request/response hierarchy is needed.

### Controller and HTTP boundary

`ArticlesController` derives from `ControllerBase` and uses `[ApiController]` with attribute routing.
Its five actions delegate to the application handlers and return `ActionResult<ArticleResponse>` or
`ActionResult<ArticleResponse[]>`. The batch action keeps the assignment's `/api/articles-concurrent` route.

MVC handles JSON deserialization and malformed-body errors. Shared application validation continues
to enforce the article rules. `ApiExceptionHandler` translates application validation, missing-article,
and concurrency failures into Problem Details responses. JSON settings belong to `AddControllers().AddJsonOptions`,
which also supplies the naming configuration used by Swagger.

### Repository pattern and EF Core InMemory

`IArticleRepository` exposes only operations needed by article use cases. It does not expose
`IQueryable`, `DbSet`, or `DbContext`. `ArticleRepository` implements both searches and writes; an additional
generic CRUD repository or Unit of Work wrapper would duplicate the existing EF Core responsibilities.

`IDbContextFactory<ArticlesDbContext>` creates a short-lived context for each operation. Contexts within
one host share an `InMemoryDatabaseRoot`, while separate hosts have separate stores. Concurrent operations
never share a context.

InMemory is the configured storage provider, as requested, replacing the PDF's optional SQL storage task.
It does not provide durability across runs or a transaction spanning a batch. A future SQL provider
would require registration changes, migrations, and verification of query translation: the current
`Contains(..., StringComparison.OrdinalIgnoreCase)` uses InMemory's query capabilities.

### Mapster and shared rules

Mapster maps input commands to `Article` and entities to `ArticleResponse`. Its configuration belongs
to the host and is compiled at startup. Input maps inherit the common `ArticleInput -> Article` mapping,
which ignores the server-managed ID and version. Updates map onto the loaded entity, including clearing
omitted optional fields.

`ArticleInput` shares the editable fields. `ArticleValidator` shares validation between single creates,
batches, and updates. Field length limits are declared once in the domain and reused by validation and
EF configuration. `ArticleResponse` is the output contract; entities are not returned directly over HTTP.

## API contract

| Method and route | Behavior | Responses |
| --- | --- | --- |
| `POST /api/articles` | Creates an article with a generated ID and version | `200`, `400` |
| `GET /api/articles/{articleId}` | Retrieves an article by ID | `200`, `404` |
| `GET /api/articles?name=...&category=...` | Searches for articles | `200`, including `[]` |
| `POST /api/articles-concurrent` | Stores an array of articles concurrently | `200`, `400` |
| `PUT /api/articles/{articleId}` | Replaces an article with a version check | `200`, `400`, `404`, `409` |

Creation returns `200 OK` to match the assignment. Errors use Problem Details; validation errors include
an `errors` dictionary. Unsupported request content types return `415`.

### Validation

| Field | Rule |
| --- | --- |
| `article_id` | Positive server-generated integer; must not be present in the request, even as `null` |
| `name` | Required, nonblank string of at most 64 characters |
| `description` | Required, nonblank string of at most 2048 characters |
| `category` | Optional string of at most 64 characters |
| `price` | Required JSON number `>= 0`, stored as `decimal`; numeric strings are rejected |
| `currency` | Uppercase ISO 4217 code; required for a positive price and may be blank when the price is zero |
| `version` | Server-generated UUID; the last read version is required for PUT and forbidden for POST |

Unknown JSON properties are rejected. Missing optional values are omitted from responses. A blank currency
for a free article is normalized to `null`; a nonblank currency is validated even when the price is zero.
The currency list is a snapshot of [SIX's official ISO 4217 List One](https://www.six-group.com/dam/download/financial-information/data-center/iso-currrency/lists/list-one.xml)
published on September 17, 2026. Update `CurrencyCodes.cs` when refreshing it. Runtime validation does not
rely on network access or platform-specific locale data.

The PDF's PUT example shows a numeric string, but its contract and validation example require a number.
The implementation consistently requires a JSON number for updates as well as creation.

### Search

- `name` matches a substring using ordinal, case-insensitive comparison.
- `category` matches the whole value, case-sensitively; the assignment only requires case-insensitive name matching.
- Supplied filters are combined with AND. Without filters, the API returns all articles ordered by ID.
- URL-encode values, for example `?name=branded&category=USB%20flash%20drive`.

### Concurrent insertion

The batch handler validates all articles before starting any writes. An invalid batch is rejected without
persisting any of its articles. Error keys include the item index, for example `[1].currency`. An empty
batch returns `[]`; a null item is invalid.

After validation, `Parallel.ForEachAsync` runs at most four writers, each with its own context. EF generates
the IDs. Responses preserve input order even if IDs are assigned in a different order. Cancellation is
propagated to outstanding operations. The no-writes guarantee applies to invalid input; storage failure or
cancellation during a valid batch may leave partial writes because InMemory has no batch transaction.

### Optimistic concurrency

1. The client reads an article and keeps its `version`.
2. A PUT request sends the complete replacement values and that version.
3. The handler validates the input and checks the loaded version. A missing ID returns `404`; a stale version returns `409`.
4. The repository sets EF's expected original version and generates a new version for the update.
5. `Version` is an EF concurrency token, so it is checked again when saving. This also protects the race
   between reading and writing. `DbUpdateConcurrencyException` becomes an application conflict and HTTP `409`.

After a conflict, the client must reload the article and decide how to reconcile its changes.

### Example

Send `POST /api/articles` with `Content-Type: application/json`:

```json
{
  "name": "Branded Memory Stick",
  "description": "Branded 16 GB memory stick",
  "category": "USB flash drive",
  "price": 17.89,
  "currency": "NOK"
}
```

Example `200 OK` response (the ID and UUID are illustrative):

```json
{
  "article_id": 1,
  "name": "Branded Memory Stick",
  "description": "Branded 16 GB memory stick",
  "category": "USB flash drive",
  "price": 17.89,
  "currency": "NOK",
  "version": "6893eac5-9bd1-4aa1-97f7-51c0d5d93d83"
}
```

For `PUT /api/articles/1`, send the editable fields and the `version` from the last response; leave
`article_id` in the URL only. Successful updates return a new version. The batch endpoint accepts an
array of create request objects and returns an array of `ArticleResponse` objects.

## XML documentation and Swagger

Public types and members, including methods in the test projects, have XML documentation. Method comments
describe behavior, parameters, return values, and relevant failures. Repository implementations reuse
the interface documentation through `<inheritdoc />`.

`GenerateDocumentationFile` is enabled solution-wide. The existing warnings-as-errors setting makes
missing public XML documentation (`CS1591`) fail the build. Generated XML files are placed beside the
assemblies. Swagger loads the API and application XML files to document controller actions and DTO properties.
Controller summaries, remarks, parameters, and response descriptions therefore come from the same comments
used by IDE tooling. The OpenAPI response schema is named `ArticleResponse`.

## Logging with Serilog

`Serilog.AspNetCore` handles application and ASP.NET Core logs. Services can use the standard injected
`ILogger<T>`. Configuration is read from the `Serilog` section of `src/PapirFly.Api/appsettings.json`:

- JSON events are written to the console with an `Application: PapirFly.Api` property.
- The default minimum level is `Information`; ASP.NET Core and EF Core sources are limited to `Warning`.
- `UseSerilogRequestLogging` records the HTTP method, request path, final response status, and elapsed time.
- The logger is owned by its application host and disposed with it. Request logging explicitly uses the
  same DI logger, so parallel test hosts do not interfere through a global static logger.
- `ReadFrom.Services` supports registered Serilog sinks, including the capturing sink used in integration tests.

The request logger wraps exception handling so it records the final status for handled failures.
Log levels can be overridden through configuration, for example `Serilog__MinimumLevel__Default=Debug`.

## Testing

```sh
# All tests
dotnet test PapirFly.sln --configuration Release

# Integration tests only
dotnet test tests/PapirFly.IntegrationTests --configuration Release
```

Alba runs the real ASP.NET Core controller pipeline through an in-memory TestServer. Tests use the actual
DI configuration, validation, Mapster, and EF repository. Raw JSON scenarios verify field names, numeric
values, error statuses, and persisted results without mocking handlers or storage.

A host is shared within each fixture-backed test class, and the article store is cleared before each
article test. Separate hosts have isolated stores. Coverage includes all five article operations,
invalid JSON, validation boundaries, search and URL encoding, batch validation before writes, concurrent
creates, update conflicts, host isolation, OpenAPI descriptions, Swagger UI, and structured Serilog events.
A separate detached-snapshot scenario checks EF concurrency enforcement independently of the handler's
preliminary version comparison. Unit tests cover validation rules and boundary values.

## GitHub Actions

[`.github/workflows/build.yml`](.github/workflows/build.yml) runs on pushes, pull requests, and manual dispatch.
On Ubuntu it:

1. Installs .NET 10 and restores the NuGet cache.
2. Runs `dotnet restore --locked-mode` against the committed `packages.lock.json` files.
3. Builds the solution in Release mode with compiler warnings treated as errors, including missing XML documentation.
4. Runs unit tests and then the full Alba integration suite. A test failure fails the build job.
5. Uploads available TRX results as the `test-results` artifact, including on test failure, with 14-day retention.

Package versions are centralized in `Directory.Packages.props`. After changing dependencies, run
`dotnet restore` and include the updated lock files with the change. CI needs no database service or
custom secrets and requests only `contents: read` permission.

## References

- [Alba: HTTP integration scenarios](https://jasperfx.github.io/alba/guide/gettingstarted.html)
- [Mapster: mapping configuration](https://github.com/MapsterMapper/Mapster/wiki/Configuration)
- [EF Core: optimistic concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [ASP.NET Core: controller-based web APIs](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0)
- [Serilog: ASP.NET Core integration](https://github.com/serilog/serilog-aspnetcore)
- [GitHub Actions: building and testing .NET](https://docs.github.com/en/actions/tutorials/build-and-test-code/net)

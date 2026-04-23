# JJGNet Broadcasting - Copilot Instructions

## Build, test, and lint

Run commands from `D:\Projects\jjgnet-broadcast`.

```powershell
dotnet restore .\src\
dotnet build .\src\ --no-restore --configuration Release
```

Use the CI-aligned test command for a normal repo-wide pass. It excludes the
network-dependent SyndicationFeedReader tests.

```powershell
dotnet test .\src\ `
  --no-build `
  --verbosity normal `
  --configuration Release `
  --filter "FullyQualifiedName!~SyndicationFeedReader"
```

Run a single test with a fully qualified name filter. This exact command was
verified against the repo:

```powershell
$testProject = `
  ".\src\JosephGuadagno.Broadcasting.Web.Tests\JosephGuadagno.Broadcasting.Web.Tests.csproj"
dotnet test $testProject `
  --no-build `
  --configuration Release `
  --filter "FullyQualifiedName~JosephGuadagno.Broadcasting.Web.Tests.Controllers.HomeControllerTests.Index_ShouldReturnView"
```

For Markdown edits, lint the files you changed:

```powershell
npx -y markdownlint-cli@0.41.0 .github\copilot-instructions.md
```

There is no separate repo-wide C# lint command defined in the workflows.

If you change `src\JosephGuadagno.Broadcasting.Web\libman.json` or the checked-in
files under `src\JosephGuadagno.Broadcasting.Web\wwwroot\libs`, run:

```powershell
cd .\src\JosephGuadagno.Broadcasting.Web
libman restore
```

## High-level architecture

- `src\JosephGuadagno.Broadcasting.AppHost\AppHost.cs` is the composition root.
  It starts SQL Server and Azurite, wires connection strings into the API, Web,
  and Functions projects, and creates the `JJGNet` database by concatenating
  `scripts\database\database-create.sql`,
  `scripts\database\table-create.sql`, and
  `scripts\database\data-seed.sql`.
- The shared application flow is
  **Domain -> Data/Data.Sql/Data.KeyVault -> Managers -> Api/Web/Functions**.
  Domain projects define models, interfaces, and constants; Managers contain
  business logic; entry-point apps consume those layers.
- The API (`src\JosephGuadagno.Broadcasting.Api`) is a bearer-token-protected
  ASP.NET Core API with OpenAPI/Scalar enabled in development. It registers
  `BroadcastingContext` plus its repositories and managers directly in
  `Program.cs`.
- The Web app (`src\JosephGuadagno.Broadcasting.Web`) is an ASP.NET Core MVC
  app with controllers and Razor views. It is not a Razor Pages app. It uses
  Microsoft Identity Web for sign-in, shared SQL store registration through
  `AddSqlDataStores()`, and the custom `UseUserApprovalGate()` middleware
  between authentication and authorization.
- The Functions app (`src\JosephGuadagno.Broadcasting.Functions`) handles
  collectors and publishers for feeds and social platforms. Aspire injects the
  same SQL, blob, table, and queue connections used by the other apps. Local
  Event Grid testing uses Azure Event Grid Simulator as described in
  `src\JosephGuadagno.Broadcasting.Functions\readme.md`.

## Repo-specific conventions

- Use Conventional Commits as documented in `CONTRIBUTING.md`.
- Database changes are script-first. Update SQL under `scripts\database\...`;
  do not use Entity Framework migrations. Seed data must stay idempotent because
  AppHost replays the creation script for fresh environments.
- Keep the layering intact: persistence belongs in `JosephGuadagno.Broadcasting.Data.Sql`,
  business logic belongs in manager classes, and the Web project should work
  through services/managers instead of calling data stores directly.
- Push paging, sorting, and filtering down into `JosephGuadagno.Broadcasting.Data.Sql`
  instead of implementing them in managers, controllers, or Razor code.
- Reuse the existing AutoMapper profiles. Shared SQL mappings live in
  `src\JosephGuadagno.Broadcasting.Data.Sql\MappingProfiles`; API and Web add
  their own profiles on top of those shared mappings.
- Use `DateTimeOffset` in C# and `datetimeoffset` in SQL for persisted date/time fields.
- Test projects live beside the production project with a `.Tests` suffix.
  xUnit, FluentAssertions, and Moq are the standard test stack.
- `SyndicationFeedReader` tests can fail because they hit
  `www.josephguadagno.net`. CI already filters them out; do not treat those
  failures as a general build break.
- In `RejectSessionCookieWhenAccountNotInCacheEvents.ValidatePrincipal`, reject
  the principal when the token cache is invalid; do not call `SignOutAsync()`
  there or you can create an auth redirect loop.
- Secrets belong in user secrets for the API, Web, and Functions projects.
  `src\JosephGuadagno.Broadcasting.Functions\local.settings.json` is a template,
  not a source of real credentials.

## Security baseline

### CSRF (`cs/web/missing-token-validation`)
- All `[HttpPost]` methods in the **Web** MVC project (`JosephGuadagno.Broadcasting.Web`)
  **must** have `[ValidateAntiForgeryToken]`.
- API controllers (`JosephGuadagno.Broadcasting.Api`) use Bearer token auth and are
  not vulnerable to CSRF. They **must** have `[IgnoreAntiforgeryToken]` at the class
  level; do **not** add `[ValidateAntiForgeryToken]` to API controllers.
- This is a **hard pre-commit gate**: any PR that adds a Web `[HttpPost]` without
  `[ValidateAntiForgeryToken]` will be rejected.

### Log injection (`cs/log-forging`)
- Never pass user-controlled strings (route params, query strings, request-body
  fields, model properties) directly into `_logger.Log*()` calls.
- Always sanitize using the centralized utility:
  ```csharp
  using JosephGuadagno.Broadcasting.Domain.Utilities;

  // ...
  _logger.LogWarning("Platform not found: {Platform}", LogSanitizer.Sanitize(platform));
  ```
- `LogSanitizer.Sanitize()` lives in `JosephGuadagno.Broadcasting.Domain.Utilities.LogSanitizer`
  and strips all ASCII control characters (0x00–0x1F, 0x7F) using a compiled regex.
- Do **not** add per-file inline `SanitizeForLog()` helpers; use the shared utility.
- This is a **hard pre-commit gate**: any PR introducing unsanitized user-controlled
  strings in log calls will be rejected.

### Manual production steps
- Any PR that requires a manual human action (DB migration, config change, new
  permission/role, secret rotation) **must** also create a GitHub issue with the
  `squad:Joe` label containing step-by-step instructions. Reference the issue in
  the PR description.

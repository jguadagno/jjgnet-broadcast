### Project Overview
`JosephGuadagno.Broadcasting` is a .NET 10 solution designed to manage and broadcast content to various social media platforms (LinkedIn, Facebook, Bluesky). It includes readers for Syndication Feeds, YouTube, and JSON feeds.

### Core Technologies
- **Runtime:** .NET 10
- **Orchestration:** .NET Aspire (AppHost)
- **Database:** SQL Server (EF Core + SQL Scripts for schema)
- **Storage:** Azure Storage (Tables/Blobs)
- **Testing:** xUnit, FluentAssertions, Moq
- **Mapping:** AutoMapper
- **Frontend:** ASP.NET Core MVC / Web

### Architectural Principles
- **Clean Architecture:**
  - `Domain`: Interfaces, Models, Enums. No dependencies.
  - `Data`: EF Core Context, DataStores, Repositories. Depends on Domain.
  - `Managers`: Business Logic. Depends on Data and Domain.
  - `Api` / `Web` / `Functions`: Entry points. Depends on Managers.
- **Repository Pattern:** Repositories wrap DataStores to provide a consistent interface.
- **Async First:** All IO-bound operations MUST be asynchronous.
- **Dependency Injection:** Always inject interfaces, not concrete implementations.
- **File-Scoped Namespaces:** Always use file-scoped namespaces (e.g., `namespace JosephGuadagno.Broadcasting.Api;`).

### Project Structure
- `src/JosephGuadagno.Broadcasting.Domain`: Core entities and interfaces.
- `src/JosephGuadagno.Broadcasting.Data`: Repository implementations.
- `src/JosephGuadagno.Broadcasting.Data.Sql`: EF Core implementation and SQL models.
- `src/JosephGuadagno.Broadcasting.Managers`: Business logic services.
- `src/JosephGuadagno.Broadcasting.Api`: REST API.
- `src/JosephGuadagno.Broadcasting.Web`: Web UI.
- `src/JosephGuadagno.Broadcasting.AppHost`: .NET Aspire orchestration.

### Coding Standards
1. **Naming:**
   - Use `PascalCase` for classes, methods, and properties.
   - Use `_camelCase` for private fields.
   - Test names should follow `Method_Scenario_ExpectedResult` pattern.
2. **Logic Placement:**
   - UI logic stays in Controllers/Views.
   - Business logic stays in Managers.
   - Persistence logic stays in DataStores.
3. **Database:**
   - Do NOT use EF Core Migrations. The database schema is managed via SQL scripts in the `scripts/` folder.
   - Update `scripts/database-create.sql`, `table-create.sql`, or `data-create.sql` when changing the schema.
4. **Testing:**
   - Write unit tests for Managers and Readers.
   - Use `FluentAssertions` for assertions.
   - Mock external dependencies (e.g., `IEngagementDataStore`).

### Common Commands
- **Run all tests:** `dotnet test`
- **Build solution:** `dotnet build`
- **Run AppHost:** `dotnet run --project src/JosephGuadagno.Broadcasting.AppHost`

### Operational Constraints
- **Plan First:** Always provide a plan before making significant changes.
- **Respect Context:** Check existing patterns in `EngagementManager` or `EngagementDataStore` before implementing new ones.
- **Clean Up:** Ensure no temporary files are left after execution.

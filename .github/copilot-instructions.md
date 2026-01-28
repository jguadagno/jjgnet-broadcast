# JJGNet Broadcasting - Developer Guide for Coding Agents

## Repository Overview

**Purpose**: Automated broadcasting app sharing blog posts and talks to Twitter, Facebook, LinkedIn, Bluesky. Collects from RSS/YouTube, distributes to social platforms.

**Stack**: .NET 10 Aspire app with API, Web (ASP.NET MVC/Razor), Azure Functions v4, SQL Server, Azure Storage. 289 C# files, 28 projects.

**Testing**: xUnit, FluentAssertions, Moq

## Critical Build Information

**ALWAYS run these commands in this exact order:**

### Initial Setup
```bash
cd /home/runner/work/jjgnet-broadcast/jjgnet-broadcast/src
dotnet restore    # Takes 5-10 seconds, expect NU1510 and NU1903 warnings (safe to ignore)
dotnet build      # Takes 30-45 seconds, expect 322 warnings (safe to ignore)
```

### Testing
```bash
cd /home/runner/work/jjgnet-broadcast/jjgnet-broadcast/src
dotnet test --no-build --verbosity normal
```
**IMPORTANT**: Some SyndicationFeedReader tests fail with network errors (external dependency on www.josephguadagno.net). This is EXPECTED and NOT a blocker. Tests requiring external network access may fail in CI/sandboxed environments.

### Clean Build (if needed)
```bash
cd /home/runner/work/jjgnet-broadcast/jjgnet-broadcast/src
dotnet clean
dotnet restore
dotnet build
```

### Web Project: Restore LibMan (client libraries)
```bash
cd src/JosephGuadagno.Broadcasting.Web
libman restore  # Install libman: dotnet tool install -g Microsoft.Web.LibraryManager.Cli
```

## Project Architecture

### Solution Structure (`src/JosephGuadagnoNet.Broadcasting.sln`)

**Entry Points:**
- `JosephGuadagno.Broadcasting.AppHost` - .NET Aspire orchestrator (defines infrastructure, starts all services)
- `JosephGuadagno.Broadcasting.Api` - REST API for managing content and schedules
- `JosephGuadagno.Broadcasting.Web` - ASP.NET Core MVC UI for managing engagements and scheduled items
- `JosephGuadagno.Broadcasting.Functions` - Azure Functions for collectors and publishers

**Core Libraries:**
- `JosephGuadagno.Broadcasting.Domain` - Domain models, interfaces, constants
- `JosephGuadagno.Broadcasting.Data` - Base data interfaces and table storage implementations
- `JosephGuadagno.Broadcasting.Data.Sql` - SQL Server repositories (EF Core)
- `JosephGuadagno.Broadcasting.Data.KeyVault` - Azure Key Vault integration
- `JosephGuadagno.Broadcasting.Managers` - Business logic for URL shortening, bitly
- `JosephGuadagno.Broadcasting.Serilog` - Logging configuration
- `JosephGuadagno.Broadcasting.ServiceDefaults` - Shared Aspire service defaults

**Collectors & Publishers:**
- Feed readers: SyndicationFeedReader (RSS/Atom), JsonFeedReader, YouTubeReader
- Social managers: Facebook, LinkedIn, Bluesky (all in `Managers.*` projects)

**Test Projects:** All test projects use xUnit, FluentAssertions, and Moq. Located in same directory as project being tested with `.Tests` suffix.

### Configuration Files

**Root Level:**
- `.editorconfig` (in `src/`) - Disables XML comment warnings (CS1591), sets code style
- `.gitignore` - Standard .NET patterns, excludes bin/, obj/, user secrets
- `CONTRIBUTING.md` - Conventional Commits specification (required for PRs)

**Project Configs:**
- User secrets stored in each project (Api, Web, Functions) - NOT committed
- `appsettings.json` / `appsettings.Development.json` - Per-project configuration
- `libman.json` (Web project only) - Client-side library management
- `local.settings.json` (Functions project) - Local Azure Functions settings

### Infrastructure (Auto-provisioned by Aspire)

**Database**: SQL Server `JJGNet` (scripts in `scripts/database/`) - Engagements, ScheduledItems, SourceData, Talks
**Storage**: Azurite emulator - Tables (Configuration, Logging), Queues (facebook-post-status-to-page, twitter-tweets-to-send, linkedin-*)
**No manual setup needed** - Aspire AppHost auto-starts SQL Server and Azurite containers

## CI/CD: Three workflows on push to `main`

1. **API**: `dotnet build ./src/JosephGuadagno.Broadcasting.Api --configuration Release` → Azure App Service `api-jjgnet-broadcast`
2. **Web**: `dotnet build ./src/JosephGuadagno.Broadcasting.Web --configuration Release` → Azure App Service `web-jjgnet-broadcast`
3. **Functions**: `dotnet build ./src/JosephGuadagno.Broadcasting.Functions --configuration Release --output ./output` → Azure Functions `jjgnet-broadcast`

All use .NET 10.x prerelease and Azure OIDC auth.

## Common Warnings (Safe to Ignore)

1. **NU1510**: Package reference pruning warnings - packages are marked as unnecessary but can be ignored
2. **NU1903**: Newtonsoft.Json 10.0.2 vulnerability - legacy dependency, tracked but not blocking
3. **NETSDK1206**: win7-x64 RID warnings for Microsoft.Azure.DocumentDB.Core - .NET 8+ compatibility notice
4. **CS8618**: Non-nullable property warnings in ViewModels - acceptable pattern for this codebase
5. **xUnit1051**: CancellationToken usage - test improvement suggestion, not critical

## Known Issues

- **SyndicationFeedReader tests fail** with network errors - EXPECTED, tests hit external URL (www.josephguadagno.net)
- **322 build warnings** - Safe to ignore (nullable refs, XML docs, package pruning)
- **Some tests need infrastructure** - Unit tests use mocks; integration tests need Aspire AppHost running

## Code Changes

**Testing**: `*.Tests` projects mirror source. Use FluentAssertions (`result.Should().NotBeNull()`), Moq (`Mock<IRepository>`).
**Dependencies**: `dotnet add package <name>`, check vulnerabilities, use .NET 10.0 compatible packages.
**Style**: 4-space indent, conventional commits (`feat:`, `fix:`), async/await for I/O, nullable refs enabled.

## Quick Reference: Key Files

**Must Read Before Changes:**
- `/README.md` - Project overview and plans
- `/developer-getting-started.md` - Setup requirements (Docker, SQL, Azurite, libman, ngrok)
- `/infrastructure-needs.md` - Full Azure infrastructure documentation
- `/CONTRIBUTING.md` - Commit message format

**Aspire AppHost:**
- `/src/JosephGuadagno.Broadcasting.AppHost/AppHost.cs` - Infrastructure definitions

**API Endpoints:**
- `/src/JosephGuadagno.Broadcasting.Api/` - Controllers, DTOs, Swagger

**Web UI:**
- `/src/JosephGuadagno.Broadcasting.Web/Controllers/` - MVC controllers
- `/src/JosephGuadagno.Broadcasting.Web/Views/` - Razor views
- `/src/JosephGuadagno.Broadcasting.Web/wwwroot/` - Static assets (requires libman restore)

**Azure Functions:**
- `/src/JosephGuadagno.Broadcasting.Functions/Collectors/` - Feed and YouTube collectors
- `/src/JosephGuadagno.Broadcasting.Functions/Twitter/` - Twitter integration
- `/src/JosephGuadagno.Broadcasting.Functions/Facebook/` - Facebook integration
- `/src/JosephGuadagno.Broadcasting.Functions/LinkedIn/` - LinkedIn integration

**Database Scripts:**
- `/scripts/database/database-create.sql`
- `/scripts/database/table-create.sql`
- `/scripts/database/data-create.sql`

## Trust These Instructions

These instructions are based on actual testing of the repository. Always follow the documented command sequences. If something doesn't work as documented, investigate the specific error before trying alternative approaches. The build process is stable and reliable when commands are run in the correct order from the correct directory.

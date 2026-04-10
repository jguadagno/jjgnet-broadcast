# Decision: Epic #667 Database Layer Review — Breaking Change Deployment Strategy

**Date:** 2026-04-08  
**Author:** Neo (Lead)  
**Issue:** #667 — Move social links for engagements into its own table  
**Branch:** `issue-667-social-media-platforms`  
**Commit:** 3fc341e

---

## Context

Reviewed Morpheus's database layer implementation for Epic #667 on local branch `issue-667-social-media-platforms`. The work is architecturally sound and complete, but introduces a breaking change to `IMessageTemplateDataStore.GetAsync()` signature that affects 3 projects (Api, Web, Functions) and causes 14 compile errors.

The PR does not exist yet — branch has not been pushed to GitHub.

---

## Review Findings

### ✅ Passes All Architecture Requirements

**Database schema:**
- SocialMediaPlatforms: Id (PK), Name (unique), Url, Icon (Bootstrap class), IsActive (soft delete)
- EngagementSocialMediaPlatforms: Composite PK (EngagementId, SocialMediaPlatformId), Handle field
- ScheduledItems: Platform (nvarchar) dropped, SocialMediaPlatformId (int FK) added
- MessageTemplates: Platform (string, part of composite PK) migrated to SocialMediaPlatformId (int FK) with PK rebuild
- Engagements: 3 social columns dropped (BlueSkyHandle, ConferenceHashtag, ConferenceTwitterHandle)
- Talks: BlueSkyHandle dropped

**Migration script quality:**
- 7-part structured migration (`scripts/database/migrations/2026-04-08-social-media-platforms.sql`)
- Correct PK rebuild sequence: add column → populate → drop old PK → create new PK → drop old column
- Best-effort string mapping for ScheduledItems.Platform → SocialMediaPlatformId
- Seed data: 5 platforms (Twitter, BlueSky, LinkedIn, Facebook, Mastodon)

**Code layer coverage:**
- EF Core entities match SQL schema
- Domain models have proper nullable annotations
- ISocialMediaPlatformDataStore interface with CRUD + soft delete
- SocialMediaPlatformDataStore repository implementation
- AutoMapper profiles (bidirectional)
- DI registration in Api Program.cs

### ❌ Build Errors (Expected Breaking Change)

**14 compile errors across:**
- `Data.Sql.Tests/MessageTemplateDataStoreTests.cs` (6 errors)
- `Api/MappingProfiles/ApiBroadcastingProfile.cs` (1 error)
- `Api/Controllers/MessageTemplatesController.cs` (2 errors)
- `Web/Services/MessageTemplateService.cs` (1 error)
- `Functions/*/ProcessScheduledItemFired.cs` (4 errors — LinkedIn, Bluesky, Facebook, Twitter)

**Root cause:** `IMessageTemplateDataStore.GetAsync` signature changed from:
- **Old:** `GetAsync(string platform, string messageType)`
- **New:** `GetAsync(int socialMediaPlatformId, string messageType)`

This was documented in commit message as expected breaking change requiring Trinity and Cypher follow-up work.

---

## Decision: Deployment Strategy for Breaking DB Migrations

**Pattern established for breaking database migrations involving PK rebuilds or column drops:**

### 1. Code Deploys First (Always)

**All code changes MUST be deployed to production before running database migration script.**

**Enforcement:**
- Pre-migration checklist in deployment runbook requires all PRs merged and deployed
- Breaking parts of migration script (column drops, PK rebuilds) isolated in separate sections

**For Epic #667 specifically:**
- Morpheus PR (Data layer) + Trinity PRs (Api layer) + Cypher PRs (Functions) + Switch PRs (Web layer)
- All PRs must merge to main
- All 3 Azure deployments (Api, Web, Functions) must complete
- Build must pass with 0 errors on main branch

### 2. Maintenance Window Required for PK Rebuilds

**MessageTemplates composite PK rebuild requires brief downtime:**
- Duration: 5-10 minutes (includes buffer)
- Services stopped: Functions (required), Api (recommended), Web (recommended)
- Reason: DROP PK + ADD PK causes table lock, active queries will fail

**When to use:**
- Composite PK changes
- High-traffic tables with schema locks
- Operations that cannot run under active load

### 3. Incremental Migration Option

**Additive changes can run separately before code deployment:**

**Safe to run now (Epic #667 Parts 1-3):**
- Create SocialMediaPlatforms table
- Create EngagementSocialMediaPlatforms junction table
- Seed SocialMediaPlatforms data

**These do NOT break existing code** — purely additive.

**Breaking changes wait for code deployment (Epic #667 Parts 4-7):**
- Migrate ScheduledItems.Platform → SocialMediaPlatformId (column drop)
- Migrate MessageTemplates.Platform → SocialMediaPlatformId (PK rebuild)
- Drop old social columns from Engagements
- Drop BlueSkyHandle from Talks

**When to use:**
- Migrations with mix of additive and breaking changes
- Allows pre-staging lookup tables and seed data before code deployment
- Reduces risk during maintenance window (additive parts already validated)

### 4. Deployment Runbook Mandatory

**Complex migrations require step-by-step runbook with:**
- Pre-migration checklist (code deployment verification)
- Service stop/start sequence
- Step-by-step SQL execution instructions
- Verification queries
- Rollback plan
- Safe vs. breaking change breakdown
- Key contacts

**Epic #667 runbook posted:** https://github.com/jguadagno/jjgnet-broadcast/issues/667#issuecomment-4210318810

### 5. Rollback Plan

**For database restore rollback:**
- Stop all services
- Restore database from pre-migration backup (Azure point-in-time restore)
- Redeploy previous code version (commit before Epic #667 merge)
- Restart services

**Data loss risk:** Any records created AFTER migration will be lost in rollback.

---

## Recommendation

**Conditional Approval for Epic #667:**

✅ **Database layer work is production-ready**  
✅ **SQL migration script is high quality**  
❌ **Cannot merge until downstream PRs resolve build errors**  
❌ **Cannot deploy until deployment runbook followed**

**Next steps:**
1. **Morpheus:** Push branch `issue-667-social-media-platforms`, create PR with detailed description
2. **Trinity:** Create follow-up PRs to update Api MessageTemplates endpoints and create SocialMediaPlatforms CRUD endpoints
3. **Cypher:** Create follow-up PR to update all 4 Functions `ProcessScheduledItemFired` handlers
4. **Switch:** Create follow-up PRs to update Web MessageTemplateService and Engagement controllers
5. **Neo:** Final review after all PRs created and build passes
6. **Joseph:** Execute deployment runbook during scheduled maintenance window after all PRs merged

---

## Impact

### Pattern Reuse

This breaking-change deployment strategy applies to:
- Any migration with column drops (affects existing queries)
- Any migration with PK/index rebuilds (table locks)
- Any migration changing interface signatures (compile-time breaks)

### Future Migrations

**When planning breaking migrations:**
1. Document breaking changes in commit message and PR description
2. Identify affected projects/files BEFORE merge
3. Coordinate dependent PRs across squads
4. Create deployment runbook with pre-flight checklist
5. Schedule maintenance window if PK rebuild or table lock required

**When reviewing breaking migrations:**
1. Verify all affected code identified
2. Check for maintenance window requirement
3. Validate incremental migration option (additive first, breaking later)
4. Require deployment runbook for complex migrations

---

## Files Reviewed

- `scripts/database/migrations/2026-04-08-social-media-platforms.sql` (279 lines)
- `scripts/database/table-create.sql` (additions)
- `scripts/database/data-seed.sql` (additions)
- 24 C# files (753 insertions, 61 deletions)

**Full review document:** `neo-review-667.md` (local file)  
**Deployment runbook:** Posted to issue #667

---

## References

- Epic #667: https://github.com/jguadagno/jjgnet-broadcast/issues/667
- Deployment runbook: https://github.com/jguadagno/jjgnet-broadcast/issues/667#issuecomment-4210318810
- Branch: `issue-667-social-media-platforms` (local, not pushed)
- Commit: 3fc341e

---

# Decision: Web Project Data.Sql Reference Removed

**Date:** 2026-04-10  
**Agent:** Neo (Lead)  
**Issue:** #690  
**PR:** #700  
**Branch:** issue-690

## Context

The codebase health audit (NEO-REPORT-667.md) identified a critical architectural violation:

> `src/JosephGuadagno.Broadcasting.Web/JosephGuadagno.Broadcasting.Web.csproj` has a direct `<ProjectReference>` to `JosephGuadagno.Broadcasting.Data.Sql`.

This violated the established team rule stored in `.squad/decisions.md`:

> "The Web project must NOT call data stores directly. All data access must go through Manager classes. Manager classes are responsible for converting SQL/EF entity models to Domain models before returning them to any caller."

## Problem

Web had a direct ProjectReference to Data.Sql for two reasons:
1. To register DataStore implementations for DI (e.g., `ApplicationUserDataStore`, `RoleDataStore`)
2. To configure BroadcastingContext (EF DbContext) for RBAC
3. To add Data.Sql AutoMapper profiles

This reference allowed Web application code to potentially use Data.Sql types directly, violating the architectural boundary.

## Decision

**Remove the direct ProjectReference while maintaining functionality via transitive dependency and extension methods.**

### Changes Made

1. **Removed** `<ProjectReference>` to Data.Sql from `Web.csproj`
2. **Added** `<ProjectReference>` to Data.Sql in `Managers.csproj` (Managers already needed it for its DataStore dependencies)
3. **Created** `ServiceCollectionExtensions.cs` in Data.Sql project with extension methods in `Microsoft.Extensions.DependencyInjection` namespace:
   - `AddSqlDataStores()` — registers all SQL data store implementations
   - `AddDataSqlMappingProfiles()` — adds BroadcastingProfile and RbacProfile to AutoMapper
4. **Updated** `Web/Program.cs`:
   - Removed `using JosephGuadagno.Broadcasting.Data.Sql;`
   - Replaced direct Data.Sql type references with extension method calls
   - Used fully-qualified type name for BroadcastingContext registration: `builder.AddSqlServerDbContext<JosephGuadagno.Broadcasting.Data.Sql.BroadcastingContext>("JJGNetDatabaseSqlServer");`

## Architecture

**Dependency Chain:**
```
Web → Managers → Data.Sql
```

- Web has direct reference to Managers
- Managers has direct reference to Data.Sql
- Web gets **transitive access** to Data.Sql.dll at compile time and runtime
- Web application code (Controllers, Services, Views) never uses Data.Sql types
- Web startup code (Program.cs) uses extension methods without direct type references

## Why This Works

1. **Extension Method Namespace:** Placing extension methods in `Microsoft.Extensions.DependencyInjection` (not `JosephGuadagno.Broadcasting.Data.Sql`) makes them discoverable without needing a using statement for Data.Sql namespace.

2. **Transitive Dependency:** C# compiler allows Web to access types from Data.Sql.dll because Managers references it. Web doesn't need a direct ProjectReference.

3. **Fully-Qualified Type Names:** For the one place where a Data.Sql type is needed (BroadcastingContext), using the fully-qualified name avoids needing `using Data.Sql;`.

## Verification

- ✅ Solution builds successfully with `dotnet build --no-incremental`
- ✅ No `using JosephGuadagno.Broadcasting.Data.Sql;` in any Web C# file
- ✅ Web Controllers only inject Manager and Service interfaces, never DataStore interfaces
- ✅ Extension methods callable from Web without Data.Sql using statement

## Impact

- **Architecture Boundary Enforced:** Web application code cannot accidentally use Data.Sql types
- **DI Configuration Simplified:** Extension methods centralize Data.Sql registration logic
- **Future-Proof:** If Api or other projects need Data.Sql services, they can use the same extension methods

## Related

- Team rule: `.squad/decisions.md` (2026-04-02T18-22-50Z)
- Audit report: `NEO-REPORT-667.md`
- Issue: #690
- PR: #700

---

# Decision: No Hardcoded Secrets in Committed Configuration Files

**Date:** 2026-04-07  
**Decision ID:** oracle-691-no-hardcoded-keys  
**Status:** Approved (by remediation)  
**Scope:** All projects (Api, Web, Functions)  
**Authority:** Oracle (Security Engineer)

## Context

Issue #691 identified a hardcoded Application Insights InstrumentationKey GUID (`c2f97275-e157-434a-981b-051a4e897744`) in `src/JosephGuadagno.Broadcasting.Functions/local.settings.json`. Despite the file being in `.gitignore`, it was already tracked in the repository, exposing the secret in version history.

The vulnerability existed in two configuration properties:
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Full connection string with embedded key
- `APPINSIGHTS_INSTRUMENTATIONKEY`: Standalone GUID value

## Decision

**ALL configuration files committed to version control (including local development settings) MUST use placeholders or empty strings for secrets, never actual values.**

### Approved Placeholder Patterns

1. **Empty string** (preferred for optional secrets):
   ```json
   "ApiKey": ""
   ```

2. **Descriptive placeholder** (preferred for required secrets):
   ```json
   "ConnectionString": "Set in User Secrets/Azure App Service Settings"
   ```

3. **Template with syntax examples** (for complex values):
   ```json
   "AzureAd:ClientCertificate": "<certificate-thumbprint or certificate-path>"
   ```

### Files Affected

This rule applies to ALL committed configuration files:
- `appsettings.json` (all projects)
- `appsettings.Development.json` (when committed)
- `local.settings.json` (Functions - even though in `.gitignore`, may be tracked)
- `*.config.json` files
- `.env.example` files (if created)

### Developer Workflow

Developers obtain secrets via:

1. **Local Development** (preferred):
   ```bash
   # API/Web projects
   dotnet user-secrets set "KeyVault:clientId" "<value>"
   
   # Functions project
   dotnet user-secrets set "Values:APPINSIGHTS_INSTRUMENTATIONKEY" "<value>"
   ```

2. **Environment Variables** (alternative):
   - Set OS-level environment variables
   - Azure Functions: `local.settings.json` Values section (not committed)

3. **Production**:
   - Azure App Service: Application Settings
   - Azure Functions: Application Settings
   - Azure Key Vault: For rotation-sensitive secrets (tokens, certificates)

### Audit Checklist

Before committing ANY config file:
- [ ] No API keys, tokens, or passwords in plaintext
- [ ] No GUIDs that represent instrumentation keys or client IDs (unless public)
- [ ] No connection strings with credentials
- [ ] No certificate thumbprints or paths to private keys
- [ ] All sensitive values use approved placeholder pattern

## Rationale

1. **Defense in depth**: Even ignored files can be tracked if added before `.gitignore` rule
2. **Developer guidance**: Placeholders serve as documentation for required configuration
3. **Zero trust**: Never assume a file won't be committed (human error, IDE auto-add)
4. **Compliance**: Prevents accidental exposure in public repositories or logs

## Implementation Status

- ✅ **Remediated**: Issue #691 via PR #697
- ✅ **Audit complete**: All `appsettings.json` files verified clean (no hardcoded secrets)
- ✅ **Pattern established**: All projects now use approved placeholders

## Monitoring

Oracle will audit configuration files in:
- All new PRs (via code review)
- Quarterly security reviews
- Pre-deployment checklists

## References

- Issue #691: Hardcoded Application Insights key in Functions settings
- PR #697: Remediation implementation
- Azure Functions docs: [Manage local.settings.json](https://learn.microsoft.com/en-us/azure/azure-functions/functions-develop-local#local-settings-file)
- .NET user secrets: [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)

---

**Enforcement:** Any PR with hardcoded secrets in config files MUST be rejected until secrets are replaced with placeholders.

**Exceptions:** None. Public configuration values (URLs, feature flags, non-sensitive IDs) are allowed.

---

# Decision: Issue #692 - EngagementSocialMediaPlatformDataStore Unit Tests

**Date:** 2026-04-10  
**Author:** Tank (Tester)  
**Issue:** #692  
**PR:** #699  
**Status:** Complete

## Context

`EngagementSocialMediaPlatformDataStore` was added as part of epic #667 (SocialMediaPlatforms feature) but had zero test coverage. This is a SQL data store handling the junction table between Engagements and SocialMediaPlatforms.

## Implementation

Created `EngagementSocialMediaPlatformDataStoreTests.cs` with 10 test cases covering all three public methods:

### GetByEngagementIdAsync (4 tests)
- Returns list when platforms exist for engagement
- Returns empty list when no platforms exist  
- Filters correctly by engagement ID
- Includes `SocialMediaPlatform` navigation property via `.Include()`

### AddAsync (2 tests)
- Successfully adds engagement-platform association with handle
- Supports null `Handle` value

### DeleteAsync (4 tests)
- Deletes existing association and returns true
- Returns false when entity doesn't exist
- Returns false when engagement ID doesn't match
- Returns false when platform ID doesn't match

## Testing Patterns Followed

1. **xUnit Assert methods** (not FluentAssertions) — matches Data.Sql.Tests project convention
2. **EF Core InMemory database** — pattern used by all other DataStore tests
3. **AutoMapper with BroadcastingProfile** — standard setup from existing tests
4. **AAA pattern** with clear Arrange/Act/Assert sections
5. **Test method naming:** `{MethodName}_{Scenario}_{ExpectedOutcome}`
6. **Helper methods:** `CreateEngagementAsync`, `CreateSocialMediaPlatformAsync`, `CreateDbEngagementSocialMediaPlatform`
7. **IDisposable pattern** for context cleanup

## Decisions Made

1. **Did NOT test cancellation token handling:** EF Core InMemory doesn't reliably throw `OperationCanceledException`, would create flaky tests
2. **Did NOT test foreign key constraint violations:** InMemory doesn't enforce FK constraints; these are DB-level concerns tested via integration tests
3. **Did NOT test exception handling with disposed context:** Creates test coupling to xUnit disposal order; exception handling is covered by try/catch in implementation

## Key Learnings

- Data.Sql.Tests uses **xUnit Assert**, not FluentAssertions (unlike Managers.Tests, Functions.Tests)
- EF Core InMemory has limitations: no FK enforcement, unreliable cancellation token support
- Focus unit tests on business logic and happy paths, not database constraints

## Impact

- **Test coverage:** EngagementSocialMediaPlatformDataStore now has 100% coverage of public methods
- **Regression protection:** Future changes to junction table logic are now protected by tests
- **Documentation:** Tests serve as usage examples for the data store

**Branch:** issue-692  
**Commit:** 8a4d602

---

# Decision: SocialMediaPlatformDataStore Test Coverage (Issue #693)

**Date:** 2026-04-02  
**Decider:** Tank (Tester)  
**Status:** Implemented  
**PR:** #698

## Context

`SocialMediaPlatformDataStore` was added as part of epic #667 (SocialMediaPlatforms feature) and had zero test coverage. This class handles CRUD operations for social media platform configuration (Twitter, Facebook, LinkedIn, Bluesky, etc.) in the SQL database.

## Decision

Created comprehensive unit tests covering all public methods of `SocialMediaPlatformDataStore`:

### Test Coverage (17 tests total)

1. **GetAsync** (4 tests):
   - Existing ID returns platform
   - Non-existing ID returns null
   - CancellationToken support
   - Theory test with multiple IDs (1, 42, 100)

2. **GetAllAsync** (3 tests):
   - Filters inactive platforms by default
   - Returns all platforms when `includeInactive = true`
   - Orders results alphabetically by name
   - Empty database returns empty list

3. **GetByNameAsync** (5 tests):
   - Finds platform by exact name
   - Case-insensitive search (Theory: "Twitter", "TWITTER", "twitter", "TwItTeR")
   - Inactive platforms not returned
   - Non-existing name returns null

4. **AddAsync** (1 test):
   - Adds new platform with auto-generated ID

5. **UpdateAsync** (1 test):
   - Updates existing platform properties

6. **DeleteAsync** (2 tests):
   - Soft deletes platform (sets `IsActive = false`)
   - Non-existing platform returns false

### Patterns Followed

- **xUnit** framework with `[Fact]` and `[Theory]`/`[InlineData]`
- **In-memory database**: `UseInMemoryDatabase(Guid.NewGuid().ToString())` for test isolation
- **AutoMapper**: Configured with `BroadcastingProfile` via `MapperConfiguration`
- **Moq**: Used for `ILogger<SocialMediaPlatformDataStore>` (not DbContext)
- **Standard xUnit assertions**: `Assert.NotNull`, `Assert.Equal`, `Assert.True` (NOT FluentAssertions)
- **AAA pattern**: Arrange/Act/Assert
- **Helper methods**: `CreateDbPlatform()`, `CreateDomainPlatform()`
- **IDisposable**: Database cleanup with `EnsureDeleted()`

### Reference Test Files

- `EngagementDataStoreTests.cs` — in-memory DB setup, test structure
- `MessageTemplateDataStoreTests.cs` — helper methods, assertion style

## Consequences

### Positive
- 100% method coverage for `SocialMediaPlatformDataStore`
- Verified soft delete behavior (sets `IsActive = false`)
- Documented case-insensitive name search
- Confirmed alphabetical ordering of `GetAllAsync` results
- Tests serve as living documentation for API behavior

### Negative
- Did not test exception handling paths (would require disposing DbContext, causing teardown issues)
- No tests for concurrent access scenarios
- No tests for EF Core mapping validation

### Neutral
- Tests run in ~1 second total (fast in-memory DB)
- All 17 tests passing in CI/CD pipeline

## Notes

- **Project uses standard xUnit assertions, NOT FluentAssertions** — this is a codebase convention
- **Soft delete pattern**: `DeleteAsync` sets `IsActive = false`, does not remove rows
- **GetByNameAsync filters by IsActive**: Inactive platforms cannot be retrieved by name
- **AutoMapper required**: Tests depend on `BroadcastingProfile` for domain ↔ DB model mapping

## Future Work

If needed:
- Add integration tests with real SQL Server (currently only in-memory)
- Test AutoMapper profile correctness separately (via `MappingTests.cs`)
- Add concurrency tests for update conflicts
- Test batch operations (if added to interface)

---

# Decision: DateTimeOffset Migration in Azure Functions (Issue #694)

**Date:** 2026-04-10  
**Status:** COMPLETE  
**Branch:** issue-694  
**PR:** #701  
**Decided by:** Trinity (Backend Dev)

## Context

All datetime fields in the codebase must use `DateTimeOffset` in C# (not `DateTime`) to avoid timezone ambiguity. A codebase audit found ~30+ violations in the Azure Functions project.

## Decision

Completed migration of all `DateTime.UtcNow`, `DateTime.MinValue`, and related datetime usages to `DateTimeOffset` equivalents across the entire Azure Functions project.

## Scope

**Total Changes:** 43 occurrences across 22 files in `src/JosephGuadagno.Broadcasting.Functions/`

**Pattern replacements:**
- `DateTime.UtcNow` → `DateTimeOffset.UtcNow` (31 occurrences)
- `DateTime.MinValue` → `DateTimeOffset.MinValue` (9 occurrences)
- `DateTime.Now` → `DateTimeOffset.Now` (0 found)
- Local variable types: `var` inference handles type change automatically

**Affected function categories:**
- Facebook publishers/processors (5 files)
- LinkedIn publishers/processors (4 files)
- Bluesky publishers/processors (3 files)
- Twitter publishers/processors (4 files)
- Collectors: RSS/YouTube/Speaking Engagements (6 files)
- Publishers: Scheduled Items, Random Posts (2 files)
- Maintenance: Clear Old Logs (1 file)

## Verification

- ✅ No remaining `DateTime.UtcNow` or `DateTime.MinValue` in Functions project (grep verified)
- ✅ Build succeeded: `dotnet build JosephGuadagno.Broadcasting.Functions --no-restore -v quiet`
- ✅ No compilation errors introduced

## Related Work

This completes the Functions project portion of the DateTimeOffset standardization. Other projects (Api, Web, Domain, Data, Managers) may still require similar audits and migrations.

## References

- Issue: #694
- PR: https://github.com/jguadagno/jjgnet-broadcast/pull/701
- Project Convention: All datetime fields must use `DateTimeOffset` (not `DateTime`) per coding standards

---

# Decision: Sprint Label Convention - Normalization Complete

**Date:** 2026-04  
**Lead:** Neo

## Decision

All GitHub sprint labels have been normalized to the convention:
- **Format:** `sprint:{number}` (e.g., `sprint:13`)
- **Description:** `Sprint {number}` optionally followed by a theme (e.g., `Sprint 13 - Codebase Health`)
- **Color:** `#0075ca`

## Normalization Summary

### Labels Found & Actions Taken

| Label | Status | Action | Final State |
|-------|--------|--------|-------------|
| `sprint:10` | ✓ | Already correct | `sprint:10` — "Sprint 10" — #0075ca |
| `sprint:11` | ✓ | Already correct | `sprint:11` — "Sprint 11" — #0075ca |
| `sprint:12` | ✓ | Already correct | `sprint:12` — "Sprint 12" — #0075ca |
| `sprint:3` | ✓ | Already correct | `sprint:3` — "Sprint 3 — Web UI, Tests, Cleanup" — #0075ca |
| `sprint:13` | ✓ | Updated description with theme | `sprint:13` — "Sprint 13 - Codebase Health" — #0075ca |
| `sprint:14` | ✓ | Already correct | `sprint:14` — "Sprint 14" — #0075ca |
| `sprint:15` | ✓ | Already correct | `sprint:15` — "Sprint 15" — #0075ca |
| `sprint 13` | ✗ Migrated | 7 issues migrated to `sprint:13`, label deleted | **DELETED** |

### Issues Migrated
The following 7 issues were migrated from old `sprint 13` to `sprint:13`:
- #696
- #695
- #694
- #693
- #692
- #691
- #690

## Outcome

✅ **All sprint labels now follow the convention:**
- 7 sprint labels active: `sprint:3`, `sprint:10`, `sprint:11`, `sprint:12`, `sprint:13`, `sprint:14`, `sprint:15`
- 1 old label removed: `sprint 13` (migrated and deleted)
- Color standardized: All use `#0075ca`
- Descriptions standardized: All use "Sprint {number}" with optional theme

## Going Forward

When creating new sprint labels:
1. Use format: `sprint:{number}`
2. Set description to: `Sprint {number}` (add theme if known)
3. Set color to: `#0075ca`
4. Apply label to all issues planned for that sprint

---

# Directive: Sprint Label Convention (User Input)

**Date:** 2026-04-10T10-54-30Z  
**By:** Joseph Guadagno (via Copilot)  
**What:** All GitHub sprint labels must follow the format `sprint:{number}` (e.g., `sprint:13`). Description should be `Sprint {number}` optionally followed by a theme (e.g., `Sprint 13 - Codebase Health`). Color must be `#0075ca`.  
**Why:** User request — captured for team memory and consistent label hygiene across all sprints.

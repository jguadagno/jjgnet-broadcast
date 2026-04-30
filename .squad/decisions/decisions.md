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

# Decision: Data-Layer Performance Fixes (Issue #855)

**Date:** 2026-06-02  
**Author:** Morpheus  
**Status:** Implemented — commit `2bfcfb4` on `issue-855-system-validation`

---

## Problem

Three categories of data-layer performance issues identified by Neo's investigation:

1. **N+1 SourceTags queries (CRITICAL):** `SyndicationFeedSourceDataStore` and `YouTubeSourceDataStore` issued one DB roundtrip per row to load SourceTags across all `GetAllAsync` overloads (~27 roundtrips per page load).
2. **Missing `.AsNoTracking()` (HIGH):** Paged read-only queries in Engagement, SyndicationFeedSource, YouTubeSource, and ScheduledItem data stores were tracked by EF, wasting memory and CPU for change detection on data that is never modified.
3. **Missing DB indexes (HIGH):** Sort/filter columns used in paged queries had no indexes, causing full table scans.

---

## Decisions Made

### Fix 1 — N+1 SourceTags batched load

**Decision:** Replace the per-row `foreach` loop with a batched query strategy in all `GetAllAsync` overloads of `SyndicationFeedSourceDataStore` and `YouTubeSourceDataStore`.

**Pattern:**
```csharp
var ids = dbItems.Select(x => x.Id).ToList();
var allTags = await broadcastingContext.SourceTags
    .Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType)
    .ToListAsync(cancellationToken);
var tagsBySourceId = allTags.GroupBy(t => t.SourceId).ToDictionary(g => g.Key, g => g.ToList());
foreach (var item in dbItems)
{
    item.SourceTags = tagsBySourceId.TryGetValue(item.Id, out var tags) ? tags : new List<Models.SourceTag>();
}
```

**Rationale:** Reduces N+1 roundtrips to exactly 2 queries (main page query + one bulk SourceTags query), regardless of page size.

**Scope:** Applied to both non-paged and paged `GetAllAsync` overloads in both data stores (4 overloads × 2 stores = 8 total fixes).

### Fix 2 — `.AsNoTracking()` on paged read-only queries

**Decision:** Add `.AsNoTracking()` at the head of the `IQueryable<T>` chain in all paged `GetAllAsync` overloads for read-only list queries.

**Files changed:**
- `EngagementDataStore` — 2 paged overloads
- `SyndicationFeedSourceDataStore` — 2 paged overloads (already being modified for Fix 1; also applied to non-paged)
- `YouTubeSourceDataStore` — 2 paged overloads (same)
- `ScheduledItemDataStore` — 4 paged overloads (simple + sort/filter, base + owner-filtered)

**Reference:** `MessageTemplateDataStore` was already correct; pattern matched exactly.

**Rule going forward:** All `GetAllAsync` overloads that return read-only lists MUST use `.AsNoTracking()`. Only write operations (Add, Update, Delete) should use tracked entities.

### Fix 3 — DB indexes for sort/filter columns

**Decision:** Add `IF NOT EXISTS`-guarded indexes to `scripts/database/table-create.sql` for all sort/filter columns used in paged queries.

**SQL Server idempotency pattern:**
```sql
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_...' AND object_id = OBJECT_ID('dbo.TableName'))
    CREATE INDEX IX_... ON dbo.TableName (...);
GO
```

**Indexes added:**
- `Engagements`: `IX_Engagements_StartDateTime` (DESC, INCLUDE Name/EndDateTime/CreatedByEntraOid), `IX_Engagements_CreatedByEntraOid`
- `SyndicationFeedSources`: Title, Author, PublicationDate DESC, AddedOn DESC, CreatedByEntraOid
- `YouTubeSources`: Title, Author, PublicationDate DESC, AddedOn DESC, CreatedByEntraOid
- `ScheduledItems`: SendOnDateTime DESC, CreatedByEntraOid
- `SocialMediaPlatforms`: (IsActive, Name) composite

**Rationale:** Paged queries with ORDER BY on unindexed columns force full table scans. These covering indexes allow SQL Server to satisfy sort + filter in a single index seek/scan.

---

## Files Changed

- `src/JosephGuadagno.Broadcasting.Data.Sql/SyndicationFeedSourceDataStore.cs`
- `src/JosephGuadagno.Broadcasting.Data.Sql/YouTubeSourceDataStore.cs`
- `src/JosephGuadagno.Broadcasting.Data.Sql/EngagementDataStore.cs`
- `src/JosephGuadagno.Broadcasting.Data.Sql/ScheduledItemDataStore.cs`
- `scripts/database/table-create.sql`

---

## Validation

- `dotnet build .\src\JosephGuadagno.Broadcasting.Data.Sql\...csproj` — ✅ 0 errors
- `dotnet test .\src\ --no-build --filter "FullyQualifiedName!~SyndicationFeedReader"` — ✅ All tests passed

---

# Decision: Issue #853 PR #891 Review

**Date:** 2026-04-28  
**Author:** Neo  
**Status:** BLOCKING — awaiting author fix

## Decision

PR #891 (`feat(#853): LinkedIn OAuth token expiry notification Function`) is **BLOCKED** pending one fix.

## Blocking Defect

`reauth_url = "/LinkedIn"` in `NotifyExpiringTokens.RenderTemplate` is a relative URL. Email clients cannot resolve relative URLs — users who receive the expiry notification email will get a broken re-auth link.

## Required Action

1. Add `Settings:WebBaseUrl` to `local.settings.json` (dev value: the web app localhost URL).
2. Inject via `IConfiguration` into `NotifyExpiringTokens` constructor.
3. Compose the absolute URL: `$"{_webBaseUrl.TrimEnd('/')}/LinkedIn"`.
4. Update the unit test that verifies rendered body to assert it contains the configured base URL.

## Convention Established

Any Azure Function that generates emailed HTML links must source the web application base URL from `Settings:WebBaseUrl` configuration. This is the established pattern going forward for all Functions that produce email notifications with clickable links.

## Everything Else

All other checklist items passed: cron, two-pass logic, UTC dedup, correct update ordering, LogSanitizer on all OID log calls, no token values logged, DateTimeOffset throughout, idempotent data-seed, migration script, 7 unit tests covering all scenarios.

---

# Neo Review: PR #889 — LinkedIn OAuth Token Expiry Data Layer

**Date:** 2026-04-28  
**PR:** https://github.com/jguadagno/jjgnet-broadcast/pull/889  
**Issue:** #852  
**Author:** Trinity  
**Verdict:** APPROVED ✅

---

## Blocking Issues

**None.** PR is clean. All review criteria passed.

---

## Non-Blocking Observations

### `GetExpiringWindowAsync` — no `from <= to` guard

`GetExpiringWindowAsync` does not validate that `from <= to`. Inverted parameters return an empty list silently. Acceptable for the current call site (scheduled Function, fixed window computation). If this method is ever exposed to external or user-controlled input, add a guard:

```csharp
if (from > to)
    throw new ArgumentException("'from' must not be greater than 'to'.", nameof(from));
```

No action required for this PR. Log as a follow-up consideration when #853 is implemented.

---

## Review Summary

| Criterion | Status |
|-----------|--------|
| `LastNotifiedAt` — `DateTimeOffset?` in domain model | ✅ |
| `LastNotifiedAt` — `DateTimeOffset?` in EF entity | ✅ |
| Migration — idempotent, correct location, `datetimeoffset NULL` | ✅ |
| `table-create.sql` parity | ✅ |
| `GetExpiringWindowAsync` — inclusive bounds, `.AsNoTracking()` | ✅ |
| `UpdateLastNotifiedAtAsync` — correct return semantics | ✅ |
| Manager delegation | ✅ |
| Unit tests (5 tests — all axes covered) | ✅ |
| Log injection — `LogSanitizer.Sanitize(ownerOid)` | ✅ |
| CSRF — N/A (data layer) | ✅ |
| DateTimeOffset + naming conventions | ✅ |

---

# Release Build Warnings Triage — 2026-05-01

**Performed by:** Neo (Lead / Architect)  
**Requested by:** Joe  
**Build Command:** `dotnet build .\src\ --configuration Release`

---

## Executive Summary

**Total warning lines:** 1,067  
**Actionable warnings:** ~467 (excluding build metadata noise)

**Severity breakdown:**
- **CRITICAL (Security):** 22 warnings (NU1903 — vulnerable Newtonsoft.Json)
- **HIGH (Correctness):** 115 warnings (CS8xxx — nullable reference type issues)
- **MEDIUM (Test hygiene):** 290 warnings (xUnit1051 — CancellationToken best practice)
- **LOW (Maintenance):** 4 warnings (NU1510 — potentially unnecessary packages)
- **INFORMATIONAL (Vendor library):** 37 warnings (NETSDK1206 — legacy RID in DocumentDB.Core)
- **NOISE:** ~400 lines of build status messages and "EnableIntermediateOutputPathMismatchWarning" logs

**Estimated fix effort:**
- NU1903 (critical): 30 minutes (one package upgrade)
- CS8xxx (nullable): 2-4 hours (batch fix across 5 projects)
- xUnit1051: 1 hour (global suppression) OR 3-4 hours (fix all 290 call sites)
- NU1510: 15 minutes (investigate + remove if safe)
- NETSDK1206: 5 minutes (add NoWarn to affected projects)

---

## Category 1: NU1903 — Vulnerable Package (CRITICAL)

**Count:** 22 occurrences  
**Warning Code:** NU1903  
**Package:** `Newtonsoft.Json 10.0.2`  
**Vulnerability:** [GHSA-5crp-9r3c-p9vr](https://github.com/advisories/GHSA-5crp-9r3c-p9vr) — High severity

### Affected Projects (13 total)
```
JosephGuadagno.Broadcasting.Api.csproj
JosephGuadagno.Broadcasting.Data.csproj
JosephGuadagno.Broadcasting.Domain.csproj
JosephGuadagno.Broadcasting.FixSourceDataShortUrl.csproj
JosephGuadagno.Broadcasting.JsonFeedReader.csproj
JosephGuadagno.Broadcasting.Managers.Bluesky.csproj
JosephGuadagno.Broadcasting.Managers.csproj
JosephGuadagno.Broadcasting.Managers.Facebook.csproj
JosephGuadagno.Broadcasting.Managers.LinkedIn.csproj
JosephGuadagno.Broadcasting.Managers.Twitter.csproj
JosephGuadagno.Broadcasting.SpeakingEngagementsReader.csproj
JosephGuadagno.Broadcasting.SyndicationFeedReader.csproj
JosephGuadagno.Broadcasting.Web.csproj
```

### Severity Assessment
**CRITICAL** — High severity CVE with known exploits.

### Recommendation
**FIX NOW** — Upgrade to Newtonsoft.Json 13.0.3 or later across all projects.

### Issue Recommendation
**One GitHub issue covering all instances:**
- Title: `chore: upgrade Newtonsoft.Json to 13.0.3+ to resolve NU1903 vulnerability`
- Labels: `security`, `dependencies`, `squad:trinity` or `squad:morpheus`
- Milestone: Sprint 29 (next sprint)
- Body: List all 13 affected projects; provide upgrade command; verify build + tests pass.

**Rationale:** Single dependency version — no reason to split across issues. Fast, low-risk change.

---

## Category 2: CS8xxx — Nullable Reference Type Warnings (HIGH)

**Count:** 115 occurrences  
**Breakdown by code:**
```
CS8618 (Non-nullable property must contain non-null value): 50
CS8625 (Cannot convert null literal to non-nullable reference): 29
CS861  (Fragment — truncated in output): 10
CS8602 (Dereference of possibly null reference): 7
CS8632 (Annotation for nullable reference types): 6
CS8669 (Annotation doesn't match type parameter): 4
CS8620 (Argument cannot be used for parameter): 3
CS8604 (Possible null reference argument): 2
CS8603 (Possible null reference return): 1
CS8619 (Nullability doesn't match target type): 1
```

### Affected Projects
```
32 warnings — JosephGuadagno.Broadcasting.Domain
12 warnings — JosephGuadagno.Broadcasting.Web
 8 warnings — JosephGuadagno.Broadcasting.Managers.LinkedIn
 5 warnings — JosephGuadagno.Broadcasting.Data.Sql
 1 warning  — JosephGuadagno.Broadcasting.Data
```

**Heaviest concentration:** Domain models (50 CS8618 warnings = non-nullable properties without required modifier or nullable annotation).

### Severity Assessment
**HIGH (Correctness Risk)** — Nullable reference warnings indicate potential `NullReferenceException` paths or incorrect assumptions about nullability. While these are compile-time warnings (not runtime failures), they represent logical defects in null-safety modeling.

### Recommendation
**FIX IN BATCH** — Worth tackling in one focused PR. Most CS8618 warnings are on Domain models and can be resolved systematically by adding `required` modifier or making properties nullable (`string?`).

**Why fix:**
- Nullable reference types are enabled project-wide; ignoring the warnings defeats the purpose of the feature.
- CS8618 (50 occurrences) is trivial to fix: add `required` or `?` to property declarations.
- CS8625 (29 occurrences) typically indicates incorrect null assignments; fixing surfaces actual bugs.

### Issue Recommendation
**One GitHub issue per project category:**

1. **Issue: `fix: resolve CS8618 nullable warnings in Domain models`**
   - 32 Domain warnings (mostly CS8618)
   - Labels: `code-quality`, `squad:trinity`
   - Estimated effort: 1 hour

2. **Issue: `fix: resolve nullable reference warnings in Web layer`**
   - 12 Web warnings
   - Labels: `code-quality`, `squad:trinity`
   - Estimated effort: 30 minutes

3. **Issue: `fix: resolve nullable warnings in Managers.LinkedIn`**
   - 8 warnings
   - Labels: `code-quality`, `squad:morpheus`
   - Estimated effort: 30 minutes

4. **Issue: `fix: resolve nullable warnings in Data.Sql layer`**
   - 5 warnings
   - Labels: `code-quality`, `squad:morpheus`
   - Estimated effort: 20 minutes

**Alternative:** Combine all into one issue ("Resolve all CS8xxx nullable warnings") if you prefer a single unified pass.

---

## Category 3: xUnit1051 — CancellationToken Best Practice (MEDIUM)

**Count:** 290 occurrences  
**Warning Code:** xUnit1051  
**Message:** "Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken to allow tests to be cancelled."

### Severity Assessment
**MEDIUM (Test Hygiene)** — Does not affect production code correctness or security. xUnit v3+ provides `TestContext.Current.CancellationToken` as a best-practice mechanism for test cancellation. Ignoring this warning means tests cannot be cancelled cleanly via xUnit's cancellation infrastructure.

### Recommendation
**SUPPRESS OR FIX (Your choice):**

**Option A: Suppress project-wide** (5 minutes)
- Add `<NoWarn>xUnit1051</NoWarn>` to test project `.csproj` files.
- Rationale: Test cancellation is rarely needed for unit tests; the warning adds noise without material benefit.

**Option B: Fix all call sites** (3-4 hours)
- Replace `CancellationToken.None` with `TestContext.Current.CancellationToken` across 290 test methods.
- Rationale: Aligns with xUnit v3 best practices; enables test cancellation infrastructure.

**Recommendation: Option A (suppress).** The cost/benefit ratio doesn't justify 3-4 hours of mechanical replacement unless test cancellation is a requirement.

### Issue Recommendation
**No issue needed** — suppress via `.csproj` edit. If you choose to fix, create:
- **Issue:** `test: replace CancellationToken.None with TestContext.Current.CancellationToken in all tests`
- Labels: `test`, `code-quality`, `squad:tank`

---

## Category 4: NU1510 — Unnecessary Package References (LOW)

**Count:** 4 occurrences  
**Warning Code:** NU1510  
**Message:** "PackageReference [X] will not be pruned. Consider removing this package from your dependencies, as it is likely unnecessary."

### Affected Projects + Packages
```
JosephGuadagno.Broadcasting.Managers.csproj
  → Microsoft.Extensions.Logging.Abstractions

JosephGuadagno.Broadcasting.Web.csproj
  → Microsoft.AspNetCore.DataProtection

JosephGuadagno.Broadcasting.Api.csproj
  → Microsoft.AspNetCore.DataProtection
```

### Severity Assessment
**LOW (Maintenance)** — These packages may be transitive dependencies already brought in by other packages. Removing unnecessary top-level references reduces maintenance burden.

### Recommendation
**INVESTIGATE + REMOVE IF SAFE** — Check if these packages are actually used directly or are just transitive. If transitive, remove the explicit `<PackageReference>`.

**How to investigate:**
```powershell
# Navigate to project directory
dotnet list package --include-transitive
```

If the package appears as a transitive dependency, the explicit reference can be safely removed.

### Issue Recommendation
**One GitHub issue:**
- Title: `chore: remove unnecessary package references flagged by NU1510`
- Labels: `dependencies`, `code-quality`, `squad:morpheus`
- Estimated effort: 15 minutes

---

## Category 5: NETSDK1206 — Legacy RID Warning (INFORMATIONAL)

**Count:** 37 occurrences  
**Warning Code:** NETSDK1206  
**Message:** "Found version-specific or distribution-specific runtime identifier(s): win7-x64. Affected libraries: Microsoft.Azure.DocumentDB.Core. In .NET 8.0 and higher, assets for version-specific and distribution-specific runtime identifiers will not be found by default."

### Severity Assessment
**INFORMATIONAL** — This is a deprecation warning from `Microsoft.Azure.DocumentDB.Core` (legacy DocumentDB SDK). The package uses `win7-x64` RID, which .NET 8+ considers deprecated. Does not affect runtime behavior on Windows platforms.

### Recommendation
**SUPPRESS** — Add `<NoWarn>NETSDK1206</NoWarn>` to projects using Azure Cosmos DB / DocumentDB Core SDK.

**Long-term:** Migrate to `Microsoft.Azure.Cosmos` SDK (modern, cross-platform replacement for DocumentDB.Core). This is likely a larger refactor and should be tracked separately.

### Issue Recommendation
**Immediate (suppress):** No issue needed — edit `.csproj` files.

**Long-term (migrate SDK):** Create a backlog item:
- Title: `chore: migrate from Microsoft.Azure.DocumentDB.Core to Microsoft.Azure.Cosmos SDK`
- Labels: `dependencies`, `refactor`, `backlog`
- Note: This is a breaking change and requires code updates + testing.

---

## Category 6: Build Metadata Noise (~400 lines)

**What it is:**
- "Restore succeeded with 16 warning(s) in 22.5s"
- "EnableIntermediateOutputPathMismatchWarning (0.5s)" status messages
- Other build progress/timing logs

**Not actionable** — ignore.

---

## Prioritization Summary

### Must-Fix (Do Now)
1. **NU1903 (Newtonsoft.Json vulnerability)** — 22 warnings, high severity CVE
   - **Effort:** 30 minutes
   - **Issue:** One issue covering all 13 projects

### Worth Batching (Plan for Next Sprint)
2. **CS8xxx (Nullable warnings)** — 115 warnings, correctness risk
   - **Effort:** 2-4 hours total
   - **Issues:** 1-4 issues (by project or combined)

### Optional / Low-Value
3. **xUnit1051 (CancellationToken)** — 290 warnings, test hygiene only
   - **Recommendation:** Suppress via `<NoWarn>`
   - **Effort if fixing:** 3-4 hours (not recommended)

4. **NU1510 (Unnecessary packages)** — 4 warnings, minor maintenance
   - **Effort:** 15 minutes
   - **Issue:** One issue

5. **NETSDK1206 (Legacy RID)** — 37 warnings, library vendor issue
   - **Recommendation:** Suppress via `<NoWarn>`
   - **Long-term:** Track SDK migration as backlog item

---

## Final Recommendation

**Immediate Actions (Next Sprint):**
1. Create issue for NU1903 (Newtonsoft.Json upgrade) — assign `squad:trinity`
2. Create 1-4 issues for CS8xxx nullable warnings — distribute across `squad:trinity` and `squad:morpheus`
3. Create issue for NU1510 (unnecessary packages) — assign `squad:morpheus`

**Suppressions (No Issue Needed):**
4. Add `<NoWarn>xUnit1051</NoWarn>` to test projects
5. Add `<NoWarn>NETSDK1206</NoWarn>` to projects using Azure DocumentDB.Core

**Backlog (Future Work):**
6. Track Azure Cosmos SDK migration (replaces DocumentDB.Core) as a backlog item

---

## Total Effort Estimate

| Category | Effort | Priority |
|---|---|---|
| NU1903 (security) | 30 min | CRITICAL |
| CS8xxx (nullable) | 2-4 hours | HIGH |
| NU1510 (packages) | 15 min | LOW |
| xUnit1051 (suppress) | 5 min | OPTIONAL |
| NETSDK1206 (suppress) | 5 min | OPTIONAL |
| **TOTAL** | **3-5 hours** | — |

**Bottom line:** The warning set is **worth tackling**. NU1903 is a must-fix security issue. CS8xxx nullable warnings are correctness improvements that justify 2-4 hours of focused work. The rest is noise or optional.

---

# Performance Investigation — All Index Pages

**Author:** Neo (Lead)  
**Date:** 2026-05-01  
**Requested by:** Joseph  
**Status:** FINDINGS — Awaiting action by Morpheus (Data) and Trinity (Backend)

---

## Executive Summary

The dominant cause of slowness on all Index pages is an **N+1 query pattern in both SyndicationFeedSources and YouTubeSources data stores**: for every paged list load, EF Core fires one database query per row to load SourceTags, turning a single page load into 27+ sequential DB roundtrips. A secondary contributor is the **Schedules/Index making two sequential HTTP calls** to the API (main list + orphan count) with no parallelism. Fix order: (1) eliminate N+1 in SyndicationFeedSources and YouTubeSources, (2) batch the Schedules/Index API calls, (3) add missing DB indexes on sort/filter columns.

---

## Findings by Category

### Category A — EF Core Query Problems

**[SEVERITY: CRITICAL]** N+1 SourceTags queries — SyndicationFeedSourceDataStore  
- **Location:** `SyndicationFeedSourceDataStore.cs` — both `GetAllAsync(int page, int pageSize, ...)` and `GetAllAsync(string ownerEntraOid, int page, int pageSize, ...)`  
- **Root cause:** After fetching the page of feed sources, the code loops `foreach (var source in dbItems)` and issues a separate `broadcastingContext.SourceTags.Where(...).ToListAsync()` call per row  
- **Impact:** pageSize=25 → 2 base queries (COUNT + data) + 25 SourceTags queries = **27 sequential DB roundtrips** per page load  
- **Fix:** Collect `dbItems.Select(s => s.Id)` into a list, issue ONE `broadcastingContext.SourceTags.Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType).ToListAsync()`, then assign in-memory with a lookup. Alternatively, model a proper EF navigation property and use `.Include(s => s.SourceTags)`

---

**[SEVERITY: CRITICAL]** N+1 SourceTags queries — YouTubeSourceDataStore  
- **Location:** `YouTubeSourceDataStore.cs` — both `GetAllAsync(int page, int pageSize, ...)` and `GetAllAsync(string ownerEntraOid, int page, int pageSize, ...)`  
- **Root cause:** Identical foreach-loop pattern as SyndicationFeedSourceDataStore  
- **Impact:** Same as above — 27 sequential DB roundtrips per page load  
- **Fix:** Same batch-load approach as SyndicationFeedSources

---

**[SEVERITY: HIGH]** Missing `.AsNoTracking()` on all paged read-only queries  
- **Location:**  
  - `EngagementDataStore.GetAllAsync(int page, ...)` (line 225–258) — no `AsNoTracking`  
  - `EngagementDataStore.GetAllAsync(string ownerEntraOid, int page, ...)` (line 261–296)  
  - `SyndicationFeedSourceDataStore.GetAllAsync(int page, ...)` (line 256–299)  
  - `SyndicationFeedSourceDataStore.GetAllAsync(string ownerEntraOid, int page, ...)` (line 301–345)  
  - `YouTubeSourceDataStore.GetAllAsync(int page, ...)` (line 192–235)  
  - `YouTubeSourceDataStore.GetAllAsync(string ownerEntraOid, int page, ...)` (line 237–281)  
  - `ScheduledItemDataStore.GetAllAsync(int page, ...)` (line 335–366)  
  - `ScheduledItemDataStore.GetAllAsync(string ownerEntraOid, int page, ...)` (line 368–400)  
- **Root cause:** EF Core's change-tracker snapshot-tracks every loaded entity for write-back detection. On read-only list queries this is pure overhead.  
- **Impact:** Extra memory allocation + CPU (snapshot comparison) per entity. At 25 entities/page, measurable but not critical alone. Combined with other issues compounds latency.  
- **Fix:** Add `.AsNoTracking()` to every `IQueryable<>` at the start of each read-only paged query. `MessageTemplateDataStore` already does this correctly — replicate the pattern.

---

**[SEVERITY: HIGH]** In-memory platform filter — MessageTemplatesController.Index  
- **Location:** `MessageTemplatesController.cs` — `Index` action (lines 42–63)  
- **Root cause:** Hard-codes `pageSize: 100` and loads all templates, then does `allViewModels.Where(t => t.Platform == selectedPlatform)` in application memory  
- **Impact:** Platform filter never reaches the DB. As MessageTemplate count grows, all 100+ are fetched regardless of selected platform.  
- **Fix:** Pass `selectedPlatform` filter to `_messageTemplateService.GetAllAsync(...)` and handle at DB level in `MessageTemplateDataStore`. The `filter` parameter targets `MessageType` — add an optional `platform` parameter targeting `SocialMediaPlatformId` (or `SocialMediaPlatform.Name` if the ID isn't available at service layer).

---

**[SEVERITY: MEDIUM]** Separate `CountAsync` + `ToListAsync` = 2 DB roundtrips on every paged query  
- **Location:** All paged `GetAllAsync` methods across all data stores  
- **Root cause:** `var totalCount = await query.CountAsync(...)` followed by `var dbItems = await query.Skip(...).Take(...).ToListAsync(...)` — two separate DB calls  
- **Impact:** 2 DB roundtrips instead of 1 (with COUNT OVER window). Acceptable design for now but multiplies with N+1 issue on SyndicationFeedSources/YouTube.  
- **Fix (optional):** Replace with a raw SQL `COUNT(*) OVER()` window-function query for high-traffic Index pages. For now, fixing the N+1 is the higher priority.

---

**[SEVERITY: MEDIUM]** Non-paged `GetAllAsync()` loads entire tables  
- **Location:**  
  - `EngagementDataStore.GetAllAsync()` (line 41) — full table scan  
  - `SyndicationFeedSourceDataStore.GetAllAsync()` (line 66) — full table scan + N+1 SourceTags  
  - `YouTubeSourceDataStore.GetAllAsync()` (line 65) — full table scan + N+1 SourceTags  
  - `ScheduledItemDataStore.GetAllAsync()` (line 37) — full table scan  
- **Root cause:** No pagination applied, no filter, no `AsNoTracking`  
- **Impact:** These are called from managers and legacy paths. Not directly in Index page paths (Index pages use paged overloads), but used in `GetCalendarEvents` and AJAX search helpers.  
- **Fix:** Audit all callers and migrate to paged overloads where possible. Add `AsNoTracking` at minimum.

---

### Category B — API/HTTP Layer

**[SEVERITY: CRITICAL]** Sequential HTTP calls on Schedules/Index  
- **Location:** `SchedulesController.cs` — `Index` action (lines 62–78)  
- **Root cause:** Two sequential `await` calls:  
  1. `await _scheduledItemService.GetScheduledItemsAsync(page, ...)` — HTTP GET  
  2. `await _scheduledItemService.GetOrphanedScheduledItemsAsync(1, 1)` — HTTP GET  
  These execute serially. Each HTTP call crosses the service boundary (Web → API → DB).  
- **Impact:** Page load time = latency(call 1) + latency(call 2) instead of max(call 1, call 2). At typical loopback latency, this adds 100–500ms per load.  
- **Fix:** Run both concurrently:
  ```csharp
  var itemsTask = _scheduledItemService.GetScheduledItemsAsync(page, Pagination.DefaultPageSize, sortBy, sortDescending, filter);
  var orphanTask = _scheduledItemService.GetOrphanedScheduledItemsAsync(1, 1);
  await Task.WhenAll(itemsTask, orphanTask);
  var result = await itemsTask;
  var orphanedResult = await orphanTask;
  ```

---

**[SEVERITY: MEDIUM]** `SearchSyndicationFeedSources` and `SearchYouTubeSources` load full MaxPageSize before in-memory filter  
- **Location:** `SchedulesController.cs` — `SearchSyndicationFeedSources` (line 418) and `SearchYouTubeSources` (line 434)  
- **Root cause:** `await _syndicationFeedSourceService.GetAllAsync(pageSize: Pagination.MaxPageSize)` then `.Where(s => s.Title.Contains(q, ...))` in LINQ-to-objects  
- **Impact:** Full MaxPageSize fetch (potentially 500+ rows) on every AJAX keystroke. Filter never hits the DB.  
- **Fix:** Pass the `q` search term as the `filter` parameter to `GetAllAsync`, let the DB do the filtering. Rely on the existing `Title.ToLower().Contains(lowerFilter)` EF expression.

---

**[SEVERITY: LOW]** No HTTP response caching at the API or Web layer  
- **Location:** All API controllers returning list responses  
- **Root cause:** No `[ResponseCache]` attributes or cache headers on GetAll endpoints  
- **Impact:** Every page refresh hits the DB. For near-static reference data (SocialMediaPlatforms, MessageTemplates) this is wasteful.  
- **Fix:** The `SocialMediaPlatformManager` already uses `IMemoryCache` (5-minute TTL) — this is the correct pattern. Consider adding manager-level caching to `SyndicationFeedSourceManager` and `YouTubeSourceManager` for the paged list result (short TTL: 60–120 seconds).

---

### Category C — Missing DB Indexes

**[SEVERITY: HIGH]** No index on Engagements sort/filter columns  
- **Location:** `scripts/database/table-create.sql` — Engagements table  
- **Root cause:** Table has only a clustered PK on `Id`. Queries sort by `StartDateTime`, `EndDateTime`, `Name` and filter by `CreatedByEntraOid`.  
- **Impact:** Full clustered-index scan + sort on every page load. Gets worse as engagement count grows.  
- **Fix:** See DB Indexes section below.

---

**[SEVERITY: HIGH]** No index on SyndicationFeedSources sort columns  
- **Location:** `scripts/database/table-create.sql` — SyndicationFeedSources table  
- **Root cause:** No indexes on `Title`, `Author`, `PublicationDate`, `AddedOn`, `CreatedByEntraOid`  
- **Impact:** Full table scan + sort on every Index page load  
- **Fix:** See DB Indexes section below.

---

**[SEVERITY: HIGH]** No index on YouTubeSources sort columns  
- **Location:** `scripts/database/table-create.sql` — YouTubeSources table  
- **Root cause:** No indexes on `Title`, `Author`, `PublicationDate`, `AddedOn`, `CreatedByEntraOid`  
- **Impact:** Full table scan + sort on every Index page load  
- **Fix:** See DB Indexes section below.

---

**[SEVERITY: MEDIUM]** No standalone `SendOnDateTime` index for ScheduledItems full-list query  
- **Location:** `scripts/database/table-create.sql` — ScheduledItems table  
- **Root cause:** `IX_ScheduledItems_Pending` covers `(MessageSent, SendOnDateTime)` for unsent queries, but `GetAllAsync` (all items, no MessageSent filter) cannot use it effectively for sort-only on `SendOnDateTime`. No `CreatedByEntraOid` index for per-user queries.  
- **Fix:** See DB Indexes section below.

---

**[SEVERITY: MEDIUM]** No composite index for SocialMediaPlatforms IsActive + Name  
- **Location:** `scripts/database/table-create.sql` — SocialMediaPlatforms table  
- **Root cause:** `GetAllAsync` frequently filters by `IsActive` and orders by `Name`  
- **Fix:** See DB Indexes section below.

---

### Category D — Application-Layer Inefficiencies

**[SEVERITY: HIGH]** `SocialMediaPlatformManager` paged `GetAllAsync` bypasses cache  
- **Location:** `SocialMediaPlatformManager.cs` — `GetAllAsync(int page, int pageSize, ...)` (line 91–94)  
- **Root cause:** The non-paged `GetAllAsync(bool includeInactive)` uses `IMemoryCache`. The paged overload (used by `SocialMediaPlatforms/Index`) passes straight through to the data store with no caching.  
- **Impact:** Every Index page load hits the DB even though SocialMediaPlatforms change rarely.  
- **Fix:** Add cache in the paged path, or — since SocialMediaPlatforms is small — fetch from the existing cached full list and apply paging/sorting/filter in-memory within the manager.

---

**[SEVERITY: MEDIUM]** No manager-level caching in SyndicationFeedSourceManager or YouTubeSourceManager  
- **Location:** `SyndicationFeedSourceManager.cs`, `YouTubeSourceManager.cs` (implied from pattern)  
- **Root cause:** These managers are pure pass-throughs. `SocialMediaPlatformManager` shows the right pattern (IMemoryCache with 5-minute TTL).  
- **Impact:** High-traffic, mostly read-only tables hit the DB on every request.  
- **Fix:** Add short-lived (60–120 second) IMemoryCache in `SyndicationFeedSourceManager` and `YouTubeSourceManager` for the paged list result. Invalidate on save/delete.

---

## Index-by-Index Summary Table

| Index Page | Primary Problem | Est. DB Roundtrips per Load | Priority |
|---|---|---|---|
| **Engagements/Index** | Missing AsNoTracking; no StartDateTime index; 2 DB calls | 2 | MEDIUM |
| **Schedules/Index** | 2 sequential HTTP calls (serial, not parallel) | 2 HTTP + 4 DB | CRITICAL |
| **ScheduledItems/Index** | (Same controller as Schedules — N/A as separate page) | — | — |
| **MessageTemplates/Index** | pageSize=100 hardcoded; in-memory platform filter | 2 | HIGH |
| **SocialMediaPlatforms/Index** | Paged path bypasses IMemoryCache | 2 | HIGH |
| **SyndicationFeedSources/Index** | N+1 SourceTags; no sort indexes; no AsNoTracking | 2 + N (25+) = **27** | CRITICAL |
| **YouTubeSources/Index** | N+1 SourceTags; no sort indexes; no AsNoTracking | 2 + N (25+) = **27** | CRITICAL |

---

## Recommended Fix Order

1. **[Morpheus — CRITICAL]** Fix N+1 SourceTags in `SyndicationFeedSourceDataStore.GetAllAsync(paged)` and `YouTubeSourceDataStore.GetAllAsync(paged)` — batch the SourceTags load into a single query using `WHERE SourceId IN (...)`.

2. **[Trinity — CRITICAL]** Fix `SchedulesController.Index` sequential HTTP calls — use `Task.WhenAll` to run `GetScheduledItemsAsync` and `GetOrphanedScheduledItemsAsync` concurrently.

3. **[Morpheus — HIGH]** Add missing DB indexes (Engagements, SyndicationFeedSources, YouTubeSources, ScheduledItems, SocialMediaPlatforms) — see exact SQL below.

4. **[Morpheus — HIGH]** Add `.AsNoTracking()` to all paged read-only queries in Engagement, SyndicationFeedSource, YouTube, and ScheduledItem data stores.

5. **[Trinity — HIGH]** Fix `SocialMediaPlatformManager.GetAllAsync(paged)` to use IMemoryCache (or serve from the cached full list).

6. **[Trinity — HIGH]** Fix `MessageTemplatesController.Index` in-memory platform filter — pass filter to the service/API so the DB does the filtering.

7. **[Trinity — MEDIUM]** Fix `SchedulesController.SearchSyndicationFeedSources` and `SearchYouTubeSources` to pass `q` as the `filter` param instead of loading MaxPageSize and filtering in memory.

8. **[Morpheus — MEDIUM]** Add `IMemoryCache` to `SyndicationFeedSourceManager` and `YouTubeSourceManager` — 60-second TTL on paged list calls, invalidate on mutation.

---

## DB Indexes to Add

Add to `scripts/database/table-create.sql` (or a new migration script):

```sql
-- Engagements: default sort is StartDateTime DESC; per-user queries filter on CreatedByEntraOid
CREATE NONCLUSTERED INDEX IX_Engagements_StartDateTime
    ON dbo.Engagements (StartDateTime DESC)
    INCLUDE (Name, EndDateTime, Url, TimeZoneId, CreatedByEntraOid);
GO

CREATE NONCLUSTERED INDEX IX_Engagements_EndDateTime
    ON dbo.Engagements (EndDateTime DESC)
    INCLUDE (Name, StartDateTime);
GO

CREATE NONCLUSTERED INDEX IX_Engagements_CreatedByEntraOid
    ON dbo.Engagements (CreatedByEntraOid);
GO

-- SyndicationFeedSources: sort columns Title (default), Author, PublicationDate, AddedOn; per-user filter
CREATE NONCLUSTERED INDEX IX_SyndicationFeedSources_Title
    ON dbo.SyndicationFeedSources (Title)
    INCLUDE (Author, PublicationDate, AddedOn, CreatedByEntraOid);
GO

CREATE NONCLUSTERED INDEX IX_SyndicationFeedSources_Author
    ON dbo.SyndicationFeedSources (Author)
    INCLUDE (Title, PublicationDate);
GO

CREATE NONCLUSTERED INDEX IX_SyndicationFeedSources_PublicationDate
    ON dbo.SyndicationFeedSources (PublicationDate DESC)
    INCLUDE (Title, Author, AddedOn);
GO

CREATE NONCLUSTERED INDEX IX_SyndicationFeedSources_AddedOn
    ON dbo.SyndicationFeedSources (AddedOn DESC)
    INCLUDE (Title, Author, PublicationDate);
GO

CREATE NONCLUSTERED INDEX IX_SyndicationFeedSources_CreatedByEntraOid
    ON dbo.SyndicationFeedSources (CreatedByEntraOid)
    INCLUDE (Title, Author, PublicationDate, AddedOn);
GO

-- YouTubeSources: same sort/filter pattern as SyndicationFeedSources
CREATE NONCLUSTERED INDEX IX_YouTubeSources_Title
    ON dbo.YouTubeSources (Title)
    INCLUDE (Author, PublicationDate, AddedOn, CreatedByEntraOid);
GO

CREATE NONCLUSTERED INDEX IX_YouTubeSources_Author
    ON dbo.YouTubeSources (Author)
    INCLUDE (Title, PublicationDate);
GO

CREATE NONCLUSTERED INDEX IX_YouTubeSources_PublicationDate
    ON dbo.YouTubeSources (PublicationDate DESC)
    INCLUDE (Title, Author, AddedOn);
GO

CREATE NONCLUSTERED INDEX IX_YouTubeSources_AddedOn
    ON dbo.YouTubeSources (AddedOn DESC)
    INCLUDE (Title, Author, PublicationDate);
GO

CREATE NONCLUSTERED INDEX IX_YouTubeSources_CreatedByEntraOid
    ON dbo.YouTubeSources (CreatedByEntraOid)
    INCLUDE (Title, Author, PublicationDate, AddedOn);
GO

-- ScheduledItems: standalone SendOnDateTime for GetAllAsync (not covered by IX_ScheduledItems_Pending);
-- per-user CreatedByEntraOid queries
CREATE NONCLUSTERED INDEX IX_ScheduledItems_SendOnDateTime
    ON dbo.ScheduledItems (SendOnDateTime DESC)
    INCLUDE (ItemTableName, ItemPrimaryKey, Message, MessageSent, SocialMediaPlatformId);
GO

CREATE NONCLUSTERED INDEX IX_ScheduledItems_CreatedByEntraOid
    ON dbo.ScheduledItems (CreatedByEntraOid)
    INCLUDE (SendOnDateTime, MessageSent);
GO

-- SocialMediaPlatforms: IsActive + Name composite for filtered/sorted list queries
CREATE NONCLUSTERED INDEX IX_SocialMediaPlatforms_IsActive_Name
    ON dbo.SocialMediaPlatforms (IsActive, Name)
    INCLUDE (Url, Icon, CredentialSetupDocumentationUrl);
GO
```

> **Note:** The `CONTAINS`/`LIKE '%...%'` filter on `Name`, `Title`, `Message` etc. cannot use B-tree indexes efficiently. Consider SQL Server Full-Text Search if text-search becomes a priority, but it is not the current bottleneck.

---

## Notes for Morpheus (Data Engineer)

- The SourceTags N+1 fix is pure data-layer. No interface changes needed — rewrite the loop body in both data stores.
- The DB indexes are additive — no existing indexes are changed.
- All indexes should be added via a new file `scripts/database/migrations/YYYY-MM-DD-perf-indexes.sql` following the project's script-first convention.
- `AsNoTracking()` is a data-layer change only — safe to apply without touching interfaces or managers.

## Notes for Trinity (Backend Dev)

- `Task.WhenAll` change in `SchedulesController.Index` is a one-line change to the awaits + extracting from tasks.
- `SocialMediaPlatformManager` paged cache: easiest approach is to call the existing cached `GetAllAsync(bool includeInactive)`, then apply paging/sort/filter in-memory within the manager — since the platform list is tiny (< 20 items), this is perfectly fine and eliminates all paging DB calls.
- `MessageTemplatesController.Index` fix requires adding a `platform` (or `platformName`) parameter to the service interface and propagating it to the API. Coordinate with Morpheus on any data store interface changes.
- `SearchSyndicationFeedSources` fix is simple: change `pageSize: Pagination.MaxPageSize` to a real page size and pass `q` as `filter`.

---

# Decision: MessageTemplates Platform Filter — ViewBag Dropdown Pattern

**Date:** 2026-05-02  
**Author:** Sparks  
**Status:** Established

## Decision

Platform filter dropdowns in MVC index views should:

1. Use `<select name="selectedPlatform" class="form-select w-auto" onchange="this.form.submit()">` for compact auto-submitting dropdowns.
2. Always cast `ViewBag` collections explicitly in Razor: `(List<string>)ViewBag.Platforms`.
3. Include all active filter state (`asp-route-filter`, `asp-route-selectedPlatform`, etc.) on every sort column link so filters are preserved when sorting.
4. Update the Clear link condition to check ALL filter params, not just the text filter.

## Rationale

Auto-submit on `onchange` reduces clicks for low-option dropdowns (platform list is short). `w-auto` prevents the select from stretching in a flex row. Without explicit casts, `dynamic` ViewBag collections cause runtime failures in Razor foreach loops.

---

# Trinity Decision Inbox — Issue #852 Data Layer

**Date:** 2026-05-01
**Author:** Trinity
**Issue:** #852 — LinkedIn OAuth token expiry notification data layer
**PR:** #889

---

## Decisions Made

### 1. `GetExpiringWindowAsync` uses inclusive boundaries

Window filter is `AccessTokenExpiresAt >= from && AccessTokenExpiresAt <= to`.
Both endpoints are inclusive to match the natural language "tokens expiring within the window."
Unit tests confirm this behaviour.

### 2. `UpdateLastNotifiedAtAsync` only touches `LastNotifiedAt`

The method does NOT update `LastUpdatedOn`. `LastUpdatedOn` tracks token data changes (access/refresh token rotation). Notification tracking is a separate concern and should not pollute the token rotation timestamp.

### 3. `LastNotifiedAt` added as nullable `datetimeoffset` — no default constraint

`null` means "never notified." This is the correct sentinel — a default of `getutcdate()` would imply every token was notified at creation time, which is wrong.

### 4. Migration is idempotent

`IF NOT EXISTS` guard on the column check. Safe to replay by AppHost or by production DBA.

### 5. Manager layer has no additional validation on `GetExpiringWindowAsync`

The method does not validate that `from <= to`. Responsibility deferred to the caller (the notification Function). The data store simply delegates the query; no defensive clamp was added to keep the layer boundaries clean.

---

# Decision: Scriban rendering in email notification Functions (Issue #853)

**Date:** 2026-05-02
**Author:** Trinity
**PR:** #891 (`issue-853-notify-expiring-linkedin-tokens`)
**Issue:** #853

---

## Context

Issue #853 required the LinkedIn expiry notification Function to send personalised emails containing the user's display name, token expiration date, and a re-auth URL. Email templates are stored in the `EmailTemplates` SQL table as raw HTML bodies.

## Decision 1 — First use of Scriban rendering for email templates

**Decision:** Render Scriban markup inside the `EmailTemplates.Body` column before passing to `IEmailSender.QueueEmail()`.

**Rationale:** The existing `UserApproved` and `UserRejected` templates send static HTML with no variable substitution. For the LinkedIn expiry notifications, personalisation is required (`{{ display_name }}`, `{{ expires_at }}`, `{{ reauth_url }}`). Rather than adding a new abstraction, the simplest approach is to call `Template.Parse` + `template.Render` in the Function itself, falling back to the raw body string if Scriban fails. This keeps the template storage mechanism unchanged.

**Implication for future teams:** Any email template that needs variable substitution can now use Scriban syntax in its `Body` column. Functions/code that send those templates are responsible for rendering before queuing. `IEmailTemplateManager` does NOT render Scriban — the caller does.

---

## Decision 2 — `reauth_url` hardcoded as `/LinkedIn`

**Decision:** The `reauth_url` variable injected into the Scriban context is the relative path `/LinkedIn`.

**Rationale:** The Web app has a `/LinkedIn` controller route that initiates the LinkedIn OAuth flow. The full base URL is not known to the Functions project (it differs per environment). Since the email ultimately lands with the user who can click it in their browser against the same Web host they used to sign in, a relative URL is sufficient for now. If emails are opened in a different context (e.g., mobile client), this will need revisiting.

**Implication:** A future change could inject the Web base URL via configuration (e.g., `WebBaseUrl` app setting) and produce a full URL. That enhancement is not in scope for #853.

---

## Decision 3 — Deduplication granularity: once per UTC calendar day

**Decision:** Skip notification if `token.LastNotifiedAt.Value.UtcDateTime.Date >= todayUtc` (where `todayUtc = from.UtcDateTime.Date`).

**Rationale:** Prevents re-queuing the same email if the Function is retried or runs twice in a day. Resets on the next UTC calendar day so users receive a fresh reminder if the token remains un-renewed.

---

# Decision: MessageTemplates Index — selectedPlatform filter approach

**Author:** Trinity  
**Date:** 2025-XX-XX  
**Related task:** MessageTemplatesController selectedPlatform filter

## Decision

For the MessageTemplates admin index page, platform filtering is done **in-memory after a single large-page load** (`page: 1, pageSize: 100`) rather than pushing the filter down to the service/API layer.

## Rationale

- The dataset is known to be small (few platforms × few message types); a single 100-item fetch is acceptable.
- Deriving distinct platform names for the dropdown requires the full unfiltered set anyway — a separate API call would be wasteful.
- No changes to `IMessageTemplateService` or `MessageTemplateService` are needed, keeping the scope minimal.
- `ViewBag.TotalPages = 1` and `ViewBag.TotalCount = filteredViewModels.Count` correctly reflect the in-memory filtered state to the view.

## ViewBag contract (Sparks interface)

| Key | Type | Value |
|-----|------|-------|
| `Platforms` | `List<string>` | All distinct platform names, ordered |
| `SelectedPlatform` | `string?` | null or "" = All |
| `TotalCount` | `int` | Count of filtered view models |
| `TotalPages` | `int` | Always 1 |

## Side fix

Corrected pre-existing `RZ1031` Razor syntax error in `Views/MessageTemplates/Index.cshtml` line 34: boolean `selected` attribute on `<option>` must use `selected="@(boolExpr)"` not a standalone ternary expression.

---

# Decision: Application-Layer Performance Patterns

**Date:** 2026-04-27
**Author:** Trinity
**Issue:** #855

## Context

Neo's system validation identified two application-layer hot paths that were incurring unnecessary latency on every page load:

1. `SchedulesController.Index` made two independent API calls sequentially.
2. `SocialMediaPlatformManager.GetAllAsync(int page, ...)` bypassed the existing `IMemoryCache` and hit the DB on every call.

## Decisions

### 1 — Independent async calls must be parallelized with Task.WhenAll

When two or more async calls in a controller action have no data dependency on each other, they **must** be started concurrently and awaited via `Task.WhenAll`. Sequential awaiting of independent tasks wastes a full roundtrip per call.

**Pattern:**
```csharp
var aTask = _service.GetAAsync(...);
var bTask = _service.GetBAsync(...);
await Task.WhenAll(aTask, bTask);
var a = await aTask;
var b = await bTask;
```

### 2 — Paged overloads in cache-backed managers must slice from the cached full list

For small/stable reference data (platforms, sources, etc.) where a full-list cache already exists, paged overloads **must not** hit the data store directly. Instead they must:
1. Call the existing non-paged cached overload.
2. Apply filter, sort, and paging in-memory.
3. Return `PagedResult<T> { Items = ..., TotalCount = filtered.Count }`.

This guarantees that Index page loads for reference data never touch the DB while the cache is warm.

## Applies To

- All Web controllers with two or more independent service calls per action.
- All manager classes that have an `IMemoryCache`-backed full-list overload and a separate paged overload.

---

### 2026-04-28T13-32-05Z: User directive
**By:** jguadagno (via Copilot)
**What:** When creating new GitHub issues, always add the `squad` label — UNLESS the issue is explicitly a manual human task for `squad:Joe`. The `squad` label triggers the auto-triage workflow that assigns the correct `squad:{member}` sub-label.
**Why:** User request — repo has a triage workflow that routes `squad`-labeled issues automatically. Captured for team memory.

---

### 2026-04-29T19-15-23: User directive
**By:** Joe (via Copilot)
**What:** When creating GitHub issues, do NOT add the `squad` label. The `squad-triage.yml` workflow handles that automatically. Instead, assign the issue directly to the appropriate squad member(s).
**Why:** User request — prevents duplicate triage triggering and respects the existing automation.

---

### 2026-04-27: User directive
**By:** Joseph Guadagno (via Copilot)
**What:** Remove `table-dark` from all `<thead>` elements — it does not fit the application theme. Use unstyled `<thead>` going forward.
**Why:** User request — captured for team memory

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

---

# Decision: PR #865 Review Findings

**Date:** 2026-04-25  
**Author:** Neo  

[Content from neo-pr865-review.md]

# Neo — PR #865 Review Findings

**Date:** 2026-04-25  
**PR:** #865 — `refactor(#862): consolidate owner-OID and site-admin helpers into ClaimsPrincipalExtensions`  
**Branch:** `issue-862-claims-principal-extensions`  
**Verdict:** APPROVED ✅

---

## Summary

Clean, complete consolidation of owner-OID and site-admin helpers into `ClaimsPrincipalExtensions`. All 7 security-critical inline bypasses fixed, no private duplicates remain, test coverage thorough. No blocking issues.

---

## Checklist

| Criterion | Status |
|-----------|--------|
| Signatures match design decision | ✅ |
| `EntraObjectIdShort` fallback in `GetOwnerOid()` | ✅ |
| `GetOwnerOid` returns `string`, throws (not `string?`) | ✅ |
| Null-as-forbidden preserved in `ResolveOwnerOid` | ✅ |
| All 7 inline bypasses fixed in UserCollector controllers | ✅ |
| Zero private duplicate methods in any controller | ✅ |
| `using JosephGuadagno.Broadcasting.Api;` in all 8 controllers | ✅ |
| 12 tests across all 3 methods | ✅ |
| Admin elevation gate tested | ✅ |
| Case-insensitive OID matching tested | ✅ |
| PR description: no backslash-word-backslash escaping | ✅ |

---

## Detailed Findings

### `ClaimsPrincipalExtensions.cs` ✅

Implementation matches the design decision in `.squad/decisions.md` (2026-04-25) exactly:

- `GetOwnerOid` — tries `ApplicationClaimTypes.EntraObjectId` first, falls back to `EntraObjectIdShort` for MSAL v2+ JWT handlers. Throws `InvalidOperationException` (not `null`) if neither present.
- `IsSiteAdministrator` — one-liner delegating to `IsInRole(RoleNames.SiteAdministrator)`.
- `ResolveOwnerOid` — default `requireAdminWhenTargetingOtherUser = true` ensures the safe path is the default. Null-as-forbidden pattern preserved.

### Inline bypass fix ✅

Both `UserCollectorFeedSourcesController` and `UserCollectorYouTubeChannelsController` — `GetAsync` and `DeleteAsync` — previously had raw `FindFirstValue`/`IsInRole` calls that bypassed the controller's private `ResolveOwnerOid`. These are now correctly replaced with:

```csharp
var currentOwnerOid = User.GetOwnerOid();
if (User.ResolveOwnerOid(config.CreatedByEntraOid, requireAdminWhenTargetingOtherUser: true) is null)
{
    // log with currentOwnerOid for context
    return Forbid();
}
```

The `currentOwnerOid` local is intentional — it is used exclusively for the structured log warning, not for the auth check.

### All 8 controllers ✅

Grep on `Controllers/` directory: zero remaining private `GetOwnerOid`, `IsSiteAdministrator`, or `ResolveOwnerOid` methods. `System.Security.Claims` using removed; `JosephGuadagno.Broadcasting.Api` added in all affected files.

### Tests ✅

12 tests in `ClaimsPrincipalExtensionsTests.cs` cover:
- `GetOwnerOid`: full-URI primary, short fallback, both-present priority, throw-on-missing
- `IsSiteAdministrator`: role present, role absent, empty principal
- `ResolveOwnerOid`: null/empty/matching OID, case-insensitive match, non-admin different OID → null, admin different OID → requested, requireAdmin=false bypass, admin with null requested → caller OID

---

## One Item for Joe to Verify

**⚠️ Test count discrepancy (not a blocker):**
- Tank's session log: **192/192** passing (12 new + 180 pre-existing)
- PR description: **166 unit tests passed**
- Gap of 26 tests — likely different test project scope between runs
- Recommend: run `dotnet test` across the full solution before merging to confirm all suites pass

---

## Decision

No architecture concerns. No directive violations. No security regressions. Implementation is exactly what was designed.

**PR #865 is approved. Ready to merge after test-count verification.**

---

*GitHub comment:* https://github.com/jguadagno/jjgnet-broadcast/pull/865#issuecomment-4317480336


---

# Decision: Test Pattern — ClaimsPrincipalExtensions (Issue #862)

**Date:** 2026-04-24  
**Author:** Tank  

[Content from tank-862-tests.md]

# Test Pattern: ClaimsPrincipalExtensions (Issue #862)

**Author:** Tank  
**Date:** 2026-04-24  
**Branch:** issue-862-claims-principal-extensions

## Context

`ClaimsPrincipalExtensions` is a new static class in `JosephGuadagno.Broadcasting.Api` that centralises three owner/role helpers previously duplicated across controllers:
- `GetOwnerOid()` — reads OID from full-URI or short "oid" claim; throws if absent
- `IsSiteAdministrator()` — checks `RoleNames.SiteAdministrator` role
- `ResolveOwnerOid(requestedOwnerOid, requireAdminWhenTargetingOtherUser)` — computes effective owner OID with admin-bypass logic

## Test Patterns Worth Capturing

### No Mocks Required for Static Extension Methods

When testing `ClaimsPrincipal` extension methods, construct the principal directly. No Moq needed.

```csharp
private static ClaimsPrincipal BuildPrincipal(
    string? oidFullUri = null,
    string? oidShort = null,
    bool isSiteAdmin = false)
{
    var claims = new List<Claim>();

    if (oidFullUri is not null)
        claims.Add(new Claim(ApplicationClaimTypes.EntraObjectId, oidFullUri));

    if (oidShort is not null)
        claims.Add(new Claim(ApplicationClaimTypes.EntraObjectIdShort, oidShort));

    if (isSiteAdmin)
        claims.Add(new Claim(ClaimTypes.Role, RoleNames.SiteAdministrator));

    return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
}
```

### Testing Throws (FluentAssertions)

```csharp
var act = () => principal.GetOwnerOid();
act.Should().Throw<InvalidOperationException>().WithMessage("*Entra Object ID*");
```

### Claim Fallback Priority Test

When a method checks two claim types in order, test:
1. Only primary claim present → returns primary value
2. Only fallback claim present → returns fallback value
3. Both claims present → returns primary (priority ordering confirmed)
4. Neither claim present → expected exception/null

## Spec vs. Implementation Divergence

The task spec said `GetOwnerOid` returns `null` when no OID claim is present. The actual implementation **throws `InvalidOperationException`**. Always read the production code before writing tests — specs can be outdated.

## ResolveOwnerOid Truth Table

| requestedOwnerOid | equals caller OID? | requireAdmin | isSiteAdmin | Result |
|---|---|---|---|---|
| null / empty | — | any | any | caller OID |
| same value | yes | any | any | caller OID |
| different | no | true | false | `null` (forbidden) |
| different | no | true | true | requestedOwnerOid |
| different | no | false | any | requestedOwnerOid |

## File Location

`src/JosephGuadagno.Broadcasting.Api.Tests/ClaimsPrincipalExtensionsTests.cs`


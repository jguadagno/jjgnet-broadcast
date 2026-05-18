# Team Decisions

Compiled record of team decisions, architecture choices, and resolutions.

## Directives
### 2026-04-30T08:12:33-07:00: User directive
**By:** Copilot (via Copilot)
**What:** All work must have an issue and must start on a branch, then be committed and delivered through a pull request.
**Why:** User request — captured for team memory

---

### 2026-04-30T08:29:06-07:00: User directive
**By:** Copilot (via Copilot)
**What:** `.squad` notes are allowed in PRs when they are pertinent to that PR.
**Why:** User request — captured for team memory

---

### 2026-04-30T23-45-07Z: User directive
**By:** Copilot (via Copilot)
**What:** Do not include dotnet test .\src\ --no-build --verbosity normal --configuration Release --filter "FullyQualifiedName!~SyndicationFeedReader" in PR bodies; use the no-filter repo test command instead.
**Why:** User request — captured for team memory

---

### 2026-05-05T12:14:52: User directive
**By:** Joe (jguadagno) (via Copilot)
**What:** All manager class dependencies MUST be injected via constructor. Never use IServiceScopeFactory or the service-locator pattern (resolving dependencies from IServiceProvider inside methods). All dependencies — including ISocialMediaPlatformManager, IMessageTemplateDataStore, and any other service — must appear as constructor parameters. This applies to all managers across the codebase (LinkedIn, Twitter, Facebook, Bluesky, and any future platforms).
**Why:** User request — PR #925 was rejected for violating this rule. Captured for team memory to prevent recurrence.

---

---

# Current Sprint — 2026-05-15

### 2026-05-15T13:14:51-07:00: C# DI naming convention
**By:** Joseph Guadagno (via Copilot)
**What:** Constructor-injected parameter names must be camelCase. Private backing fields must be `_camelCase`. PascalCase parameters (as seen in BlueskyManager.cs `SyndicationFeedItemManager`, `YouTubeItemManager`) are non-conformant and should be corrected.
**Why:** Standard C# naming conventions. Consistency across the codebase.
**Reference:** `src\JosephGuadagno.Broadcasting.Managers.Bluesky\BlueskyManager.cs` (lines 39-40).

---

### 2026-05-15T13:49:35: User directives — Publisher settings refactor design decisions
**By:** Joe (via Copilot)
**What:**
1. **KV secret naming (runtime derivation):** `{type}-{ownerOid}-{name}-{setting_name}` where `{type}` = `collector` or `publisher`. Example: `publisher-{ownerOid}-bluesky-bluesky-password`. No explicit `*SecretName` columns.
2. **API route convention:** Use `/Publishers/{name}/Settings` and `/Collectors/{name}/Settings`.
3. **ShortLivedAccessToken (Facebook):** Persist as a secret in Key Vault.
4. **EventPublisherSettings:** Out of scope — application-level config, not user-scoped.
5. **All four publishers in scope:** Bluesky, LinkedIn, Facebook, Twitter/X.
6. **No production data migration needed:** `UserPublisherSettings` has never been used in production.
**Why:** User decisions answering Neo's five open questions from the publisher settings architectural analysis.

---

# Architecture Decision: Publisher Settings Refactor — Architectural Recommendation

**Date:** 2026-05-15T13:21:33-07:00
**Author:** Neo (Lead)
**Status:** Endorsed — Issues #958 (Phase 1) and #959 (Phase 2) created

Three concrete problems driving the refactor:
1. **Cleartext secrets in SQL.** `Settings NVARCHAR(MAX)` stores raw credential values. KV integration path in `GetCredentialsAsync` exists but is never populated by the write path.
2. **Magic string key names.** Typos fail at runtime, not compile time.
3. **Inconsistency with the collector pattern.** Alignment reduces cognitive load.

**Phasing:**
- **Phase 1 (#958):** SQL + EF models, new per-publisher tables, data stores. No API or Web changes.
- **Phase 2 (#959):** Manager, API, DTOs, Web, tests. After Phase 1 is deployed and validated.

**Constraint:** `EventPublisherSettings` is out of scope — infrastructure (Event Grid), not user-scoped credentials.

---

# Architecture Decision: Extract `BuildSecretName` to Shared Utility

**Date:** 2026-05-15T14:06:58.645-07:00
**Author:** Neo (Lead)
**Status:** Decision — fold into #961

**Location:** `JosephGuadagno.Broadcasting.Domain.Utilities.KeyVaultSecretNameBuilder` (static class)
**Namespace:** `JosephGuadagno.Broadcasting.Domain.Utilities`

**Signature:**
```csharp
public static string Build(
    KeyVaultSecretOwnerType ownerType,  // Collector or Publisher
    string ownerOid,
    string name,
    string settingName) → string
```

**Output:** `{ownerType}-{sanitizedOwnerOid}-{sanitizedName}-{sanitizedSettingName}`

**Examples:**
- `Build(Collector, ownerOid, "youtube-channel-{channelId}", "api-key")` → `collector-{oid}-youtube-channel-{channelId}-api-key`
- `Build(Publisher, ownerOid, "bluesky", "app-password")` → `publisher-{oid}-bluesky-app-password`

**Migration concern:** Changing KV secret name format breaks existing secrets at rest. Requires two-step deployment (write new key, verify Functions, delete old key). Existing `BuildSecretName` in `UserCollectorYouTubeChannelManager` is private — delete it, replace 3 call sites.

---

# Architecture Decision: Collectors Alignment with Publisher Settings Pattern

**Date:** 2026-05-15T13:55:01.142-07:00
**Author:** Neo (Lead)
**Status:** Decision — Issues #960 and #961 created

4 collector types found. Data layer already complete for all 4. No cleartext secrets anywhere.

**Gap summary:**

| Dimension | YouTube | FeedSource | SpeakingEngagement | ScheduledItem |
|---|---|---|---|---|
| API route `/Collectors/{name}/Settings` | ❌ | ❌ | ❌ | ❌ (missing entirely) |
| Web CRUD controller | ✅ | ✅ | ✅ | ❌ |
| Web service interface | ✅ | ✅ | ✅ | ❌ |
| KV naming `collector-` prefix | ❌ | N/A | N/A | N/A |
| No cleartext secrets | ✅ | ✅ | ✅ | ✅ |

**Key findings:**
- `HasApiKey` in YouTube is a **transient** domain property (populated at runtime from KV), not a SQL column.
- ScheduledItem is one-row-per-user (UNIQUE on `CreatedByEntraOid`) — matches publisher single-settings pattern.
- `youtube-channel-apikey-{owner}-{channel}` → missing `collector-` prefix (naming compliance gap, not security incident).

**Issues created:**
- **#960** — feat: align collector API routes to /Collectors/{name}/Settings and complete ScheduledItem layer
- **#961** — fix: align YouTube collector KV secret naming to collector- prefix convention

---

# Decision: Self-Contained Collector/Publisher Model Architecture

**Date:** 2026-05-16T10:25:34-07:00
**Author:** Joseph (via Copilot)
**Status:** Directive — Architectural principle for all collector/publisher work

Each collector and publisher is treated as a **self-contained model**. Minimize shared components and APIs. Adding or removing a collector or publisher should NOT require a big refactor. Each collector/publisher owns its own service, controller, views, and DTOs. Shared code should be kept to an absolute minimum.

**Rationale:** Ensures the architecture stays modular and maintainable as publishers/collectors are added or removed over time.

---

# Code Review: PR #963 — Publisher Settings Phase 2

**Date:** 2026-05-15
**Author:** Neo (Lead/Architect)
**Verdict:** BLOCKED ❌ → Fixed by Trinity (eda470e7) → UNBLOCKED ✅

**Blocking finding:** Log injection (`cs/log-forging`) in `UserPublisherSettingService.cs` — 3 `LogWarning` call sites passing user-controlled values without `LogSanitizer.Sanitize()`:
- Line 93: `setting.CreatedByEntraOid`
- Lines 156-157: `platform`, `setting.CreatedByEntraOid`
- Lines 164-167 (`LogSaveFailure`): `setting.CreatedByEntraOid`, `setting.SocialMediaPlatformName ?? setting.SocialMediaPlatform?.Name`

**What passed (19/19 other checks):**
- All 5 API controllers: `[IgnoreAntiforgeryToken]`, `[Authorize]`, ownership checks, `LogSanitizer` on all log args
- All 4 typed manager implementations: `BuildSecretName` with `SecretNameSanitizer`, `Has*` booleans only (no secret value exposure)
- All 4 manager test classes with `BuildSecretName` coverage
- Functions migration: all 3 files replace shim with typed managers
- DI registrations correct in API, Functions, Web
- SQL migration: idempotent guard present
- Build: 0 errors, 0 warnings

**Fix (eda470e7):** Trinity wrapped all 3 sites with `LogSanitizer.Sanitize()` and added missing `using JosephGuadagno.Broadcasting.Domain.Utilities;`.

---

# Decision: CollectorSettings — Inline Actions Removed, SpeakingEngagements Added

**Date:** 2026-05-12
**Author:** Trinity
**Branch:** issue-950-sanity-check

- Removed 6 redundant inline POST actions (`Add/Edit/DeleteFeedSource`, `Add/Edit/DeleteYouTubeChannel`) from `CollectorSettingsController`.
- `CollectorSettingsController` is now a **read-only page controller** — all mutations flow through dedicated controllers.
- `CollectorSettingsPageViewModel` gains `SpeakingEngagements` collection, populated via the admin/non-admin branch pattern.
- Inline `.Select(x => new ViewModel { ... }).ToList()` is the established projection pattern in `BuildPageViewModelAsync` — do not introduce AutoMapper.

---

# Frontend Decision: CollectorSettings Index Refactor

**Date:** 2026-05-12T08:29:04-07:00
**Author:** Sparks
**Branch:** issue-950-sanity-check

Replaced inline Add/Edit/Delete modals on `CollectorSettings/Index.cshtml` with redirect links to dedicated CRUD pages. Added Speaking Engagements card section with `bi-mic-fill` icon. Applied `<thead class="table-dark">` to all card tables.

**Pattern:** For settings-style pages that previously used modals, prefer redirect links to dedicated controller actions. Reserve modals for lightweight confirmations only.

---

# Decision: ApiKey Required for YouTube Channel — Split DTOs

**Date:** 2026-05-12
**Author:** Trinity
**Branch:** issue-950-sanity-check / Commit: 6a28f04

- **Create POST:** `CreateUserCollectorYouTubeChannelRequest` — `ApiKey` is `[Required]`.
- **Edit PUT:** `UpdateUserCollectorYouTubeChannelRequest` — `ApiKey` is `string?`; required only when `HasApiKey == false` on the existing record. Controller checks after fetch; Web Edit checks `viewModel.HasApiKey` from hidden field.
- `HasApiKey` is **server-computed** — ignore from client input via AutoMapper `.ForMember(d => d.HasApiKey, o => o.Ignore())`.
- Razor Edit view: round-trip `HasApiKey` via `<input type="hidden" asp-for="HasApiKey" />` — without it, Edit POST always receives `false`.

---

# CSRF Token Validation Sweep — issue-950-sanity-check

**Date:** 2026-05-12
**Author:** Trinity

Full audit of all API and Web MVC controllers. **No changes required** — all controllers were already compliant:
- API: all 8 controllers had `[IgnoreAntiforgeryToken]` at class level
- Web: all 13 controllers had `[ValidateAntiForgeryToken]` on every `[HttpPost]`

CodeQL `cs/web/missing-token-validation` alerts were stale. Read the actual files before applying mechanical security fixes.

---

# Decision: [IgnoreAntiforgeryToken] Sweep — API Controllers

**Date:** 2026-05-15T14:15:40.968-07:00
**Author:** Trinity

Full grep of all 10 API controllers. All already have class-level `[IgnoreAntiforgeryToken]`. **No code changes required.** Security bot alerts for 3 controllers were false positives — the class-level attribute was applied in a prior sweep. Do not add per-method attributes; class-level is the established pattern.

---

# Decision: Sanitize User-Controlled Values in UserPublisherSettingService Log Calls

**Date:** 2026-05-15
**Author:** Trinity
**Branch:** issue-959-publisher-settings-phase2 / PR #963 / Commit: eda470e7

Fixed Neo's blocking `cs/log-forging` finding: wrapped all 3 `LogWarning` user-controlled args with `LogSanitizer.Sanitize()`. Added `using JosephGuadagno.Broadcasting.Domain.Utilities;`.

**Learnings:**
- `LogSanitizer.Sanitize()` applies in the **service layer**, not just controllers.
- Private helper methods (like `LogSaveFailure`) are equally subject — audit the full file.
- Always check that `using JosephGuadagno.Broadcasting.Domain.Utilities;` is present in new files that log user-controlled data.

---

# Decision: Every migration that creates tables MUST also update table-create.sql

**Date:** 2026-05-16T09:52:31.833-07:00
**Author:** Morpheus (Data Engineer)
**Branch:** issue-972-end-user-validation
**Commit:** 7b88ea23

## Context

The Aspire AppHost (`AppHost.cs`) bootstraps the JJGNet database on fresh
environments by concatenating exactly three scripts:

1. `scripts\database\database-create.sql`
2. `scripts\database\table-create.sql`
3. `scripts\database\data-seed.sql`

Files under `scripts\database\migrations\` are **not** automatically run by the
AppHost. They are production deployment artifacts only.

## Problem Observed

Issue #972: `/Publishers/Settings/Index` threw `CommandError` ("Invalid object
name 'UserPublisherLinkedInSettings'") on any fresh Aspire-managed environment.
Root cause: the Phase 1 migration (`2026-05-15-publisher-settings-per-publisher-tables.sql`)
created the four per-publisher tables but never added them to `table-create.sql`.

## Decision

**Every migration PR that creates a new table MUST include the same DDL (with
`IF NOT EXISTS` guards) in `scripts\database\table-create.sql` in the same PR.**

This is a hard gate for all data-layer PRs. Reviewers should check that both
files are updated together.

## Rationale

- Omitting a table from `table-create.sql` silently breaks fresh environments
  (local Aspire, CI spin-ups, new developer onboarding).
- The IF NOT EXISTS guards make `table-create.sql` idempotent — safe for Aspire
  replay even if the migration has already been applied to a running database.

---

# Decision: Audit Web Service Base URL Constants on API Route Refactors

**Date:** 2026-05-16T09:01:41.335-07:00
**Author:** Trinity
**Branch:** issue-973 / PR #974
**Issue:** #973

## Decision

When API controller routes are refactored, the corresponding Web service `private const string` base URL constants must be updated in the same PR or as an immediate follow-up. These constants are the sole routing source of truth for Web→API calls and the compiler gives no indication when they go stale.

## Context

After the collector API route refactor (issue #960) moved controllers from `/UserCollector*` to `/Collectors/{name}/Settings`, three Web service files retained the old constants:

- `UserCollectorYouTubeChannelService.cs`: `/UserCollectorYouTubeChannels` → `/Collectors/YouTube/Settings`
- `UserCollectorFeedSourceService.cs`: `/UserCollectorFeedSources` → `/Collectors/FeedSource/Settings`
- `UserCollectorSpeakingEngagementService.cs`: `/UserCollectorSpeakingEngagements` → `/Collectors/SpeakingEngagement/Settings`

This caused all three collector CRUD operations to return 404 silently.

## Recommendation

Add a checklist item to the API route refactor process: after changing `[Route(...)]` on any API controller, grep the Web `Services/` folder for the old route string to catch stale constants.

---

# Decision: Replace Task.WhenAll with Sequential Awaits for Scoped DbContext

**Author:** Trinity  
**Date:** 2026-05-16  
**Commit:** 20fc6b79  
**Status:** IMPLEMENTED

## Context

Three controllers used `Task.WhenAll` to fan out multiple manager calls in parallel:

- `PublishersController.GetAllAsync` — 4 platform managers (Bluesky, Twitter, LinkedIn, Facebook)
- `CollectorsController.GetAllAsync` — 4 collector managers (YouTube, Feed, Speaking, Scheduled)
- `SchedulesController.Index` — 2 scheduled item calls

All managers inject `BroadcastingContext` which is registered as a **scoped** service — one instance per HTTP request. EF Core's `DbContext` is **not thread-safe** for concurrent operations.

## Problem

Firing tasks before the first `await` (or using `Task.WhenAll`) starts all operations concurrently on the same `DbContext` instance. This causes the underlying SQL connection to enter a closed/corrupt state:

> "BeginExecuteReader requires an open and available Connection. The connection's current state is closed."

This was the confirmed root cause of the GET /Publishers/Index failure (5th query in the call chain).

## Decision

Replace all three `Task.WhenAll` fan-outs with sequential `await` calls. The performance trade-off (slightly higher latency) is acceptable — these aggregate endpoints make 2–4 lightweight DB reads and the correctness gain far outweighs the latency cost.

## Rule Going Forward

**Never use `Task.WhenAll` — or start multiple tasks before awaiting — when the underlying services share a single scoped `DbContext`.** Use sequential awaits instead.

If true parallel DB access is needed in the future, inject a `IDbContextFactory<BroadcastingContext>` and create a separate context per task.

## Files Changed

- `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\PublishersController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Collectors\CollectorsController.cs`
- `src\JosephGuadagno.Broadcasting.Web\Controllers\SchedulesController.cs`

---

# Decision: Guard UpdateSecretValueAndPropertiesAsync Against Missing Secret (Initial Setup)

**Date:** 2026-05-16T14:19:11.017-07:00
**Author:** Trinity
**Commit:** 1cbeac20

## Problem

`KeyVault.UpdateSecretValueAndPropertiesAsync` failed on the very first save for any publisher
(Bluesky, LinkedIn, Facebook, Twitter). When no secret exists yet in Key Vault, the Azure SDK
`SecretClient.GetSecretAsync` throws `RequestFailedException(404)` — it does **not** return null.

The method had a null-response guard (`if (originalSecretResponse is null) throw ...`) but no
guard for the 404 case, so `RequestFailedException` propagated uncaught and blocked initial setup.

## Root Cause

Azure SDK contract: `SecretClient.GetSecretAsync` throws on 404, never returns null. The existing
null check was unreachable for the "secret does not exist" scenario.

## Decision

Wrap the "get + disable old version" block in:

```csharp
catch (RequestFailedException ex) when (ex.Status == 404)
{
    // Secret does not exist yet (initial setup) — no old version to disable.
    _logger.LogInformation("Secret '{SecretName}' does not exist yet; skipping disable of previous version.", secretName);
}
```

When the secret is absent, skip the disable step and fall through directly to `SetSecretAsync`
to create the first version. The null-response `ApplicationException` guard is retained for genuine
SDK failures on non-404 paths.

## Files Changed

- `src\JosephGuadagno.Broadcasting.Data.KeyVault\KeyVault.cs` — fix applied
- `src\JosephGuadagno.Broadcasting.Data.KeyVault.Tests\KeyVaultTests.cs` — new test added:
  `UpdateSecretValueAndPropertiesAsync_WhenSecretDoesNotExist_ShouldCreateSecretWithoutDisablingOldVersion`

## Test Results

11/11 KeyVault tests pass. Full solution build: 0 errors, 0 warnings.

---

# Decision: Publishers/Index DbCommand Failure — Root Cause & Defensive Patterns

**Date:** 2026-05-16
**Author:** Trinity (Backend Developer)
**Status:** Adopted (commits `7b88ea23`, `3ef227a2`)

---

## Context

`/Publishers/Index` was failing with a `Failed executing DbCommand` error against
`UserPublisherFacebookSettings`. The per-publisher settings tables were introduced
via a SQL migration (`2026-05-15-publisher-settings-per-publisher-tables.sql`) but
were never added to `scripts/database/table-create.sql`, which is the only script
the Aspire AppHost runs for fresh environments.

---

## Decisions

### 1. Migration tables must be backfilled into `table-create.sql`

Every SQL migration that creates a new table **must** also add that table to
`scripts/database/table-create.sql` with an `IF NOT EXISTS` guard. This keeps
the Aspire AppHost bootstrapping script in sync for fresh/CI environments.

**Rationale:** The AppHost's composition root concatenates only three scripts:
`database-create.sql`, `table-create.sql`, and `data-seed.sql`. Migrations are
run manually against existing environments. Any table that exists only in a
migration file will be silently absent in any fresh environment started via Aspire.

**Pattern enforced:**

```sql
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MyNewTable')
BEGIN
    CREATE TABLE [MyNewTable] ( ... )
END
```

---

### 2. Data store operations must have try/catch with null-return fallback

All four CRUD operations (`GetByUserAsync`, `GetByIdAsync`, `SaveAsync`,
`DeleteAsync`) in every `*DataStore` class **should** catch `Exception` and
return `null` / `false` rather than letting DB exceptions propagate to
controllers.

**Rationale:** `PublishersController.GetAllAsync` fans out to all four
publisher data stores via `Task.WhenAll`. One uncaught `SqlException` (e.g.,
a missing table, a transient connection error) caused the **entire** aggregate
to fail and the page to render an error. With the try/catch, missing or
unconfigured publisher settings degrade gracefully to "not configured" instead
of taking down the whole page.

**Pattern enforced:**

```csharp
try
{
    return await _context.UserPublisherFacebookSettings
        .FirstOrDefaultAsync(u => u.UserId == userId);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error in {Method} for user {UserId}",
        nameof(GetByUserAsync), LogSanitizer.Sanitize(userId));
    return null;
}
```

---

### 3. No migration script needed when schema is already correct

Before writing a migration, compare the EF entity model, Domain model,
`BroadcastingContext` Fluent API configuration, and all SQL scripts to confirm
an actual column mismatch exists. In this case, all four sources were in
agreement — no migration was required.

---

## Impact

- No schema mismatch — no data changes.
- `table-create.sql` fix (`7b88ea23`) ensures fresh Aspire environments start
  with all required tables.
- try/catch fix (`3ef227a2`) ensures a single publisher data store failure does
  not cascade to a full page error.
- Build: 0 warnings, 0 errors.
- Tests: 404 passed, 0 failed.

---

# Current Sprint — 2026-05-16

### 2026-05-16: Hash Discriminator in KeyVaultSecretNameBuilder (Supersedes Sanitize-All-Segments)

**Author:** Oracle (Security Engineer)
**Branch:** issue-972-end-user-validation
**Status:** Implemented

**What:** Azure Key Vault enforces `[a-zA-Z0-9-]{1,127}` for secret names. The initial fix replaced all `_` with `-` in `KeyVaultSecretNameBuilder`, but YouTube Channel IDs use base64url encoding where both `-` and `_` are semantically distinct. Simple substitution creates silent collisions (e.g., `UCabc_def` and `UCabc-def` map to the same Key Vault name, causing one user's secret to silently overwrite another's).

**Decision:** Apply `HashDiscriminator()` using SHA-256 (first 8 bytes = 16 hex chars) to the `discriminator` parameter only. All other segments (`ownerOid`, `platform`, `settingName`) continue using `SanitizeSegment()` — they are controlled values with no collision risk.

**Implementation:**
```csharp
private static string HashDiscriminator(string discriminator)
{
    if (string.IsNullOrEmpty(discriminator))
        return string.Empty;
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(discriminator));
    return Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();
}
```

**Consequences:**
- ✓ Distinct channel IDs → distinct Key Vault names (`UCabc_def` → `d5ca878c9efddfbd`, `UCabc-def` → `ab82aaa6dd869199`)
- ✓ Any Unicode discriminator maps safely without special-char restrictions
- ✓ Deterministic (same input → same output, no drift)
- ✓ 16 hex chars fits within 127-char limit
- ✓ Pass 1 was never in production, so no migration needed

**Files Changed:**
- `src/JosephGuadagno.Broadcasting.Domain/Utilities/KeyVaultSecretNameBuilder.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Tests/KeyVaultSecretNameBuilderTests.cs`

---

### 2026-05-16: Use `data-loading-text` to Control Form Submit Button Loading State

**Author:** Sparks (Frontend Developer)
**Branch:** issue-972-end-user-validation

**What:** `site.js` has a shared `DOMContentLoaded` handler that disables form submit buttons on submit and replaces their label with a spinner + hardcoded `"Saving..."`. This is correct for Save/Create/Edit forms but wrong for Filter/search forms, which were also showing `"Saving..."`.

**Decision:** Extend the handler to read a `data-loading-text` attribute from the submit button. If present, use its value; otherwise fall back to `"Saving..."`.

```js
var loadingText = btn.dataset.loadingText || 'Saving...';
btn.innerHTML = '...' + loadingText;
```

**Pattern:**
| Button purpose | Attribute needed | Displayed text |
|---|---|---|
| Save / Create / Edit | *(none)* | Saving... |
| Filter / Search | `data-loading-text="Searching..."` | Searching... |
| Custom action | `data-loading-text="Your text..."` | Your text... |

**Rationale:** Zero duplication (one handler + one fallback), forward-compatible (new actions just need the attribute), consistent UX.

**Files Changed:**
- `wwwroot/js/site.js` — reads `data-loading-text`
- 9 × `Views/*/Index.cshtml` — Filter buttons annotated

---

### 2026-05-16: jQuery Validate url2 Override for Localhost URL Validation in Development

**Author:** Sparks (Frontend Developer)
**Branch:** issue-972-end-user-validation
**Status:** Decided

**What:** Collector views (FeedSource and SpeakingEngagements) validate URLs with `[Url]` data annotations, mapped to jQuery Validate's `url` rule. The standard `url` rule uses a strict regex that rejects `localhost` URLs (e.g., `http://localhost:5000/feed.xml`), blocking developers from testing collectors against local dev servers.

**Considered Options:**
- **Option A (Rejected):** Inject `IWebHostEnvironment` in views — injects infrastructure concerns into presentation layer.
- **Option B (Rejected):** Add `data-rule-url2="true"` attribute — adds `url2` as additional rule on top of `url`, so `url` still fires and rejects localhost. Also applies in all environments (no dev-only guard). Previous attempt was half-implemented/half-commented.
- **Option C (Chosen):** Use `<environment include="Development">` tag helper to override the validator in the @Scripts section only in Development. Clean separation, no infrastructure injection, production behavior unchanged.

**Decision:** In each affected view's `@section Scripts`, after `_ValidationScriptsPartial`, add:

```cshtml
<environment include="Development">
    <script>
        jQuery.validator.methods.url = jQuery.validator.methods.url2;
    </script>
</environment>
```

**Key Facts:**
- `additional-methods.js` (which defines `url2`) was already in `_ValidationScriptsPartial.cshtml` — no libman/CDN changes
- Override is page-scoped (fires after DOM load, applies to page validator instance only)
- Production and Staging: standard `url` rule unchanged, localhost URLs rejected (expected)

**Affected Files:**
- `Views/CollectorFeedSources/Add.cshtml` — removed stale `data-rule-url2`, added env override
- `Views/CollectorFeedSources/Edit.cshtml` — added env override
- `Views/CollectorSpeakingEngagements/Add.cshtml` — added env override
- `Views/CollectorSpeakingEngagements/Edit.cshtml` — added env override

---


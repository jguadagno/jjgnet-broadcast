# Trinity - History

## Summary

Trinity (Backend API Developer) implements core API functionality including CRUD endpoints, authentication/authorization workflows, OAuth token refresh, and data persistence. Work spans three layers: Controllers (HTTP routing), Managers (business logic), and Data/Data.Sql (Entity Framework Core persistence). Key contributions include EngagementSocialMediaPlatforms CRUD endpoints, UserApprovalManager for RBAC workflows, OAuth token refresh with token versioning, ownership isolation enforcement, and `IMemoryCache` caching layer for managers. Trinity follows Neo's architectural patterns: explicit service contracts with DTOs, response mapping to isolate Data layer changes from API contracts, and role-based authorization. Pattern: implement feature vertically from API controller through Manager to Data layer, write explicit request/response types in API, and map Data objects to DTOs before returning.

---

### 2026-05-16 — Publisher Settings Refactor: 5 Self-Contained Controllers + Services + Views

**Status:** ✅ COMPLETE — commit 95487e72; 1246 tests passed (0 errors, 0 warnings)

**What was delivered:**
- Refactored from monolithic `PublisherSettingsController` + `PublisherSettingsService` to **5 self-contained per-publisher implementations**:
  - `BlueskyPublisherSettingsController` + `BlueskyPublisherSettingsService`
  - `LinkedInPublisherSettingsController` + `LinkedInPublisherSettingsService`
  - `FacebookPublisherSettingsController` + `FacebookPublisherSettingsService`
  - `TwitterPublisherSettingsController` + `TwitterPublisherSettingsService`
  - Shared API controllers refactored for each platform

- Deleted old monolithic implementations
- Each publisher owns its controller, service, DTOs, views
- Established **self-contained architecture directive**: adding/removing a publisher does NOT require big refactors
- Aligns with existing collector pattern (YouTube, FeedSource, SpeakingEngagement, ScheduledItem)

**Test Results:** 1246 passed, 0 errors, 0 warnings

---

### 2026-05-15 — PR #963 Log Injection Fix & Issue #958 Phase 1

**Status:** ✅ COMPLETE

- PR #963: Fixed 3 `LogSanitizer` sites in `UserPublisherSettingService.cs` (eda470e7)
- PR #962: Phase 1 delivered 4 per-publisher SQL tables + EF models + data stores (28 tests)

---

### 2026-05-14 — Issue #950: Neo Review Fixes

**Status:** ✅ COMPLETE — commits 6a56416, 525dc2d; 157 Functions tests passing

---

## Learnings (Recent)

Trinity focuses on vertical API→Manager→Data patterns. Self-contained controller + service + DTOs per publisher/collector minimizes shared code. Each platform is independent — adding/removing doesn't require big refactors.

---

### 2026-05-17 — 204-vs-404 Fix: Singleton User-Config GET Endpoints

**Status:** ✅ COMPLETE — build succeeded, 0 errors, 0 warnings; all tests pass (2 pre-existing KeyVault failures unrelated to this work)

**Decision:** Neo approved `neo-404-vs-204-optional-resources.md` — singleton GET endpoints (one row per user, identified by ownerOid only) must return `204 No Content` when no record exists. `404 Not Found` implies a bad ID, which is wrong for first-time-user flow.

**What was delivered:**

1. **5 API controllers changed** (singleton GET only; DELETE methods retain 404):
   - `CollectorScheduledItemSettingsController`, `BlueskySettingsController`, `FacebookSettingsController`, `TwitterSettingsController`, `LinkedInSettingsController`
   - `[ProducesResponseType(404)]` → `[ProducesResponseType(204)]`; `LogWarning` → `LogInformation`; `NotFound()` → `NoContent()`; XML doc updated

2. **New extension** `src\JosephGuadagno.Broadcasting.Web\Extensions\DownstreamApiExtensions.cs`:
   - `GetOptionalForUserAsync<T>` on `IDownstreamApi` — catches `HttpRequestException` where `StatusCode == NotFound`, returns null. Defense-in-depth wrapper usable by any Web service.

3. **5 already-working Web services** converted from try/catch-404 to `GetOptionalForUserAsync<T>` (dead code removed since API now returns 204):
   - `UserCollectorScheduledItemService`, `UserPublisherBlueskySettingsService`, `UserPublisherFacebookSettingsService`, `UserPublisherTwitterSettingsService`, `UserPublisherLinkedInSettingsService`

4. **10 previously-unguarded single-item GET call sites** in 9 Web services switched from `GetForUserAsync<T>` to `GetOptionalForUserAsync<T>`:
   - `EngagementService` (GetEngagementAsync + GetEngagementTalkAsync), `MessageTemplateService`, `ScheduledItemService`, `SocialMediaPlatformService`, `SyndicationFeedItemService`, `UserCollectorFeedSourceService`, `UserCollectorSpeakingEngagementService`, `UserCollectorYouTubeChannelService`, `YouTubeItemService`

**Rule established:** ID-based GETs (int id in route) keep 404. Singleton-by-ownerOid GETs use 204. DELETE always returns 404 when missing.

---

### 2026-05-16 — KeyVault initial-setup bug in UpdateSecretValueAndPropertiesAsync

**Bug:** `KeyVault.UpdateSecretValueAndPropertiesAsync` tried to load and disable the existing secret version before creating a new one. On initial setup (no secret exists yet), `SecretClient.GetSecretAsync` throws `RequestFailedException(404)` — it does NOT return null. The method had a null check but no 404 guard, so the exception propagated as-is and blocked the very first `SaveAsync` for any publisher (Bluesky, LinkedIn, Facebook, Twitter).

**Root cause:** The Azure SDK `SecretClient` throws on 404 rather than returning null. The existing null check was unreachable for the "not found" case.

**Fix pattern:** Wrap the "get + disable old version" block in `try/catch (RequestFailedException ex) when (ex.Status == 404)`. When the secret doesn't exist yet, log an informational message and skip the disable step, then fall through to `SetSecretAsync` to create the first version. The null-response `ApplicationException` guard is kept for genuine SDK failures on other status codes.

**Affected file:** `src\JosephGuadagno.Broadcasting.Data.KeyVault\KeyVault.cs`
**Test added:** `UpdateSecretValueAndPropertiesAsync_WhenSecretDoesNotExist_ShouldCreateSecretWithoutDisablingOldVersion`
**Commit:** 1cbeac20

---

### 2026-05-16 — Publishers/Index: Data-Driven Platform Cards from ISocialMediaPlatformService

**Status:** ✅ COMPLETE — commit 1857b497; build succeeded, 0 errors, 0 warnings

**What was delivered:**
- Added `PublisherPlatformCardViewModel` to `PublishersAggregateViewModel.cs`
- Added `Platforms` property (`IReadOnlyList<PublisherPlatformCardViewModel>`) to `PublishersAggregateViewModel`
- Updated `PublishersController` to inject `ISocialMediaPlatformService`, fetch platforms in parallel with aggregate settings, filter by `PlatformControllerMap`, and build `Platforms` list
- Updated `Publishers/Index.cshtml` to iterate `Model.Platforms` instead of a hardcoded inline array
- Mastodon (in DB, no controller) is silently skipped via the static `PlatformControllerMap` allowlist

**Learnings:**
- Static `IReadOnlyDictionary` in the controller is the right place for name→controller mapping; it's deterministic and doesn't belong in the DB
- Start both tasks before awaiting either (`var t1 = Foo(); var t2 = Bar(); var r1 = await t1; var r2 = await t2;`) for simple parallel fan-out without `Task.WhenAll`
- Platforms not in the controller map are silently skipped in LINQ `.Where()` — no exceptions, no view-layer branching needed

---

### 2026-05-16 — Publisher Settings Refactor: 5 Self-Contained Controllers + Services + Views

**Status:** ✅ COMPLETE — commit 95487e72; 1246 tests passed (0 errors, 0 warnings)

**What was delivered:**
- Refactored from monolithic `PublisherSettingsController` + `PublisherSettingsService` to **5 self-contained per-publisher implementations**:
  - `BlueskyPublisherSettingsController` + `BlueskyPublisherSettingsService` + `BlueskyPublisherSettingsView`
  - `LinkedInPublisherSettingsController` + `LinkedInPublisherSettingsService` + `LinkedInPublisherSettingsView`
  - `FacebookPublisherSettingsController` + `FacebookPublisherSettingsService` + `FacebookPublisherSettingsView`
  - `TwitterPublisherSettingsController` + `TwitterPublisherSettingsService` + `TwitterPublisherSettingsView`
  - Shared API controllers refactored for each platform

- Deleted old monolithic `PublisherSettingsController.cs` and `PublisherSettingsService.cs`
- Each publisher owns its controller, service, DTOs, and Razor views
- Established **self-contained architecture directive**: adding/removing a publisher does NOT require big refactors

**Architecture alignment:**
- Mirrors the existing collector pattern (YouTube, FeedSource, SpeakingEngagement, ScheduledItem)
- No shared settings logic — each platform is isolated
- Shared dependency injection via `AddSqlDataStores()` and `AddDataSqlMappingProfiles()`
- `KeyVaultSecretNameBuilder` for unified KV secret naming across all publishers

**Test Results:**
- 1246 tests passed
- 0 errors, 0 warnings
- All CI gates green

**Learnings:**
- Self-contained design reduces cognitive load across 4+ publishers — each is independent
- API controllers, Web controllers, and Function publishers all follow the same per-publisher pattern
- Shared utility (KV naming) at Domain layer, but service implementations isolated

---

### 2026-05-17 — Publishers/Index Root Cause Analysis: `UserPublisherFacebookSettings` DbCommand Failure

**Status:** ✅ COMPLETE — build succeeded (0 warnings, 0 errors), 404 tests passed

**Root cause (two-part):**

1. **Missing tables in `table-create.sql`** — The Aspire AppHost only runs `database-create.sql` + `table-create.sql` + `data-seed.sql` for fresh environments. The 4 per-publisher settings tables (`UserPublisherFacebookSettings`, `UserPublisherBlueskySettings`, `UserPublisherLinkedInSettings`, `UserPublisherTwitterSettings`) were added by the `2026-05-15-publisher-settings-per-publisher-tables.sql` migration but were never backfilled into `table-create.sql`. Any fresh Aspire environment therefore started with no tables → EF threw `Invalid object name 'UserPublisherFacebookSettings'`. Fixed in commit `7b88ea23`.

2. **No defensive fallback in data stores** — Without a try/catch, the `SqlException` propagated through the manager to `PublishersController.GetAllAsync`, which uses `Task.WhenAll` for all 4 platforms. One failure caused the entire aggregate to fail and the page to error. Fixed in commit `3ef227a2`: try/catch added to all 4 operations (`GetByUserAsync`, `GetByIdAsync`, `SaveAsync`, `DeleteAsync`) across all 4 data stores.

**Schema analysis:**
- EF entity model: 12 properties (including `CreatedOn`, `LastUpdatedOn`) ✓
- Domain model: 12 properties ✓
- Migration SQL: 12 columns ✓
- `table-create.sql` (post-fix): 12 columns ✓
- `BroadcastingContext` Fluent API: all 12 mapped, `CreatedOn`/`LastUpdatedOn` with `HasDefaultValueSql("(getutcdate())")` ✓
- **No schema mismatch existed** between EF model and DB.

**Anomaly note — missing `CreatedOn` in the error query:**
The failing SQL shown in the task description omits `[u].[CreatedOn]` (11 columns instead of 12). The current code generates 12-column SELECT. This indicates the error was captured against an older compiled binary. The actual SQL Server error would have been "Invalid object name 'UserPublisherFacebookSettings'" — EF logs the attempted SQL, not the SQL Server error text.

**No migration script needed** — the tables were already created by the migration; `table-create.sql` is only for fresh environments. Adding them idempotently (with `IF NOT EXISTS`) to `table-create.sql` was the correct fix.

**Pattern reinforced:** Whenever adding tables via a migration, also backfill `scripts/database/table-create.sql` with an `IF NOT EXISTS` guard so Aspire fresh-environment bootstrapping stays in sync.

---

### 2026-05-16 — Concurrent DbContext Fix: Task.WhenAll → Sequential Awaits

**Status:** ✅ COMPLETE — commit 20fc6b79; 247 tests passed (0 errors, 0 failures)

**Root cause:**
`PublishersController.GetAllAsync`, `CollectorsController.GetAllAsync`, and `SchedulesController.Index` all used `Task.WhenAll` to fan out calls to managers that share a single scoped `BroadcastingContext`. EF Core's `DbContext` is not thread-safe for concurrent operations — simultaneous queries on the same context caused the SQL connection to enter a closed/corrupt state:
> "BeginExecuteReader requires an open and available Connection. The connection's current state is closed."

**Fix:** Replaced all three `Task.WhenAll` fan-outs with sequential `await` calls. The DbContext is scoped per HTTP request, so each await completes before the next begins — no concurrent access.

**Files changed:**
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\PublishersController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Collectors\CollectorsController.cs`
- `src\JosephGuadagno.Broadcasting.Web\Controllers\SchedulesController.cs`

**Learning:** Never use `Task.WhenAll` (or fire-and-forget task creation before the first await) when the underlying managers share a single scoped `DbContext`. The pattern `var t1 = Foo(); var t2 = Bar(); await Task.WhenAll(t1, t2)` looks like a safe optimization but is a correctness bug with EF Core scoped contexts. Use sequential awaits instead.

### GetForUserAsync<T> — 404 handling pattern (2026-05-17)

Any Web service that calls `IDownstreamApi.GetForUserAsync<T>` for a **single nullable object** (not a collection) MUST wrap the call in `catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)` and return `null`. The API legitimately returns 404 for first-time users who have no configuration yet; without the catch the exception propagates and crashes the page. The controller already handles `null` gracefully. Log the 404 as `LogInformation` (not `LogWarning`) — it is expected, not an error. Always sanitize the OID via `LogSanitizer.Sanitize(ownerOid)`.

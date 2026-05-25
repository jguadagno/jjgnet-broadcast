## Summary (archived 2026-05-19)

Older history entries have been archived. See history-archive.md for complete session logs.

---

### 2026-05-21 — HasAccessToken Dead Code Removal (LinkedIn)

**Status:** ✅ COMPLETE — 271 data layer tests pass; pre-existing `LinkedInControllerTests.cs` build error unrelated

**What changed:**
- Removed `HasAccessToken` from `Domain.Models.UserPublisherLinkedInSettings`, `Domain.Models.LinkedInPublisherSetting`, `Data.Sql.Models.UserPublisherLinkedInSettings`, `Data.Sql/UserPublisherLinkedInSettingsDataStore` (mapping assignment), `Api/Dtos/LinkedInSettingsDtos` (response DTO), `Api/Controllers/Publishers/LinkedInSettingsController` (`settings.HasAccessToken = true`), `Web/Models/PublisherPlatformSettingsViewModels` (property + validation), `Web/Controllers/PublisherLinkedInSettingsController` (mapping), `Data.Sql.Tests/UserPublisherLinkedInSettingsDataStoreTests` (fixtures), `Views/PublisherLinkedInSettings/Index.cshtml` and `Edit.cshtml`.
- Updated the LinkedIn ViewModel validation: the `HasAccessToken`-gated rule `(ChangeCredentials || !HasAccessToken)` was simplified to `ChangeCredentials` only, since there's no longer a stored-token indicator.
- DB column `HasAccessToken` exists in `UserPublisherLinkedInSettings` table — EF ignores unmapped columns, so no immediate breakage. SQL migration tracked in `.squad/decisions/trinity-hasaccesstoken-removal.md`.

---

### 2026-05-21 — Fix LinkedInControllerTests Signature Mismatch

**Status:** ✅ COMPLETE — 12/12 LinkedInControllerTests pass; 0 new failures introduced

**What changed:**
- `src/JosephGuadagno.Broadcasting.Web.Tests/Controllers/LinkedInControllerTests.cs` line 197–214: changed `public async Task RefreshToken_WhenCallbackUrlIsValid_ShouldRedirectToLinkedInAuthUrl()` to `public void` and removed `await` from `controller.RefreshToken()` call. The production `LinkedInController.RefreshToken()` returns `IActionResult` synchronously; the test was incorrectly awaiting it.

**Root cause:** A prior session changed `LinkedInController.RefreshToken()` from `async Task<IActionResult>` to synchronous `IActionResult` (it no longer needs async — just builds a URL and redirects). The test was not updated at that time.

**Pre-existing unrelated failures:** 2 Functions tests (`LoadAllSpeakingEngagementsTests.RunAsync_HandlesNullEngagementsList_Gracefully`, `LoadNewPostsTests.RunAsync_HandlesNullFeedList_Gracefully`) fail with `Assert.IsType() Failure` — these predate this fix and are out of scope.

## Learnings

**2026-05-25 — AutoMapper `ReverseMap()` is unsafe for EF navigation collections:**
`CreateMap<A, B>().ReverseMap()` generates the B→A direction without any `Ignore()` directives, so navigation collection properties are included in the reverse map. When used with `mapper.Map(source, destination)` on a tracked EF entity, AutoMapper replaces the tracked `ICollection<T>` with new untracked objects, causing "Unexpected entry.EntityState: Detached" errors on `SaveChanges`. The fix is to split into two explicit `CreateMap` declarations and add `.ForMember(dest => dest.NavProp, opt => opt.Ignore())` on the domain→data direction — the same pattern already used for `Talk`, `MessageTemplate`, and `SyndicationFeedItem`. Timestamp fields with conditional default logic must remain explicit assignments after `mapper.Map(source, destination)` rather than being expressed in the profile, since putting `DateTimeOffset.UtcNow` in a profile makes tests non-deterministic.

**Null guards and test consistency must travel together:** When removing dead code (e.g., `HasAccessToken`), be precise about what is changed. The `newItems == null ||` guard in collector loops is not related to the removed feature — stripping it causes `NullReferenceException` when the reader returns `null`, which the outer `catch` converts to `BadRequestObjectResult`, breaking tests that expect `OkObjectResult`. Always verify each modified line is actually related to the removal before committing.

**When a controller method goes sync, update the test too:** If `async Task<IActionResult>` is simplified to `IActionResult`, any test that `await`s the result will fail to compile with CS1061 (`IActionResult` has no `GetAwaiter`). Change the test method to `void` (or drop the `async`/`await` pair) to match. The assertion chain doesn't change.

**`HasAccessToken` was a DB column, not just a C# flag:** When removing a bool flag that was persisted to SQL, EF Core's "ignore unmapped columns" behavior means the app still works after removing the C# property — but the column is orphaned. Always note the pending SQL cleanup in the decisions inbox so it can be tracked as a separate migration.

**Twitter `HasAccessToken` is still active:** `UserPublisherTwitterSettings` has both `HasAccessToken` and `HasAccessTokenSecret` — these are live, Twitter still uses the Key Vault token pattern. Do NOT remove them when cleaning up LinkedIn.

**2026-05-25T10:47:45.368-07:00 — Engagement saves must update a tracked aggregate:**
`SpeakingEngagementsReader` populates `Engagement.Talks`, so remapping a domain
engagement to a fresh EF `Models.Engagement` and only forcing the root
`EntityState` leaves child `Talk` rows detached. Fix
`EngagementDataStore.SaveAsync` by loading/creating the tracked engagement,
copying scalar fields onto that tracked entity, and then upserting talks onto
the tracked aggregate (matching existing talks by ID or stable talk fields when
imported talks have no IDs).

---

### 2026-05-21 — Facebook OAuth Token Architecture Fix

**Status:** ✅ COMPLETE — changes unstaged; GitHub issue #988 created

**What changed:**
- `PostPageStatus.cs` — replaced per-user KV token retrieval (`GetPageAccessTokenAsync` / `GetLongLivedAccessTokenAsync`) with `IUserOAuthTokenManager.GetByUserAndPlatformAsync(ownerOid, SocialMediaPlatformIds.Facebook)`. Kept `facebookSettingsManager` for settings (`PageId`, `IsEnabled`). Added `IUserOAuthTokenManager` as a constructor parameter.
- `RefreshTokens.cs` — rewrote entirely. Removed `IFacebookApplicationSettings`, `ITokenRefreshManager`, `IKeyVault` (global KV approach). New logic: query `GetExpiringWindowAsync(now-10y, now+5d)`, filter by `SocialMediaPlatformId == 4`, call `facebookManager.RefreshToken(token)` per user, store result via `StoreOAuthCallbackTokenAsync`. Errors per-user are caught and logged without aborting the loop.
- `PostPageStatusTests.cs` — added `IUserOAuthTokenManager` mock, updated `BuildSut()`, updated `SetupValidCredentials()`, added `Run_WhenOAuthTokenNotFound_SkipsPostingWithoutException` test case. 14 tests pass.
- GitHub issue #988 created with label `squad:Joe` for the manual data migration (seed existing KV tokens into `UserOAuthTokens` before deploy).

## Learnings

**`TokenInfo.ExpiresOn` is `DateTime`, not `DateTimeOffset`:** The `FacebookManager.RefreshToken` returns `TokenInfo` with `ExpiresOn` as `DateTime`. When writing to `UserOAuthTokens` (which uses `DateTimeOffset`), convert via `new DateTimeOffset(DateTime.SpecifyKind(newToken.ExpiresOn, DateTimeKind.Utc))` to avoid timezone ambiguity.

**Pre-existing branch break in `LinkedInControllerTests.cs`:** Line 214 tries `await controller.RefreshToken()` but `LinkedInController.RefreshToken()` returns `IActionResult` (sync). This is a pre-existing break from other uncommitted changes on the branch (`LinkedInController.cs` was already modified). Not related to the Facebook OAuth fix.

**`UserOAuthToken.SocialMediaPlatformId` exists:** Confirmed the domain model has the property, so `GetExpiringWindowAsync` + LINQ filter by `SocialMediaPlatformId` works cleanly — no need to add `GetAllByPlatformAsync`.

**RefreshTokens no longer uses `ITokenRefreshManager` or `IKeyVault`:** The `TokenRefreshes` table and global KV path are dead code for Facebook after this change. Cleanup of those is deferred to a future issue.

---
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

### 2026-05-19 — Fix: MSAL L1 Cache Pin Scoped to Release Builds Only

**Status:** ✅ COMPLETE — commit c5242189; Release and Debug builds both pass (0 errors, 0 warnings)

**Root cause:**
`MsalDistributedTokenCacheAdapterOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)` was set unconditionally in `Web/Program.cs`. While intended to pin the L1 (in-memory) cache and prevent per-request SQL reads, it propagated to the SQL (L2) distributed cache, overriding its 14-day sliding expiration. Result: forced re-login after 15 minutes of inactivity and on every app restart in development.

**Fix:**
Wrapped `options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);` in `#if !DEBUG` / `#endif`. In Debug builds, the setting is omitted → SQL cache keeps 14-day sliding expiry. In Release, the L1 pin remains → production performance optimization preserved.

**Learning:**
`MsalDistributedTokenCacheAdapterOptions` applies to **both** L1 (in-memory) and L2 (distributed/SQL) cache layers. Properties set here are not L1-exclusive — `AbsoluteExpirationRelativeToNow` silently overrides the distributed cache TTL configured at the SQL store level.

---

### GetForUserAsync<T> — 404 handling pattern (2026-05-17)

Any Web service that calls `IDownstreamApi.GetForUserAsync<T>` for a **single nullable object** (not a collection) MUST wrap the call in `catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)` and return `null`. The API legitimately returns 404 for first-time users who have no configuration yet; without the catch the exception propagates and crashes the page. The controller already handles `null` gracefully. Log the 404 as `LogInformation` (not `LogWarning`) — it is expected, not an error. Always sanitize the OID via `LogSanitizer.Sanitize(ownerOid)`.

# Trinity — History Archive

## Archived on 2026-05-07

### 2026-04-30 — Issues #897 and #902: clean PR recovery from stacked local branches

**Status:** ✅ COMPLETE — PR #911 on `issue-897-social-media-publisher-interface`, PR #912 on `issue-902-linkedin-message-composition`

**What was delivered:**
- Cleaned `issue-897-social-media-publisher-interface` down to product files only and opened PR #911 against `main`
- Cleaned `issue-902-linkedin-message-composition` down to product files only and opened PR #912 stacked on `issue-897-social-media-publisher-interface`
- Verified both clean branches from `origin/main` lineage or clean stacked lineage using the repo Release restore/build/test pass

**Key patterns discovered:**
1. When a local issue branch mixes product files with `.squad` drift, recover the product change in a dedicated worktree and push a clean remote branch instead of rewriting the dirty workspace.
2. `issue-902-linkedin-message-composition` is not safely reviewable straight from `main`; cherry-picking onto `origin/main` conflicts in `LinkedInManager`, `ILinkedInManager`, and `LinkedInManagerUnitTests`, so stacking it on the clean #897 branch is the lowest-risk review path.
3. The repo's branch guard blocks commits from ad-hoc branch names; use an issue-scoped local branch such as `feature/897-social-media-publisher-interface` even for temporary recovery worktrees.

### 2026-04-30 — Issues #890 and #893: Sprint 29 hardening PR split

**Status:** ✅ COMPLETE — PR #909 on `issue-890-expiring-window-guard`, PR #910 on `issue-893-webbaseurl-warning`

**What was delivered:**
- `src\JosephGuadagno.Broadcasting.Data.Sql\UserOAuthTokenDataStore.cs`: `GetExpiringWindowAsync(from, to)` now throws `ArgumentException` when `from > to`
- `src\JosephGuadagno.Broadcasting.Data.Sql.Tests\UserOAuthTokenDataStoreTests.cs`: added invalid-window coverage
- `src\JosephGuadagno.Broadcasting.Functions\LinkedIn\NotifyExpiringTokens.cs`: resolves `Settings:WebBaseUrl` once per run, logs one warning when missing, and reuses the normalized value across both notification windows
- `src\JosephGuadagno.Broadcasting.Functions.Tests\LinkedIn\NotifyExpiringTokensTests.cs`: covers null/empty/whitespace config with warning verification and relative-link fallback

**Key patterns discovered:**
1. In Data.Sql, inverted date windows are caller bugs; fail fast with a parameterized `ArgumentException` instead of returning an empty result set.
2. In Azure Functions, normalize optional configuration once at function entry and pass the value into downstream helpers to avoid duplicate warnings and repeated config reads.
3. When one dirty working tree contains work for multiple issues, split by issue-scoped branches and PRs before merging so review history stays aligned with the repo's one-PR-per-issue rule.

### 2026-05-02 — Issue #853: LinkedIn OAuth token expiry notification Function and email templates

**Status:** ✅ COMPLETE — PR #891 on `issue-853-notify-expiring-linkedin-tokens`; all tests passing

**What was delivered:**
- `ConfigurationFunctionNames.LinkedInNotifyExpiringTokens` constant added to Domain
- `LinkedIn/NotifyExpiringTokens.cs` timer Function (daily 08:00 UTC via `%linkedin_notify_expiring_tokens_cron_settings%`)
- Two passes: 7-day window (`LinkedInTokenExpiring7Day` template) and 1-day window (`LinkedInTokenExpiring1Day` template)
- Email templates seeded in `data-seed.sql` and a dedicated migration script
- 7 unit tests in `Functions.Tests/LinkedIn/NotifyExpiringTokensTests.cs`

**Key patterns discovered:**
1. `EmailTemplates` table stores plain HTML (or Scriban-renderable HTML) — `IEmailTemplateManager.GetTemplateAsync(name)` retrieves by name. Rendering Scriban on the body before queuing is the right extension point.
2. `UserOAuthToken.CreatedByEntraOid` is the OID field (issue spec called it `OwnerOid`). `UserOAuthToken.SocialMediaPlatformId` is the platform field (issue spec called it `PlatformId`). `IUserOAuthTokenManager.UpdateLastNotifiedAtAsync` still takes `ownerOid`/`platformId` parameter names — pass `token.CreatedByEntraOid` and `token.SocialMediaPlatformId`.
3. Timer functions use `%settings_key_name%` cron binding with a default in `local.settings.json`. No DI registration needed — Functions runtime discovers `[Function]`-decorated classes automatically.
4. `IApplicationUserDataStore` is already registered in `Functions/Program.cs`. Use `GetByEntraObjectIdAsync(oid)` to resolve user email for notification.
5. Deduplication: compare `token.LastNotifiedAt.Value.UtcDateTime.Date >= todayUtc` — using `from.UtcDateTime.Date` for `todayUtc` keeps the check stable within the run.

### 2026-04-27 — Issue #855: Application-layer performance fixes

**Status:** ✅ COMPLETE — 242 + 166 tests passing, committed on `issue-855-system-validation`

**Fix 1: SchedulesController.Index — Task.WhenAll**
- `GetScheduledItemsAsync` and `GetOrphanedScheduledItemsAsync` were awaited sequentially, adding a full serial HTTP roundtrip to every page load.
- Fix: start both tasks without awaiting, then `await Task.WhenAll(itemsTask, orphanedTask)`, then `await` each completed task to retrieve results.

**Fix 2: SocialMediaPlatformManager paged GetAllAsync — serve from cache**
- The paged overload `GetAllAsync(int page, int pageSize, ...)` bypassed `IMemoryCache` and hit the data store on every call.
- Fix: call the existing cached `GetAllAsync(includeInactive)` to get the full list, apply filter/sort/page in-memory, and return `PagedResult<SocialMediaPlatform>`.
- Sort dispatch uses a `switch` expression on `sortBy.ToLowerInvariant()` covering all domain fields; default is Name.

**Key patterns:**
1. When two async calls have no data dependency, convert to `Task.WhenAll` — do not await inline.
2. Cache-backed managers: paged overloads should call the non-paged cached overload and slice in-memory; avoids duplicate DB calls for small/stable datasets.
3. Always add `using System.Linq;` when adding LINQ to a file that didn't have it.

---

### 2025-XX-XX — MessageTemplates Index: selectedPlatform filter

**Status:** ✅ COMPLETE — 0 build errors

**What was updated:**
- `MessageTemplatesController.cs` `Index` action: added `selectedPlatform` param; loads with `pageSize: 100, page: 1` to get all templates in one shot; derives distinct platforms BEFORE filtering; filters in memory; sets `ViewBag.Platforms`, `ViewBag.SelectedPlatform`, `ViewBag.TotalCount = filteredViewModels.Count`, `ViewBag.TotalPages = 1`.
- `Views/MessageTemplates/Index.cshtml` line 34: fixed pre-existing `RZ1031` Razor tag helper error — replaced inline ternary `@(... ? "selected" : "")` attribute with proper boolean `selected="@(p == (string)ViewBag.SelectedPlatform)"` syntax.

**Key learnings:**
1. Razor tag helpers (`<option>`) reject standalone C# expressions as attributes (RZ1031). Use `attribute="@(boolExpr)"` instead of `@(expr ? "attr" : "")`.
2. For small admin datasets, loading all items at `pageSize: 100` and filtering in memory is simpler and correct — no service interface change needed.
3. Always derive `platforms` list from the FULL unfiltered set so the dropdown shows all options regardless of current filter.

---

### 2026-05-XX — MessageTemplates Web-layer sort/filter wiring

**Status:** ✅ COMPLETE — 0 build errors

**What was updated:**
- `src\JosephGuadagno.Broadcasting.Web\Interfaces\IMessageTemplateService.cs`: Added `sortBy`, `sortDescending`, `filter` parameters to `GetAllAsync`
- `src\JosephGuadagno.Broadcasting.Web\Services\MessageTemplateService.cs`: Updated `GetAllAsync` to pass those params in the query string; `filter` is only appended when non-null/non-empty
- `src\JosephGuadagno.Broadcasting.Web\Controllers\MessageTemplatesController.cs`: `Index` action now accepts and forwards `sortBy`, `sortDescending`, `filter`; sets `ViewBag.SortBy`, `ViewBag.SortDescending`, `ViewBag.Filter`

**Pattern used:** Matched the `SocialMediaPlatformsController` reference pattern exactly.

---

### 2026-04-27 — Issues #868, #869, #872, #873: Fix PagedResponse<T> deserialization in Web services

**Status:** ✅ COMPLETE — PR #875 on `issue-868-fix-paged-response-web-services`; 232/232 tests passing

**Root cause:** Sprint 27 paging refactor (#866/#867) changed all API GET-list endpoints to return `PagedResponse<T>`. The Web service layer still called `GetForUserAsync<List<T>>()` — JSON deserializer received `{"items":[...],"page":1,...}` where it expected `[...]`, throwing `JsonException` on every index page load.

**What was fixed:**
- `SyndicationFeedSourceService`, `YouTubeSourceService`, `SocialMediaPlatformService`: switched to `GetForUserAsync<PagedResponse<T>>()`, returning `PagedResult<T>` with paging params.
- `UserPublisherSettingService`: unwraps `PagedResponse<T>` internally; interface return type (`List<UserPublisherSetting>`) unchanged so callers weren't broken.
- Interfaces `ISyndicationFeedSourceService`, `IYouTubeSourceService`, `ISocialMediaPlatformService`: updated `GetAllAsync` signatures with paging params.
- Controllers: `SyndicationFeedSourcesController`, `YouTubeSourcesController`, `SocialMediaPlatformsController` Index actions updated with paging params + ViewBag. `EngagementsController`, `PublisherSettingsController`, `SchedulesController`, `HelpController` callers updated to use `.Items` and pass `Pagination.MaxPageSize` for dropdown/all-item use cases.
- Views: `SyndicationFeedSources/Index.cshtml` rebuilt from blank; `YouTubeSources/Index.cshtml` and `SocialMediaPlatforms/Index.cshtml` updated with sortable headers, filter form, pagination partial.
- 7 test files updated across Web.Tests.

**Key learnings:**
1. When a service interface returns `PagedResult<T>` instead of `List<T>`, ALL callers in the Web project need auditing — not just the index controllers. Dropdown callers (engagements, publisher settings) need `pageSize: Pagination.MaxPageSize` and `.Items`.
2. Hidden callers exist in non-obvious controllers (`SchedulesController.SearchSyndicationFeedSources`, `HelpController.SocialMediaPlatforms`) — always grep for all usages before considering a service interface change complete.
3. `UserPublisherSettingService` is special: keep the public interface returning `List<T>` to avoid cascading changes; only change the internal deserialization type.
4. Test mocks using `GetForUserAsync<List<T>>()` must be updated to `GetForUserAsync<PagedResponse<T>>()` — Moq type parameters are part of the setup match and won't fire if they don't match exactly.



### 2026-05-XX — Issue #866: Wire Paged Manager Overloads (TODO Cleanup)

**Status:** ✅ COMPLETE — 1 commit on `issue-866-getall-consistency`; build 0 errors

**What was fixed:**
- 6 controllers had `// TODO(morpheus):` stubs calling old non-paged overloads; replaced with the full-signature overloads Morpheus had already added to the interfaces.
- `SyndicationFeedSourcesController` and `YouTubeSourcesController`: admin path calls `GetAllAsync(page, pageSize, sortBy, sortDescending, filter)`; owner path calls `GetAllAsync(ownerOid, page, pageSize, sortBy, sortDescending, filter)`; return type switched from `List<T>` + manual count to `PagedResult<T>`.
- `SocialMediaPlatformsController`: consolidated `GetAllIncludingInactiveAsync()` / `GetAllAsync()` branches into single `GetAllAsync(page, pageSize, sortBy, sortDescending, filter, includeInactive)` call.
- `UserCollectorFeedSourcesController`, `UserCollectorYouTubeChannelsController`, `UserPublisherSettingsController`: replaced `GetByUserAsync(resolvedOwnerOid)` with `GetAllAsync(resolvedOwnerOid, page, pageSize, sortBy, sortDescending, filter)`.

**Key learning:** When a manager interface exposes a new paged overload alongside the legacy non-paged one, the controller `PagedResponse` construction must switch from `items.Count` (from `List<T>`) to `result.TotalCount` (from `PagedResult<T>`) to get correct total counts for pagination metadata.

---

### 2026-04-25 — Issue #866: Standardize All GetAll API Endpoints (FINAL SESSION)

**Status:** ✅ COMPLETE — 3 commits on `issue-866-getall-consistency`; 192 tests passing; ready for PR merge

**Final deliverable verification:**
- ✅ All 9 controllers renamed/updated with `GetAllAsync(page, pageSize, sortBy, sortDescending, filter)`
- ✅ Return type standardized to `ActionResult<PagedResponse<T>>` across all 9 controllers
- ✅ Morpheus pre-staged paged overloads: all controllers now call new overloads directly
- ✅ Test cascade fixed: 5 test files updated; 192 tests passing; 0 failures
- ✅ Build verification: `dotnet build --configuration Release` succeeded; 0 errors

**Controllers finalized:**
1. `EngagementsController` — Renamed `GetEngagementsAsync` → `GetAllAsync`
2. `MessageTemplatesController` — Added sort/filter paging
3. `SchedulesController` — Renamed `GetScheduledItemsAsync` → `GetAllAsync`; paging added
4. `SocialMediaPlatformsController` — Full paging/sort/filter; return type changed
5. `SyndicationFeedSourcesController` — Renamed `GetSyndicationFeedSourcesAsync` → `GetAllAsync`
6. `UserCollectorFeedSourcesController` — Full paging/sort/filter; return type changed
7. `UserCollectorYouTubeChannelsController` — Full paging/sort/filter; return type changed
8. `UserPublisherSettingsController` — Full paging/sort/filter; return type changed
9. `YouTubeSourcesController` — Renamed `GetYouTubeSourcesAsync` → `GetAllAsync`

**Integration notes:**
- Morpheus had pre-staged uncommitted work: all Domain interfaces + DataStore implementations with paged overloads ready
- Trinity consumed these directly — no adapter pattern needed
- Manager layer delegation is consistent: all forward sort/filter/paging to data stores
- Test fixes applied PowerShell batch regex patterns for efficiency (regex > repeated edit calls)

---

### Learnings

### 2026-04-25 — Issue #866: Standardize All GetAll API Endpoints
**Status:** ✅ COMPLETE — 2 commits on `issue-866-getall-consistency`

**What was changed:**
- All 9 API controllers updated to use `GetAllAsync` (renamed from entity-specific names) with `page`, `pageSize`, `sortBy`, `sortDescending`, `filter` parameters and `ActionResult<PagedResponse<T>>` return type
- Morpheus had pre-staged work in the working tree: all Domain interfaces and DataStore implementations already had paged overloads — controllers were updated to use them directly (no wrapper needed for most)
- `MessageTemplatesController` and `SchedulesController` now call paged `GetAllAsync` overloads on their respective managers/data stores
- Test fixes: `ControllerAuthorizationPolicyTests`, `SchedulesControllerTests`, `ScheduledItemManagerTests`, `MessageTemplateDataStoreTests`, `ScheduledItemDataStoreTests`

**Key learnings:**
1. **Cascade CS0535 errors are misleading**: In a full-solution build, a compile failure in a dependency (e.g., `Data.Sql`) can cause spurious interface-not-implemented errors in `Managers`. Always build each project in isolation to identify real vs cascade errors.
2. **New overload ambiguity pattern**: Adding a paged `GetAllAsync(int, int, string, bool, string?, CT)` alongside existing `GetAllAsync(int, int, CT)` causes CS0121 at every call site that only passes 2 positional args. Fix: switch all call sites to the new overload, passing all params explicitly.
3. **Moq `It.IsAny<>()` with ambiguous overloads**: Mock setups using `It.IsAny<int>(), It.IsAny<int>()` are also ambiguous when new overloads are added. Must add `It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()` to target the new 7-arg overload.
4. **`dotnet build --no-restore` can show stale errors**: Use `--no-incremental` to force full recompile when edits are not picked up.
5. **PowerShell batch regex replace for test files**: When the same pattern appears 10+ times in a test file, `(Get-Content -Raw) -replace ... | Set-Content` is far faster than individual `edit` tool calls.
6. **Moq `sut.OldMethodName()` calls need renaming separately**: The regex for mock setups won't catch direct `sut.MethodName()` invocations — these need a separate replace pass.

---

### 2026-05-XX — Issue #862: Consolidate ClaimsPrincipal Helpers into Extension Class
**Status:** ✅ COMPLETE — PR opened on `issue-862-claims-principal-extensions`

**What was consolidated:**
Duplicate `GetOwnerOid()`, `IsSiteAdministrator()`, and `ResolveOwnerOid()` private methods existed verbatim across 8 API controllers. Created `ClaimsPrincipalExtensions.cs` in the `JosephGuadagno.Broadcasting.Api` namespace and replaced all 28+ call sites.

**Key decisions:**
- `GetOwnerOid()` returns `string` (throws `InvalidOperationException` if missing) — not `string?` as originally proposed, to match actual existing behavior
- `ResolveOwnerOid()` preserves the **null-as-forbidden** pattern: returns `null` when a non-admin tries to target another user's OID; callers check `if (resolvedOwnerOid is null) return Forbid()`. The proposed design would have silently returned the current user's OID — a security regression
- Added `EntraObjectIdShort` ("oid") fallback in `GetOwnerOid()` for Microsoft.Identity.Web v2+ JWT handlers (additive improvement, not breaking)
- Explicit `using JosephGuadagno.Broadcasting.Api;` required in each controller — C# does NOT auto-expose parent-namespace extension methods to child namespaces

**Fixed inline bypasses:**
`UserCollectorFeedSourcesController` and `UserCollectorYouTubeChannelsController` had raw `FindFirstValue`/`IsInRole` calls in `GetAsync` and `DeleteAsync` that bypassed the private `ResolveOwnerOid`. Replaced with `User.GetOwnerOid()` + `User.ResolveOwnerOid(config.CreatedByEntraOid, true)`.

**Build/test:** 0 errors, all tests green (166 unit + all suites).

---


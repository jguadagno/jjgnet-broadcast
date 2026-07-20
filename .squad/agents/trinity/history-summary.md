# Trinity — History Summary Archive

> Learnings before 2026-05-22 archived to history-archive.md (2026-05-25)

---

## Archived Sessions (2026-05-16 to 2026-05-25)

### 2026-05-25 — Concurrent DbContext, MSAL Cache Pin, Engagement EF Core Fix

- **Concurrent DbContext Fix (2026-05-16):** Replaced `Task.WhenAll` with sequential awaits across three controllers (`PublishersController`, `CollectorsController`, `SchedulesController`) — EF Core DbContext is not thread-safe for concurrent operations within a scoped request.
- **MSAL L1 Cache Pin (2026-05-19):** Wrapped `AbsoluteExpirationRelativeToNow = 15 minutes` in `#if !DEBUG` to prevent it from overriding the distributed (SQL) cache's 14-day sliding expiry in development.
- **Engagement EF Core Bug (2026-05-25):** Fixed AutoMapper `ReverseMap()` detachment issue in `EngagementDataStore.SaveAsync`. Root cause: `CreateMap<A, B>().ReverseMap()` includes navigation properties without `Ignore()`, replacing EF-tracked collections with untracked objects. Solution: explicit bidirectional maps with `.ForMember(d => d.NavProp, opt => opt.Ignore())`.

### 2026-05-21 — LinkedInControllerTests & Facebook OAuth Token Architecture

- **LinkedInControllerTests Fix:** Changed test method from `async Task` to `void` and removed `await` on synchronous `RefreshToken()` call.
- **Facebook OAuth Refactor:** Migrated from per-user Key Vault token retrieval to `IUserOAuthTokenManager.GetByUserAndPlatformAsync()`. Rewrote `RefreshTokens.cs` to query expiring window, call `facebookManager.RefreshToken()`, and store via `StoreOAuthCallbackTokenAsync`. GitHub issue #988 created for seed migration.

### 2026-05-17 — GetForUserAsync<T> 404 Handling Pattern

Wrap single-object nullable API calls in `catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)` returning `null` — first-time users legitimately have no configuration yet. Log as `LogInformation`, not `LogWarning`.

### 2026-05-26 — Issue #995 Phase 1 (Phase 2 Task 1 Completed)

- **RandomPosts Per-User Timer:** Migrated from global Event Grid dispatcher to per-user per-minute Cronos timer.
- **Schema:** Added `UserRandomPostSettings` and `UserEventPublisherMapping` tables.
- **Tests:** 4 unit tests covering no-settings, cron-not-due, correct-queue dispatch, exception propagation.
- **Key learnings:** QueueClient Moq overload resolution via raw invocations; Cronos 5-field cron evaluation; Feb 31 always null for "never due" tests.

---

## Current Status

**Phase 2 Task 2 — Collector Dispatch Routing:** COMPLETED

- Created `ICollectorEventPublisher` service for per-user routing
- Updated 3 collector functions; deleted 16 dead `ProcessNew*` functions
- 248 tests passing
- Branch: `issue-995-per-user-publisher-routing`, commit `41db74f6`


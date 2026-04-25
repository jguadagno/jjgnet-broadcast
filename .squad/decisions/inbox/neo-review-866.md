---
date: 2026-04-25
author: Neo
branch: issue-866-getall-consistency
issue: "#866"
verdict: BLOCKED
---

# Review: issue-866-getall-consistency — GetAll Standardization

## Verdict: BLOCKED ❌

**Build:** 0 errors (718 pre-existing warnings) — PASS  
**Tests:** 11 FAILED / 1347 passed / 51 skipped

---

## Blocking Defect 1 — 6 Controllers silently discard all paging/sort/filter parameters

The paged manager overloads **exist and are implemented**. The domain interfaces and
manager classes all have the correct signatures. The controllers accept the parameters
but call old, non-paged methods and wrap the full result in a `PagedResponse` shell.
This is in-memory pagination — violates the "DB filtering: All lookups via data store
methods, never in-memory at manager layer" directive.

| Controller | File | Lines | Issue |
|---|---|---|---|
| `SyndicationFeedSourcesController` | `Controllers/SyndicationFeedSourcesController.cs` | 63–70 | Calls `GetAllAsync()` / `GetAllAsync(ownerOid)` — TODO comment left in |
| `YouTubeSourcesController` | `Controllers/YouTubeSourcesController.cs` | 61–68 | Same pattern — TODO comment left in |
| `SocialMediaPlatformsController` | `Controllers/SocialMediaPlatformsController.cs` | 55–58 | Calls `GetAllIncludingInactiveAsync()` / `GetAllAsync()` — TODO comment left in |
| `UserCollectorFeedSourcesController` | `Controllers/UserCollectorFeedSourcesController.cs` | 52–53 | Calls `GetByUserAsync(ownerOid)` — TODO comment left in |
| `UserCollectorYouTubeChannelsController` | `Controllers/UserCollectorYouTubeChannelsController.cs` | 52–53 | Same pattern — TODO comment left in |
| `UserPublisherSettingsController` | `Controllers/UserPublisherSettingsController.cs` | 52–53 | Calls `GetByUserAsync(ownerOid)` — TODO comment left in |

All six have `// TODO(morpheus):` comments explicitly acknowledging the gap.
The paged overloads are ready — controllers must call them.

---

## Blocking Defect 2 — 11 tests fail

Root cause: Moq `.Setup()` signatures don't match the actual interface method signatures.

### MessageTemplatesControllerTests (4 failures)

Controller (`MessageTemplatesController.cs` line 66/71) calls the **full** 6-parameter paged overload:
```
GetAllAsync(ownerOid, page, pageSize, sortBy, sortDescending, filter, ct)
```
Tests (`MessageTemplatesControllerTests.cs` lines 141, 155, 159, 173, 187, 200, 218) mock
the **short** 3-parameter paged overload:
```
GetAllAsync(string, int, int, CancellationToken)
```
Moq doesn't match; mock returns `null`; `result.Items` throws `NullReferenceException`.

**Fix:** Update all `_messageTemplateDataStoreMock.Setup(...)` calls for `GetAllAsync` to
include `It.IsAny<string>()`, `It.IsAny<bool>()`, `It.IsAny<string?>()` for sortBy,
sortDescending, filter params.

### UserCollectorFeedSourcesControllerTests, UserCollectorYouTubeChannelsControllerTests, UserPublisherSettingsControllerTests, SocialMediaPlatformsControllerTests (7 failures)

Tests mock paged `GetAllAsync(...)` but controllers call old non-paged methods.
These fail because the mock is never hit and the controller returns a different response
shape than the test expects.

**Fix:** Once Defect 1 is fixed (controllers call paged overloads), these tests will
align. Verify Setup signatures match the full paged method signatures.

---

## What Passes ✅

- **EngagementsController** and **SchedulesController**: correctly call paged overloads end-to-end ✅
- **MessageTemplatesController**: correctly calls paged overloads (controller implementation is right) ✅
- All domain interfaces: paged overloads defined ✅
- All manager implementations: paged overloads implemented ✅
- All data store implementations: paged overloads implemented ✅
- Security: `[IgnoreAntiforgeryToken]` at class level on all modified API controllers ✅
- Security: No `[ValidateAntiForgeryToken]` on any API controller method ✅
- Security: `LogSanitizer.Sanitize()` used on all user-controlled strings in log calls ✅
- Security: `Forbid()` call sites in `MessageTemplatesController` have corresponding tests ✅
- `ownerOid` / `includeInactive` preserved where they existed ✅
- Page/pageSize guards present in all modified controllers ✅
- Return types are `ActionResult<PagedResponse<T>>` across all modified controllers ✅

---

## Required fixes before re-review

1. **Trinity**: Wire up the 6 TODO controllers to call paged manager overloads. Remove TODO comments.
2. **Tank**: Fix `MessageTemplatesControllerTests` Moq Setup signatures (4 tests).
   Verify remaining controller tests use full paged signatures.
3. Re-run `dotnet test` and confirm 0 failures.

---

## Note on minor inconsistency (non-blocking)

`EngagementsController.GetAllAsync` uses a two-statement pageSize guard:
```csharp
if (pageSize < 1) pageSize = 1;
if (pageSize > Pagination.MaxPageSize) pageSize = Pagination.MaxPageSize;
```
Other controllers use the combined `if (pageSize < 1 || pageSize > Pagination.MaxPageSize) pageSize = Pagination.DefaultPageSize;`.
Both clamp to valid values; this is cosmetic and does not block the PR. Can align in a follow-up.

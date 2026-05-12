# Tank - History Archive

Archived entries prior to 2026-04-26.

---

## 2026-04-26 — Issue #866: Fix Remaining 11 Failing Controller Tests (GetAllAsync Overload Mismatch)

**Status:** ✅ COMPLETE — 50 targeted tests passing; 0 failures; 5 test files fixed

**Root cause:** Trinity's commit `9dac48c` updated 6 controllers to call new paged
`GetAllAsync` overloads. The test mocks still targeted old 2/3/4-param overloads, causing
Moq to silently return null → `NullReferenceException` at runtime.

**Two categories of fixes applied:**

**Category A — Mock overload mismatch (Setup + Verify):**
- `MessageTemplatesControllerTests`: 3-param admin setup → 5+CT; 4-param owner setup → 6+CT
- `UserCollectorFeedSourcesControllerTests`: `GetByUserAsync(oid, CT)` → `GetAllAsync(oid, int, int, string, bool, string?, CT)`
- `UserCollectorYouTubeChannelsControllerTests`: same
- `UserPublisherSettingsControllerTests`: same
- `SocialMediaPlatformsControllerTests`: `GetAllAsync(bool, CT)` → `GetAllAsync(int, int, string, bool, string?, bool, CT)`

**Category B — Return type change (List<T> → PagedResult<T>):**
- All `GetByUserAsync` mocks returned `List<T>` → new `GetAllAsync` returns `PagedResult<T> { Items, TotalCount }`
- `SocialMediaPlatformsControllerTests`: `ReturnsAsync(List<T>)` → `ReturnsAsync(new PagedResult<T> { Items = ..., TotalCount = ... })`

**Category C — Assertion mismatch (result.Result → result.Value):**
- Controllers return `new PagedResponse<T>` directly (value path), not via `Ok()` (result path)
- `result.Result.Should().BeOfType<OkObjectResult>()` → `result.Value.Should().NotBeNull()`
- `ForbidResult` assertions remain on `result.Result` (correct — `Forbid()` uses result path)

**Moq pattern for UserCollector/PublisherSettings (6+CT owner overload):**
```csharp
_manager
    .Setup(m => m.GetAllAsync(
        It.IsAny<string>(),   // ownerOid
        It.IsAny<int>(),      // page
        It.IsAny<int>(),      // pageSize
        It.IsAny<string>(),   // sortBy
        It.IsAny<bool>(),     // sortDescending
        It.IsAny<string?>(),  // filter
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(new PagedResult<T> { Items = items, TotalCount = items.Count });
```

**Moq pattern for SocialMediaPlatforms (6 data params + includeInactive + CT):**
```csharp
_managerMock
    .Setup(m => m.GetAllAsync(
        It.IsAny<int>(),      // page
        It.IsAny<int>(),      // pageSize
        It.IsAny<string>(),   // sortBy
        It.IsAny<bool>(),     // sortDescending
        It.IsAny<string?>(),  // filter
        It.IsAny<bool>(),     // includeInactive
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(new PagedResult<SocialMediaPlatform> { Items = platforms, TotalCount = platforms.Count });
```

**Commit:** `587add2` — `test: fix Moq overload mismatch in GetAllAsync controller tests (#866)`

---



**Status:** ✅ COMPLETE — 192 tests passing; 0 failures; 5 test files fixed

**Test cascade identified and resolved:**
- CS0535 cascade errors from full-solution builds misleading — Trinity experienced spurious interface-not-implemented errors in Managers when real errors were in Data.Sql
- Solution: Build each project in isolation to identify real vs cascade errors

**Test files fixed (5 total):**
1. **ControllerAuthorizationPolicyTests.cs** — Fixed `nameof()` refs for renamed methods (`GetEngagementsAsync` → `GetAllAsync`, `GetScheduledItemsAsync` → `GetAllAsync`, etc.)
2. **SchedulesControllerTests.cs** — Updated Moq setups to target new 7-arg paged `GetAllAsync(int, int, string, bool, string?, CancellationToken)` overload; renamed all `sut.GetScheduledItemsAsync()` → `sut.GetAllAsync()`
3. **ScheduledItemManagerTests.cs** — Disambiguated overload call with explicit `cancellationToken: default`
4. **MessageTemplateDataStoreTests.cs** — Disambiguated with explicit `sortBy: "subject"`
5. **ScheduledItemDataStoreTests.cs** — Disambiguated with explicit `sortBy: "sendondatetime"`

**Moq pattern for new 7-arg overload:**
```csharp
// Before (2-arg overload):
_managerMock.Setup(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
  .ReturnsAsync(...);

// After (7-arg overload):
_managerMock.Setup(m => m.GetAllAsync(
    It.IsAny<int>(),      // page
    It.IsAny<int>(),      // pageSize
    It.IsAny<string>(),   // sortBy
    It.IsAny<bool>(),     // sortDescending
    It.IsAny<string>(),   // filter
    It.IsAny<CancellationToken>()))
  .ReturnsAsync(...);
```

**Test results:** `dotnet test` — 192 passing, 0 failures

---

### Team Rules (Permanent)

**Status:** ✅ COMPLETE  
**PR:** #739 (feat(#729): enforce owner isolation in API controllers)  
**Issue:** #729

### Work Summary

Added 9 missing security tests for Talks and Platforms sub-actions in PR #739 after Neo's Round 2 rejection. Tests complete the coverage matrix for non-owner 403 rejections across all three controllers.

### Tests Added (Round 2)

**TalksController (4 tests):**
- GetTalksByEngagementId — non-owner ForbidResult
- CreateTalk — non-owner ForbidResult
- UpdateTalk — non-owner ForbidResult
- DeleteTalk — non-owner ForbidResult

**PlatformsController (4 tests):**
- GetPlatformsByEngagementId — non-owner ForbidResult
- CreatePlatform — non-owner ForbidResult
- UpdatePlatform — non-owner ForbidResult
- DeletePlatform — non-owner ForbidResult

**Additional:**
- SiteAdmin unfiltered-overload tests refined across all controllers

### Test Pattern
- Entity created with owner OID: `test-oid-12345`
- SUT initialized with different OID: `non-owner-oid-99999`
- All Moq side-effects verified as `Times.Never` during authorization failures
- All constants from Domain.Constants and Domain.Scopes (no magic strings)

### Test Results
- **Total:** 93/93 passing
- **Round 1:** 11 tests (Engagements + initial Talks/Platforms)
- **Round 2:** 9 tests (remaining Talks/Platforms sub-actions)
- **Overall security suite:** 20 tests across three rounds

### Outcome
Neo approved after Round 3 verification. Joseph merged PR #739 to main. Security coverage now complete.

### Related
- **Tank's contribution:** Closed the 9-test gap identified by Neo in Round 2
- **Neo's feedback loop:** Clear identification of missing coverage accelerated completion

---

## 2026-05-XX — Issue #950: Update FeedCheck Test Mocks for EntraOId Parameter

**Status:** ✅ COMPLETE  
**Branch:** `issue-950-sanity-check`  
**Commit:** `2d49b01`

### Work Summary

Updated all FeedCheck-related test files to accommodate the new `string entraOId` parameter added to `IFeedCheckDataStore.GetByNameAsync` and `IFeedCheckManager.GetByNameAsync` as part of the EntraOId user-separation feature.

### Files Updated (8 total)

1. **`FeedCheckDataStoreTests.cs`** — Added `entraOId` to `CreateFeedCheck()` helper; updated 2 existing `GetByNameAsync` calls; added 2 new user-isolation tests:
   - `GetByNameAsync_WithEntraOId_ReturnsRecord` — correct (Name, EntraOId) combo returns record
   - `GetByNameAsync_DifferentEntraOId_ReturnsNull` — same name, different OID returns null (user isolation)
2. **`FeedCheckManagerTests.cs`** — Updated Moq setup/act/verify to 3-param signature `(name, entraOId, CancellationToken)`
3. **`LoadNewPostsTests.cs`**, **`LoadNewVideosTests.cs`**, **`LoadAllPostsTests.cs`**, **`LoadAllVideosTests.cs`** — `SetupFeedCheck()` helpers updated to 3-param mock setup
4. **`LoadAllPostsTests.cs`** (extra fix) — `RunAsync_UsesCollectorOwnerOid_WhenReadingPosts` test was stale: production `LoadAllPosts.cs` was refactored by Trinity to accept `userOid` directly as a parameter instead of calling `GetCollectorOwnerOidAsync`. Updated test to verify `GetAsync(OwnerEntraOid, ...)` is called directly.

### Key Learnings

- **Stale tests from production refactoring:** When a method changes from resolving an OID internally (via `GetCollectorOwnerOidAsync`) to accepting it as a parameter, tests that verify the internal call become stale even if unrelated to the current feature. Always check if `Performed invocations: No invocations performed` Moq errors point to removed code paths.
- **Mock overload resolution:** Moq silently returns null when `.Setup()` param count doesn't match the interface. For optional `CancellationToken`, always include `It.IsAny<CancellationToken>()` explicitly in setup.
- **EF in-memory tests with new filter columns:** New user-isolation tests in `FeedCheckDataStoreTests.cs` only pass once the production `FeedCheckDataStore.GetByNameAsync` actually filters by both `Name` AND `EntraOId` — Trinity completed this in the same branch.

### Test Results

- `FeedCheckDataStoreTests`: 12/12 pass (including 2 new)
- `FeedCheckManagerTests`: 6/6 pass
- `Functions.Tests` (full suite): 158/158 pass

---

## 2026-05-XX — Issue #820: Unit Tests for YouTubeSourcesController and SyndicationFeedSourcesController

**Status:** ✅ COMPLETE  
**Branch:** issue-820-controller-tests  
**PR:** #839

### Work Summary

Added 36 unit tests across two new test files for the source management controllers introduced in PRs #837 and #838.

### Controllers Tested

- `YouTubeSourcesController` — 18 tests
- `SyndicationFeedSourcesController` — 18 tests

### Test Coverage

Each controller: Index, Details (found/not-found/non-owner/admin), Add GET, Add POST (invalid/success/failure), Delete GET (found/not-found/non-owner/admin), DeleteConfirmed (not-found/non-owner/success/failure/admin).

### Security Observations

Neither controller uses `Forbid()`. Both use the Web MVC redirect pattern: non-owner access redirects to Index with `TempData["ErrorMessage"]`. Security coverage matrix built per checklist:
- Details, Delete GET, and DeleteConfirmed each have a non-owner redirect test and an admin bypass test.

### Key Facts

- `YouTubeSource` and `SyndicationFeedSource` both have `required string CreatedByEntraOid` — must be set in `BuildSource()` helpers.
- `SyndicationFeedSource` also has `required string FeedIdentifier`.
- Both controllers use `Add`/`Delete` (not `Create`/`Edit`) as action names.
- `SyndicationFeedSourcesController.Delete` requires `RequireAdministrator` policy (stricter than YouTube's `RequireContributor`).

### Test Results

- 36/36 new tests pass; full suite 157 passed, 0 failed.

---


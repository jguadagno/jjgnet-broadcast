# History

> Learnings before 2026-04-25 archived to history-archive.md (2026-05-25)

## Recent Session: Issue #969 — Collector Controllers in ControllerAuthorizationPolicyTests (2026-05-15)

- **Work:** Added 5 Collector API controllers to `ControllerAuthorizationPolicyTests`
- **Result:** ✅ COMPLETE — 157/157 tests pass; PR #970 opened; issue #969 commented
- **Controllers Added:** `CollectorsController` (1 action), `CollectorYouTubeSettingsController` (5 actions), `CollectorFeedSourceSettingsController` (5 actions), `CollectorSpeakingEngagementSettingsController` (5 actions), `CollectorScheduledItemSettingsController` (3 actions)
- **Pattern:** Each controller added to `ControllerTypes` (class-level `[Authorize]` check) and all public actions added to `ActionPolicies` with their exact policy (`RequireViewer` for GET, `RequireContributor` for POST/PUT/DELETE)
- **Commit:** `40060071` on branch `issue-969-collector-authz-policy-tests`

---

## Recent Session: Fix PublisherSettingsControllerTests Assertions (2026-05-15)

- **Work:** Fixed 2 failing tests in `PublisherSettingsControllerTests.cs`
- **Result:** ✅ COMPLETE — 4/4 PublisherSettingsControllerTests pass; full suite 0 failures
- **Root Cause:** Tests were written asserting the wrong view/redirect target. `SavePlatformAsync` returns `View("Edit", model)` on invalid `ModelState`, and `RedirectToAction(nameof(Edit), ...)` on success — tests incorrectly expected `"Index"` in both cases.
- **Fix 1:** `SaveBluesky_WhenModelIsInvalid_ShouldRebuildIndexView` → renamed to `SaveBluesky_WhenModelIsInvalid_ShouldReturnEditView`, assertion changed from `"Index"` to `"Edit"`.
- **Fix 2:** `SaveLinkedIn_WhenValid_ShouldPersistAndRedirect` → redirect assertion changed from `Index` to `Edit`, added `id` route value check (`3`), kept `userOid` check.
- **Commit:** `c44ad92` on branch `issue-950-sanity-check`

## Recent Session: Full Test Verification Pass — `issue-980-publisher-architecture-refactor` (2026-05-21)

- **Work:** Full build + test verification pass on the branch
- **Result:** ⚠️ 2 failures found — both caused by uncommitted working-tree changes on the branch
- **Build:** ✅ 0 errors, 0 warnings
- **Tests:** 1275 total: 2 failed, 1232 passed, 41 skipped
- **Failures:**
  1. `LoadAllSpeakingEngagementsTests.RunAsync_HandlesNullEngagementsList_Gracefully` — NullReferenceException because `null ||` was removed from `if (newItems == null || newItems.Count == 0)` in uncommitted `LoadAllSpeakingEngagements.cs`
  2. `LoadNewPostsTests.RunAsync_HandlesNullFeedList_Gracefully` — Same root cause in uncommitted `LoadNewPosts.cs`
- **Root Cause:** Branch's uncommitted work removed the null guards that were introduced in commit `4b765f88` alongside the tests. `git show HEAD` still has the correct code; `git diff HEAD` reveals the working-tree regression.
- **Report filed:** `.squad/decisions/inbox/tank-test-failures.md`

---

## Learnings

### Null guard regression pattern (2026-05-21)
When production code has `if (x == null || x.Count == 0)` and tests assert graceful null handling (`OkObjectResult`), removing the `null ||` part converts a safe no-op into a NullReferenceException caught by the outer `catch`, which returns `BadRequestObjectResult`. Always check `git diff HEAD` before running tests — failing null-handling tests with `OkObjectResult` expected but `BadRequestObjectResult` actual is the telltale signature of this pattern.

### Post-save redirect target in PublisherSettingsController
`SavePlatformAsync` always redirects to `Edit` (not `Index`) after both success and invalid model state. Tests for Save* actions must assert `View("Edit", ...)` on validation failure and `RedirectToAction("Edit", ...)` on success, with route values `{ id = platformId }` (non-admin) or `{ id = platformId, userOid = targetOid }` (site-admin).

---

## Recent Session: Issue #945 LinkedInController Test Coverage (2026-05-09)

- **Work:** Added 4 new tests to `LinkedInControllerTests.cs` covering gaps in the existing 8 tests
- **Result:** ✅ COMPLETE — 12/12 LinkedInController tests pass; 236/236 Web tests pass
- **Tests Added:** `Index_WhenPlatformNotFound`, `Callback_WhenCallbackUrlMissing`, `Callback_WhenTokenResponseIsNull`, `Callback_WhenTokenHasRefreshExpiry`
- **Commit:** `ec3c255` on branch `issue-945-linkedin-di-fix`
- **Key Learning:** The Web project has its own `ISocialMediaPlatformService` (distinct from Domain's `ISocialMediaPlatformManager`); `LinkedInController` on this branch uses the Web-layer service. Test mocks must target `ISocialMediaPlatformService`.

---

## Recent Session: Sprint 30 #897 ISocialMediaPublisher Interface Tests (2026-05-01)

- **Work:** Completed comprehensive test coverage for ISocialMediaPublisher contract across all four platform managers
- **Result:** ✅ COMPLETE — Three-layer test strategy (interface shape + inheritance + platform routing) applied to Twitter, Bluesky, Facebook, LinkedIn
- **Validation:** 1154 tests passed, 41 skipped, 0 failed; Functions DI wiring verified
- **Outcome:** Sprint 30 gating task unblocked; #902→#899–#900–#901 composition refactor sequence ready to proceed
- **Decision Merged:** `tank-social-media-publisher-contract-tests.md` (test pattern established for future shared contracts)

---

## Ownership Test Checklist (Sprint 18 Established — 2026-04-18)

> Formal checklist extracted from sprint 18 work (Issues #729, #730, #738, #739).
> Full SKILL.md: `.squad/skills/security-test-checklist/SKILL.md`

### Step-by-Step (Condensed)

1. **Grep Forbid() sites first** — count before writing a single test
2. **Build coverage matrix** — one row per `Forbid()` site; one test column per row
3. **Write non-owner test** per site using the OID mismatch pattern
4. **Assert ForbidResult + Times.Never** on all side-effect mocks
5. **Write admin bypass test** if controller has `IsSiteAdministrator()` branch
6. **Run `dotnet test`** — zero failures before opening PR

### Grep Command

```powershell
Select-String -Path ".\src\**\*Controller.cs" -Pattern "Forbid\(\)" -Recurse
```

### OID Setup Pattern

```csharp
// Entity is owned by "owner-oid-12345" …
var item = BuildScheduledItem(5, oid: "owner-oid-12345");
_managerMock.Setup(m => m.GetAsync(5)).ReturnsAsync(item);

// … caller has a different OID — ownership check must reject it
var sut = CreateSut(Domain.Scopes.Schedules.All, ownerOid: "non-owner-oid-99999");

var result = await sut.UpdateScheduledItemAsync(5, request);

result.Result.Should().BeOfType<ForbidResult>();
_managerMock.Verify(m => m.SaveAsync(It.IsAny<ScheduledItem>()), Times.Never);
```

### Web MVC Pattern (Redirect vs ForbidResult)

| Layer | Forbidden Response |
|---|---|
| API Controller | `result.Result.Should().BeOfType<ForbidResult>()` |
| Web MVC Controller | `result.Should().BeOfType<RedirectToActionResult>()` + `TempData["ErrorMessage"].Should().NotBeNull()` |

### Team Rules (Permanent)

- **ALWAYS run `dotnet test` before committing** — no exceptions
- **ZERO test failures before opening PR** — failing tests block the PR
- **For any security/ownership feature:** grep `Forbid()` first, build matrix, write test per site
- **When controller signatures add `ownerOid` parameter:** update mock `.Setup()` overload immediately — mismatched overloads silently miss setups

### Team Rules (Permanent)

- **ALWAYS run `dotnet test` before committing** — no exceptions
- **ZERO test failures before opening PR** — failing tests block the PR
- **For any security/ownership feature:** grep `Forbid()` first, build matrix, write test per site
- **When controller signatures add `ownerOid` parameter:** update mock `.Setup()` overload immediately — mismatched overloads silently miss setups

### Mock Overload Resolution Note

When a controller method signature changes to add an `ownerOid` parameter, Moq will silently skip mismatched `.Setup()` calls rather than throwing. This causes the mock to return null and tests to behave incorrectly. Always verify the exact parameter types match the controller dispatch path.

### Filtered full-suite baseline (2026-05-25)

The repo-wide CI-aligned test pass remains clean after the recent
LinkedInController test fix.

`dotnet test .\src\ --no-build --configuration Release --filter
"FullyQualifiedName!~SyndicationFeedReader"`

That command completed with 1274 total tests, 1233 passed, 0 failed,
and 41 skipped. When it regresses, compare against that baseline before
assuming the expected skips indicate a new failure.

---


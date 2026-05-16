# Tank - History

## Summary

Tank (QA Engineer) builds comprehensive test coverage across unit, integration, security, and regression test categories using xUnit, FluentAssertions, and Moq. Primary focus: ensuring backend API contracts work correctly, authorization/RBAC logic is enforced, ownership isolation prevents data leaks, and authentication flows are secure. Key test patterns include: mocking external services (HttpClientFactory via Moq), testing async operations with Task.Delay and verification, ownership isolation regression tests (verify User A cannot access User B's resources), and RBAC authorization tests (verify Viewers cannot POST, Admins can manage users). Tank works closely with Trinity (API endpoint contracts), Switch (Web-layer integration tests), and Neo (security test patterns). Established pattern: write tests before implementation (TDD), test both happy path and error cases, use descriptive test names like `GetEngagements_WhenUserIsContributor_ShouldReturnOwnEngagementsOnly`, mock external dependencies, and verify authorization boundaries. Notable: Tank maintains ownership isolation test suite to prevent regressions as new features are added. Key decision: ownership tests go in integration test class alongside endpoint tests, not as separate security-only test file.

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

## Learnings

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

---



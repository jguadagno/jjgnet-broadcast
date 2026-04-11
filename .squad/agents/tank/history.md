# Tank - History

## Core Context

**Role:** Tester | Framework: xUnit 2.9.3, FluentAssertions 7.2.0, Moq 4.20.72
Note: Data.Sql.Tests uses standard xUnit Assert.* (NOT FluentAssertions)

**Critical rules:**
- Sealed 3rd-party types (idunno.AtProto, Azure SDK sealed): typed null (SealedType?)null, never Mock.Of<T>()
- Azure.Storage.Queues.Models.SendReceipt is sealed -> (SendReceipt?)null!
- EmailClient is mockable (virtual methods, protected ctor) -> new Mock<EmailClient>()
- Always inject IQueue (not QueueServiceClient directly) for testable queue-sending code
- FunctionContext required as second parameter in queue trigger Run methods
- Verify method names from interface file, not spec narrative

**Platform IDs (seed data):** Twitter=1, BlueSky=2, LinkedIn=3, Facebook=4, Mastodon=5

**Test patterns:**
- In-memory EF Core: UseInMemoryDatabase(Guid.NewGuid().ToString()) for isolation (no FK enforcement)
- Real AutoMapper + profile in MapperConfiguration (tests actual mapping config, not mocked)
- TestProblemDetailsFactory helper required for API controller tests calling Problem()
- Auth attribute tests: GetCustomAttributes<AuthorizeAttribute>() via reflection
- BuildSut() helper pattern centralizes constructor changes; naming: {Method}_{Scenario}_Should{Expected}
- Moq.Callback<T>() to capture objects for deep assertion

**Epic #667 changes (MessageTemplate/Platform):**
- MessageTemplate.Platform (string) replaced by SocialMediaPlatformId (int FK)
- IMessageTemplateDataStore.GetAsync changed from (string, ...) to (int socialMediaPlatformId, ...)
- All ProcessScheduledItemFired functions got new ISocialMediaPlatformManager constructor param
- Mock setup: SocialMediaPlatformId = 1, .Setup(m => m.GetAsync(It.IsAny<int>(), ...))

**Completed work:**
- PR #501: JsonFeedReader tests; PR #543: 30 publisher tests; PR #559: Twitter integration tests
- RBAC Phase 1 & 2 tests (37 + 5); Issue #613: EngagementsController auth attribute tests
- Issue #575: IMapper mock in test fixtures; Issue #667: 40 compile errors fixed in Functions.Tests
- Issue #693: SocialMediaPlatformDataStore tests (17 tests, PR #698)
- Issue #67: ScheduledItemValidationService backend + build fix (PRs #665, #665-fix)

**Team standing rules:** Only Joseph merges PRs; All mapping via AutoMapper; Paging at data layer only
## 2025-01-27: Test Coverage & Quality Audit (Pre-Feature Health Check)

**Status:** ✅ COMPLETED  
**Findings:** `.squad/decisions/inbox/tank-test-audit-findings.md`

**Scope:** Comprehensive audit of test suite health, coverage gaps, quality patterns, and recent feature test status.

**Coverage Analysis:**
- ✅ 15 of 19 source projects have corresponding .Tests projects
- ✅ 7 of 13 Data.Sql DataStores have test files
- ❌ 6 DataStores without tests: EngagementSocialMediaPlatformDataStore, SocialMediaPlatformDataStore, EmailTemplateDataStore, ApplicationUserDataStore, RoleDataStore, UserApprovalLogDataStore

**Critical Gaps Identified:**
1. **EngagementSocialMediaPlatformDataStore** (epic #667) — Junction table operations have NO tests
2. **SocialMediaPlatformDataStore** (epic #667) — CRUD operations including soft delete have NO tests
3. **RejectSessionCookieWhenAccountNotInCacheEvents** (issue #85) — Complex auth logic with re-entry guard has NO tests
4. **RateLimitingPolicies** (issue #304) — Rate limiting has no integration test verification

**Quality Assessment:**
- ✅ **EXCELLENT:** FluentAssertions usage (19 test files use `.Should()` assertions)
- ✅ **EXCELLENT:** Moq usage (all mocks use `Mock<T>`, no hand-rolled fakes)
- ✅ **EXCELLENT:** AAA pattern adherence (all tests have Arrange-Act-Assert sections)
- ✅ **EXCELLENT:** Async test signatures (zero `async void` instances)
- ✅ **EXCELLENT:** No empty tests (all tests have assertions)
- ⚠️ **MINOR:** xUnit1051 warnings in integration tests (5+ instances, should use `TestContext.Current.CancellationToken`)

**Recent Feature Test Status:**
- ✅ **SendEmail function** (issue #618) — EXCELLENT coverage (6 test scenarios)
- ✅ **SocialMediaPlatform Manager/API** (epic #667) — Manager and Controller layers covered
- ⚠️ **SocialMediaPlatform DataStore** (epic #667) — DataStore layer has NO tests (GAP)
- ⚠️ **EngagementSocialMediaPlatform** (epic #667) — DataStore layer has NO tests (GAP)

**Recommendations (Priority Order):**
1. 🔴 HIGH: Create `EngagementSocialMediaPlatformDataStoreTests.cs` (junction table operations)
2. 🔴 HIGH: Create `SocialMediaPlatformDataStoreTests.cs` (CRUD + soft delete)
3. 🔴 HIGH: Create `RejectSessionCookieWhenAccountNotInCacheEventsTests.cs` (auth edge cases)
4. 🟡 MEDIUM: Verify SourceTags junction table coverage in existing tests
5. 🟡 MEDIUM: Add rate limiting integration test
6. 🟡 MEDIUM: Fix xUnit1051 warnings (replace `default` with `TestContext.Current.CancellationToken`)

## Learnings

### Test Pattern: In-Memory EF Core for DataStore Tests
- **Pattern:** Use `UseInMemoryDatabase(Guid.NewGuid().ToString())` for test isolation
- **Example:** `EngagementDataStoreTests` creates unique in-memory DB per test class
- **Benefit:** Fast, isolated, no cleanup needed (database auto-disposed)
- **Usage:** Standard pattern for all Data.Sql DataStore tests

### Test Pattern: Real AutoMapper with LoggerFactory in Tests
- **Pattern:** Configure real AutoMapper with profile, not mocked
- **Example:**
  ```csharp
  var config = new MapperConfiguration(cfg =>
  {
      cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
  }, new LoggerFactory());
  var mapper = config.CreateMapper();
  ```
- **Benefit:** Tests actual mapping configuration, catches profile errors
- **Usage:** All DataStore and Manager tests use real mapper

### Test Pattern: Callback Capture for Assertion
- **Pattern:** Use Moq `Callback<T>()` to capture objects sent to mocks
- **Example:**
  ```csharp
  Email? capturedEmail = null;
  _mockQueue
      .Setup(x => x.AddMessageAsync(It.IsAny<Email>()))
      .Callback<Email>(email => capturedEmail = email)
      .ReturnsAsync((SendReceipt?)null!);
  // Assert on capturedEmail properties
  ```
- **Benefit:** Verify complex object properties without strict matching
- **Usage:** Common pattern in EmailSenderTests, SendEmailTests

### Test Pattern: TestProblemDetailsFactory Helper
- **Pattern:** Custom `TestProblemDetailsFactory` for API controller tests
- **Purpose:** Avoid null reference exceptions in `ControllerBase.ProblemDetailsFactory`
- **Example:**
  ```csharp
  var controller = new SocialMediaPlatformsController(...)
  {
      ProblemDetailsFactory = new TestProblemDetailsFactory()
  };
  ```
- **Usage:** Required for all API controller tests that call Problem() methods

### Test Pattern: Helper Methods for Test Data
- **Pattern:** Private helper methods in test classes for building test objects
- **Examples:**
  - `CreateEngagement(int id, string name)` — builds Engagement with defaults
  - `BuildTestEmail(string from, string to, ...)` — builds Email with defaults
  - `BuildBase64JsonMessage(EmailModel email)` — encodes Email as Base64 JSON
- **Benefit:** DRY, readable, consistent test data
- **Usage:** Standard pattern across all test classes

### Test Naming Convention
- **File:** `{ClassName}Tests.cs` (e.g., `SocialMediaPlatformManagerTests.cs`)
- **Method:** `{MethodName}_{Scenario}_Should{ExpectedBehavior}`
- **Examples:**
  - `GetAllAsync_WhenPlatformsExist_ShouldReturnOnlyActivePlatforms`
  - `DeleteAsync_WhenPlatformDoesNotExist_ShouldReturnFalse`
  - `Run_InvalidBase64_DoesNotCallEmailClient`
- **Benefit:** Self-documenting, predictable, searchable

### xUnit Best Practice: CancellationToken in Async Tests
- **Issue:** xUnit1051 warning when async tests don't accept CancellationToken
- **Recommendation:** Use `TestContext.Current.CancellationToken` instead of `default`
- **Impact:** Makes tests more responsive to cancellation, better CI/CD behavior
- **Status:** Currently 5+ warnings in integration tests (low priority fix)

**Next:** Joseph to review findings and prioritize test gap remediation work.

## Current Session: 2026-04-02T (Issue #693) — Add Unit Tests for SocialMediaPlatformDataStore

**Summary:** Created comprehensive unit tests for `SocialMediaPlatformDataStore` covering all CRUD operations (17 tests, all passing).

**What I Did:**
1. Investigated `SocialMediaPlatformDataStore.cs` and `ISocialMediaPlatformDataStore` interface
2. Reviewed existing DataStore test patterns (`EngagementDataStoreTests`, `MessageTemplateDataStoreTests`)
3. Created `SocialMediaPlatformDataStoreTests.cs` with:
   - 14 `[Fact]` tests for individual scenarios
   - 3 `[Theory]` tests with `[InlineData]` (7 total theory executions)
   - Coverage: GetAsync, GetAllAsync, GetByNameAsync, AddAsync, UpdateAsync, DeleteAsync
   - Tests for: happy paths, non-existing IDs, case-insensitive search, soft delete behavior, empty database
4. Fixed initial FluentAssertions dependency issue — project uses standard xUnit `Assert.*` not FluentAssertions
5. Removed tests that disposed context (causing double-dispose errors in teardown)
6. Verified all tests pass (17/17 passing)
7. Committed, pushed branch `issue-693`, opened PR #698

**Test Patterns Used:**
- In-memory EF Core database with `UseInMemoryDatabase(Guid.NewGuid().ToString())` for isolation
- AutoMapper with `BroadcastingProfile` configured via `MapperConfiguration`
- Moq for `ILogger<SocialMediaPlatformDataStore>` (verified error logging)
- AAA (Arrange/Act/Assert) pattern
- Helper methods: `CreateDbPlatform()`, `CreateDomainPlatform()`
- `IDisposable` for database cleanup (`EnsureDeleted`)

**Key Findings:**
- `SocialMediaPlatformDataStore` implements soft delete (`IsActive = false`) not hard delete
- `GetByNameAsync` is case-insensitive and only returns active platforms
- `GetAllAsync` orders results alphabetically by name
- Exception handling logs errors and returns `null` (Add/Update) or `false` (Delete)

**Branch:** `issue-693` | **PR:** #698 | **Status:** Awaiting review

## Learnings

8. **Project does NOT use FluentAssertions:** Despite being a common xUnit companion library, this project exclusively uses standard xUnit assertions (`Assert.NotNull`, `Assert.Equal`, `Assert.True`, etc.). Attempting to use FluentAssertions results in build errors (CS0246: type not found). Always check existing test files for assertion style before copying patterns.

9. **Avoid disposing DbContext in exception tests:** When testing exception handling, disposing the EF Core `DbContext` in a test method causes `ObjectDisposedException` in the test class's `Dispose()` method. Better to test exception paths without artificially disposing resources, or use a separate context instance.

10. **SocialMediaPlatformDataStore soft delete pattern:** The `DeleteAsync` method sets `IsActive = false` rather than removing the record. This is important for tests — verify `IsActive` state, not row count. Also, `GetByNameAsync` filters by `IsActive`, so inactive platforms won't be found even if they exist in the database.

## Current Session: 2026-04-11 (Issue #708) — Regression Coverage for Double-Submit Bug Fix

**Summary:** Verified fix for client-side double-submit bug and documented regression coverage strategy (no new test framework needed, backend validation provides defense-in-depth).

**What I Did:**
1. Read squad context (history, decisions, wisdom, now) to understand issue #708
2. Verified the fix was already applied in `site.js` (event.preventDefault() now called when button disabled)
3. Assessed test infrastructure: NO JavaScript testing frameworks in place (no Selenium, Playwright, etc.)
4. Verified existing API test coverage for `AddPlatformToEngagementAsync` endpoint (15 tests, all passing)
5. Documented regression coverage decision in `.squad/decisions/inbox/tank-708-regression-coverage.md`
6. Updated history with learnings

**Fix Already in Place:**
- **File:** `Web/wwwroot/js/site.js` lines 8-12
- **Change:** Added `event` parameter and `event.preventDefault()` call when button is already disabled
- **Impact:** Prevents double-submit for all forms in the application

**Regression Coverage Strategy:**
- ✅ **Client-side fix:** JavaScript now prevents double-submit
- ✅ **API validation:** 15 existing tests verify duplicate detection logic (`EngagementsController_PlatformsTests`)
- ❌ **No new framework:** Do NOT add Selenium/Playwright for this isolated bug (cost/benefit too high)
- ✅ **Manual QA:** Verification steps documented for browser testing

**Key Finding:**
The API already has **defense-in-depth** protection. Even if double-submit occurs, the `AddPlatformToEngagementAsync` endpoint returns `400 BadRequest` for duplicate platform associations. The existing test suite validates this behavior thoroughly.

**Test Results:**
- `EngagementsController_PlatformsTests`: 15/15 passing
- No new tests required (backend validation already comprehensive)

**Branch:** `social-media-708` | **Status:** Ready for manual QA and PR review

## Learnings

11. **Web.Tests vs Api.Tests assertion styles:** The `Web.Tests` project DOES use FluentAssertions (v8.9.0) while `Api.Tests` also uses FluentAssertions (v8.9.0). However, `Data.Sql.Tests` uses standard xUnit assertions. Always check the .csproj file to confirm available assertion libraries before writing tests.

12. **Client-side JavaScript testing decision framework:** When encountering client-side JS bugs, use this decision tree:
    - **Option 1:** Add browser automation (Selenium/Playwright) if:
      - Project has 5+ client-side bugs needing regression tests
      - Implementing complex client-side features (SPA, rich interactions)
      - Backend validation insufficient to prevent data corruption
    - **Option 2:** Fix JS + verify backend validation + manual QA if:
      - Isolated bug with simple fix
      - Backend API already has comprehensive test coverage
      - Backend validation prevents data corruption even if bug recurs
    - **This project:** No JS testing framework exists; prefer Option 2 unless pattern of JS bugs emerges

13. **Defense-in-depth testing pattern:** Client-side bugs (like double-submit) should have two layers of protection:
    - **Layer 1:** Client-side prevention (JavaScript fix) — improves UX, reduces server load
    - **Layer 2:** Server-side validation (API business logic) — prevents data corruption
    - **Testing focus:** Test Layer 2 comprehensively (API tests). Layer 1 can be manual QA unless high-risk/high-frequency bug.

14. **Key file path for regression coverage decisions:** `.squad/decisions/inbox/tank-{issue}-regression-coverage.md` — documents why a particular testing approach was chosen for future reference when similar bugs occur.

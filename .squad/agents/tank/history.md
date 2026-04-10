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

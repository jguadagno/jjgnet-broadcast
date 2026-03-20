# Tank — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Tester
- **Joined:** 2026-03-14T16:37:57.750Z
- **Key Contributions:** Created JsonFeedReader implementation + tests (Issue #302), verified Api.Tests correctness (Issue #515), authored 30 publisher function tests (Issue #301), fixed UTF-8 encoding issues and sealed type mocking pattern
- **Test Framework:** xUnit 2.9.3, FluentAssertions 7.2.0, Moq 4.20.72

### Previous Learnings Summary (2026-03-14 to 2026-03-20)

**Sprint 7 Context (Issue #302 — JsonFeedReader.Tests):**
- Created JsonFeedReader implementation (System.Text.Json based, not JsonFeed.NET due to namespace conflicts)
- Authored IJsonFeedReader, IJsonFeedReaderSettings interfaces and JsonFeedSource model
- 4 constructor validation tests created and passed
- Learning: TDD works well when implementation is missing — tests define contract first
- **Branch:** feature/s7-302-jsonfeedreader-tests | **PR:** #501

**Sprint 8 Context (Issue #515 — Api.Tests Verification):**
- Verified Api.Tests project: all 42 tests passing, clean build (warnings only)
- Confirmed correct PagedResponse<T> usage, pagination parameters, TalkRequest construction
- Finding: Tests were already correctly implemented, no fixes needed
- **Branch:** squad/515-fix-api-tests | **Status:** Ready for PR (no code changes required)

**Sprint 9 Context (Issue #301 — Publisher Function Tests):**
- Authored 30 publisher function tests across 5 test classes (Facebook, LinkedIn, Bluesky)
- Facebook/PostPageStatusTests (5), LinkedIn/PostTextTests (4), LinkedIn/PostLinkTests (7), LinkedIn/PostImageTests (7), Bluesky/SendPostTests (10)
- Established patterns for HttpClient mocking (Moq.Protected, ItExpr), exception handling, fallback logic
- **PR:** #543 | **Status:** Merged to main
- UTF-8 encoding corruption and sealed type mocking issues discovered and documented

**Sealed Type Mocking Discovery Process:**
1. **Commit 450aa70:** Attempted fix using Mock.Of<T>() pattern for sealed types
2. **Issue:** Mock.Of<T>() still fails — validates mockability even though it's a Moq method
3. **Commit 9aeee7a:** Final fix using typed null: (SealedType?)null for Task<T?> return types
4. **Result:** All 153 tests passing, CI/CD pipeline green

**Hard Rules Established:**
1. Always run `dotnet test` before committing to catch Moq validation errors early
2. For sealed library types returning Task<T?>, use typed null instead of Mock.Of<T>()
3. Sealed types from 3rd-party libraries (idunno.AtProto) cannot be mocked — use null or construct real instances

## Current Session: 2026-03-20T22:28:44Z — Final Functions Test Fix & Team Documentation

**Summary:** Completed orchestration logging and team knowledge capture for sealed type mocking pattern. All artifacts documented for future reference.

**Root Cause Analysis:**
- Azure Functions test project failing: "Type to mock (CreateRecordResult) must be an interface, a delegate, or a non-sealed, non-static class"
- Sealed types from idunno.AtProto library cannot be mocked (even with Mock.Of<T>())
- Mock.Of<T>() validates mockability BEFORE attempting to create the mock

**Fix Applied:**
- File: `src/JosephGuadagno.Broadcasting.Functions.Tests/Bluesky/SendPostTests.cs`
- 6 instances: `Mock.Of<CreateRecordResult>()` → `(CreateRecordResult?)null`
- 3 instances: `Mock.Of<EmbeddedExternal>()` → `(EmbeddedExternal?)null`

**Deliverables Created:**
1. **Orchestration log** (`2026-03-20T22-28-44Z-tank.md`) — Complete fix narrative and CI/CD verification
2. **Session log** (`2026-03-20T22-28-44Z-functions-test-fix.md`) — Brief summary for session tracking
3. **Decision merge** (decisions.md) — Sealed type mocking pattern documented as team decision
4. **History summarization** (tank/history.md) — This entry with knowledge compression

**Verification:**
- ✅ Build: 0 errors (55 pre-existing warnings)
- ✅ Tests: 153/153 passing
- ✅ CI/CD: Green pipeline
- ✅ Ready for Sprint 11

**Key Pattern Established:**
```csharp
// For sealed library types (idunno.AtProto, idunno.Bluesky):
.ReturnsAsync((SealedType?)null);  // Use typed null, never Mock.Of<T>()
```

**Commits Referenced:**
- 450aa70 — UTF-8 encoding + Mock.Of<T>() attempt (partial fix)
- 9aeee7a — Sealed type mocking with typed null (full resolution)
- 38b1964 — Documentation and decision merge (this session)

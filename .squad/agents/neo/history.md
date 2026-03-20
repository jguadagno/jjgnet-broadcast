# Neo — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Lead (code reviewer, PR approver/merger, sprint planner)
- **Joined:** 2026-03-14T16:37:57.748Z

### Sprint Planning (condensed)

| Sprint | Milestone | Theme | Issues |
|--------|-----------|-------|--------|
| 7 | [#2](https://github.com/jguadagno/jjgnet-broadcast/milestone/2) | Message Templating & Testing Foundations | #474–478, #302 |
| 8 | [#3](https://github.com/jguadagno/jjgnet-broadcast/milestone/3) | API Improvements, Security, & Infrastructure | #315, #316, #317, #303, #336, #328, #335 |
| 9 | [#4](https://github.com/jguadagno/jjgnet-broadcast/milestone/4) | Test Coverage Expansion — Azure Functions & Managers | #300, #301, #330, #331, #319 |

### Established Code Review Patterns

**DTO & API Layer:**
- Request DTOs must never include route parameters (route is ground truth)
- `ToResponse(null)` must never be called — guard with null check returning `NotFound()` first
- Return type must be `Task<ActionResult<T>>` when explicitly returning `Ok()`, `NotFound()`, etc.
- Null guard pattern: `if (x is null) return NotFound(); return Ok(ToResponse(x));`

**Pagination:**
- `PagedResponse.TotalPages` must guard against divide-by-zero
- Pagination endpoints must clamp `page≥1`, `pageSize 1–100`
- Tests must use `PagedResponse<T>` assertions, not raw `List<T>`

**File & Encoding Issues:**
- BOM (UTF-8 U+FEFF) can appear on line 1 of C# files — watch for it in multi-file PRs
- UTF-8 encoding corruption can silently break CI; verify character encoding after large text file additions

**Database & Infrastructure:**
- `SaveChangesAsync(CancellationToken)` override in DbContext covers both overloads (no-arg variant delegates to it)
- `ALTER DATABASE MODIFY FILE` is the correct zero-downtime approach for size constraint migrations

**PR Review Protocol:**
- Self-authored PRs cannot be approved via `gh pr review --approve` — merge directly when CI green
- PR rejection protocol: different agent must fix (not original author)
- Cross-PR CI interference: CI failure may be from another PR's unrelated changes; trace root cause
- Always verify issue status before reviewing PR (check if already closed to catch duplicates)
- Branch naming should match issue number/scope to avoid confusion

**Testing & Mocking:**
- Sealed classes require `Mock.Of<T>()` pattern instead of constructor-based mocking
- xUnit tests use `[Fact]` with `async Task` signatures
- Exception verification: `Assert.ThrowsAsync<TException>` for presence, `Record.ExceptionAsync` for absence
- Test naming: `Method_Scenario_ExpectedResult` (e.g., `RunAsync_SkipsDuplicate_WhenFeedIdentifierAlreadyExists`)
- AAA structure (Arrange-Act-Assert) with clear section comments

## Learnings

### 2026-03-20: Issue Closure Verification Pattern — Check PR for Implementation Completeness

**Task:** Evaluate if issue #329 (feat: staging deployment slots) was already implemented by PR #483.

**Process:**
1. Fetch issue details (title, body, labels, creation date)
2. Fetch PR details + full diff to verify coverage
3. Cross-reference: does PR implementation **fully address** all stated requirements in the issue?
4. If yes: close the issue with a comment explaining which PR delivered the work

**Result:** ✅ Issue #329 closed as implemented by PR #483

**Reusable Pattern:**
- **Before closing an issue**, verify the PR actually implemented it (don't assume from PR title alone)
- Issues often reference future work in sprint context; PRs from different sprints might have already solved them
- Use `gh issue close 329 --comment "..."` with PR reference to maintain audit trail
- Document decision in `.squad/decisions/inbox/<name>.md` for future reference

**Key Learning:** Always check issue status before reviewing related PRs to catch duplicates early and avoid redundant work across squads.

### 2025-03-21: Full Backlog Triage — 32 Issues Assigned Across 6 Squads

**Task:** Complete triage of all 41 open issues in jguadagno/jjgnet-broadcast repository.

**Results:**
- **32 issues newly assigned** with squad labels based on domain expertise
- **8 issues already assigned** (no action taken)
- **1 issue skipped** (squad:Joe — human-only work)

**Squad Breakdown (Newly Assigned):**
- squad:neo → 12 issues (architecture, CI/CD, cross-cutting patterns)
- squad:sparks → 7 issues (Azure Functions collectors/publishers)
- squad:switch → 8 issues (database schema, data layer, repositories)
- squad:trinity → 2 issues (Web UI features)
- squad:morpheus → 2 issues (API endpoints, managers)
- squad:ghost → 1 issue (Web UI auth/session)

**Key Triage Patterns Established:**
1. **Architecture & Cross-Cutting** → squad:neo
   - Result<T> pattern (#312), IOptions<T> adoption (#309), CancellationToken propagation (#311)
   - Health checks (#313), Serilog deduplication (#314), EventPublisher semantics (#310)
   - CI/CD strategy (#329, #326), Aspire infrastructure (#327)
   - Documentation tasks (#12, #13, #14) — delegable or self-assigned

2. **Azure Functions** → squad:sparks
   - Collector implementations (#8 - GitHub)
   - Publisher refactoring (#45 - Tweet, #46 - Facebook, #102 - LinkedIn)
   - Message customization (#69), exception handling (#94)
   - Naming refactor (#9 - 'publishers' terminology)

3. **Database & Data Layer** → squad:switch
   - Schema additions (#536 - Bluesky handle, #537 - LinkedIn page, #53-#55 - twitter handles, custom images)
   - Repository pagination (#325)
   - Schema normalization (#323 - Tags junction table)
   - SQL optimization (#322 - NVARCHAR sizing)

4. **Web UI** → squad:trinity
   - Server-side pagination (#334)
   - Schedule validation UI (#67)

5. **API & Managers** → squad:morpheus
   - Scheduled items refactor (#89)
   - WebApi caching (#78)

6. **Auth & Security** → squad:ghost
   - Web UI session reload (#81)
   - Already owns #85 parent + 5 sub-issues (#544-#548) for Sprint 11 MSAL work

**Triage Constraints Honored:**
- ✅ Never removed existing squad labels
- ✅ Never touched squad:Joe issues (issue #535 skipped entirely)
- ✅ Applied triage comments to all 32 newly assigned issues
- ✅ No sprint labels added (deferred to squad members and sprint planning)
- ✅ All assignments follow established domain expertise

**Process Efficiency:**
- Used `gh issue view` batch fetching to retrieve all issue labels
- Grouped label applications by squad (7 batches total)
- Applied triage comments in single loop (32 comments added)
- Total execution: ~5 minutes for complete backlog triage

**Triage Report:** `.squad/decisions/inbox/neo-backlog-triage-sprint11.md` (comprehensive assignment breakdown with rationales)

**Sprint 9 Final Status & Earlier PR Review Sessions:**
- Sprint 9 (4 issues) closure completed via PRs #516, #517, #520-526 (all merged)
- Key reviews: DTO patterns (#512/#514), Functions retry/DLQ (#516), SQL sizing (#517), API scopes (#526)
- All established patterns documented in Core Context above
- See orchestration logs 2026-03-20T20-11-20Z-neo.md for detailed session record

### 2026-03-20: Sprint 9 Final — All Issues Closed

**Sprint 9 status:** ALL 7 ISSUES CLOSED  
All PRs merged this session: #516 (#319), #517 (#324), #521 (#519)  
User-merged this session: #526 (#170) + Azure AD updated  
Previously merged: #520 (#333), #522 (#332), #523 (#167/#166), #524 (#191), #525  
**Follow-up created:** Issue #527 — GetTalkAsync only accepts Talks.All scope; Talks.View still commented (pre-existing gap flagged during #521 review)

### 2026-03-21: Sprint 9 Closure & Sprint 11 Planning — 5 Sub-Issues Created

**Final Sprint 9 work (2026-03-20T22:05:20Z):**
- **PRs #542 & #543 reviewed & merged:** 51 collector tests + 30 publisher tests → issues #300, #301 closed
- **PR #538 identified as duplicate:** Closed without merge (duplicate of already-merged PR #539)
- **Tank's encoding fix deployed:** UTF-8 corruption in publisher tests fixed (commit 450aa70), Azure Functions deployment unblocked

**Sprint 11 MSAL Planning:**
- **Created 5 sub-issues for issue #85 decomposition:**
  - #544: Audit existing token cache strategy
  - #545: Implement token cache resilience (retry + fallback for MsalUiRequiredException)
  - #546: Add token refresh pre-check
  - #547: Integration tests for token cache
  - #548: Document MSAL token lifecycle
- **Labels:** `squad:ghost`, `sprint:11` on all sub-issues
- **Status:** Ready for Sprint 11 implementation kick-off

**Sprint 9 Test Coverage Delivered:** 81 new tests across 11 test files (collectors + publishers), all issues closed, all PRs merged, no blockers remaining, deployment unblocked after Tank's fix

**Earlier Sprint 9 Work Summary:**
- PR #529: Engagement social fields (EF + DTOs) ✅ 
- PR #533: Api.Tests repair (pagination fixes) ✅ 
- PR #534: Engagement Web UI (Create/Edit/Details views) ✅
- PR #538: Duplicate PR detected & closed (duplicate of PR #539)
- Triaged issues #527, #528 (scope gap, MSAL cache eviction) → routed to Trinity, Ghost respectively

### 2026-03-21: PR #542 & #543 Review — Azure Functions Collector & Publisher Tests

**Review verdict:** BOTH APPROVED & MERGED  
**PRs:** #542 squad/300-collector-tests (Tank), #543 squad/301-publisher-tests (Tank)  
**Issues closed:** #300 (auto), #301 (auto)  
**Status:** Both squash-merged to main, branches deleted

**PR #542 Review (51 collector tests):**
1. ✅ **Comprehensive coverage**: 3 new LoadAll* test files (32 tests), enhanced 3 LoadNew* files (19 tests added)
2. ✅ **Issue #300 requirements met**:
   - Successful load scenarios ✅
   - Empty feed handling ✅
   - Duplicate detection (VideoId, FeedIdentifier, composite keys) ✅
   - Error handling (reader exceptions, manager exceptions) ✅
   - Parameter validation (checkFrom parsing) ✅
3. ✅ **Naming convention**: All tests follow Method_Scenario_ExpectedResult (e.g., `RunAsync_SkipsDuplicate_WhenFeedIdentifierAlreadyExists`)
4. ✅ **Real logic testing**: Tests verify actual duplicate detection, error handling, and data validation — not just mock call verification
5. ✅ **Moq usage**: Proper mocking of ISyndicationFeedReader, managers, IUrlShortener with correct It.IsAny<DateTimeOffset>() patterns
6. ✅ **AAA pattern**: Clean Arrange-Act-Assert structure throughout
7. ✅ **CI green**: All checks passed

**PR #543 Review (30 publisher tests):**
1. ✅ **Comprehensive coverage**: Facebook (5 tests), LinkedIn (18 tests), Bluesky (10 tests)
2. ✅ **Issue #301 requirements met**:
   - Successful publish scenarios ✅
   - Null/empty queue message handling ✅
   - Manager exception handling (FacebookPostException, LinkedInPostException, BlueskyPostException) ✅
   - Generic exception propagation ✅
3. ✅ **Naming convention**: All tests follow Method_Scenario_ExpectedResult (e.g., `Run_WithValidPostText_CallsPostShareText`)
4. ✅ **Real logic testing**: Tests verify error handling, null returns, API-specific exceptions — not just surface-level mock calls
5. ✅ **Moq usage**: Proper mocking with Times.Once/Times.Never verification, exception setup
6. ✅ **AAA pattern**: Clean Arrange-Act-Assert structure with clear section comments
7. ✅ **CI green**: All checks passed

**Merge sequence:**
- Both PRs self-authored (squad branch pattern)
- PR #542 merged first — had merge conflict in tank/history.md, resolved by keeping both sections
- PR #543 merged second — had "both added" conflicts in test files (collector vs publisher tests), resolved by keeping PR #543 versions
- Cannot approve self-authored PRs via `gh pr review --approve` — merged directly when CI green per established protocol

**Test quality patterns observed:**
- xUnit [Fact] attributes with proper async Task signatures
- NullLogger used for test logging (no real logging infrastructure needed)
- Helper methods (BuildSut, CreateFeedSource, BuildLinkedInPostText) reduce test boilerplate
- Record.ExceptionAsync pattern for exception absence verification
- Assert.ThrowsAsync<TException> for exception presence verification

**Sprint 9 milestone progress:** Issues #300 and #301 now complete (collector & publisher tests). Sprint 9 test coverage expansion continues.

### 2026-03-20: PR #538 Review — Duplicate PR Closed

**Review verdict:** CLOSED AS DUPLICATE  
**PR:** #538 squad/321-bluesky-session-cache  
**Issue:** #330 (already closed by PR #539)  
**Status:** Closed without merge - duplicate work

**Findings:**
1. ❌ **Duplicate PR**: PR #538 implements identical changes to already-merged PR #539 (both close #330)
2. ❌ **Branch naming mismatch**: Branch named `squad/321-bluesky-session-cache` but contains EngagementManager tests for issue #330, not Bluesky session cache work for #321
3. ✅ **Code quality was good**: Before discovering duplication, reviewed the 223 additions to EngagementManagerTests.cs:
   - All 10 new tests follow Method_Scenario_ExpectedResult naming convention
   - Comprehensive timezone conversion coverage (EST, PST, PDT, CET, UTC)
   - Proper deduplication logic tests (SaveAsync with Id=0, GetByNameAndUrlAndYearAsync)
   - FluentAssertions used correctly throughout
   - Moq usage proper with It.IsAny<> patterns
4. ⚠️ **CI failure unrelated**: Build failed on SendPostTests.cs (AtCid type missing) - this is from PR #542/#543 merge into main, not from PR #538's changes

**Action taken:**
- Closed PR #538 with comment explaining it's a duplicate of PR #539
- Issue #330 remains closed (already completed by PR #539)
- Deleted local branch squad/321-bluesky-session-cache

**Pattern reinforced:**
- Always check if issue is already closed before reviewing PR
- Branch naming should match the issue number/scope to avoid confusion
- Duplicate PRs for the same issue can occur if multiple squad members pick up the same work simultaneously

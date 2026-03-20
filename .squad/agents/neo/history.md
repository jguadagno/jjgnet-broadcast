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

### Sprint 8/9 PR Review Outcomes (condensed)

| PR | Description | Outcome |
|----|-------------|---------|
| #512 | DTO layer (Trinity → Morpheus fixed) | REJECTED (BOM + route-in-DTO), APPROVED after Morpheus fix; merged |
| #514 | Pagination (Trinity → Morpheus fixed) | REJECTED (divide-by-zero + negative Skip), APPROVED after Morpheus fix; merged |
| #518 | Api.Tests DTO fix (Tank) | APPROVED & MERGED |
| #520 | Form loading state (Sparks) | APPROVED & MERGED |
| #522 | Form accessibility (Sparks) | HELD — CI red from PR #523 cross-contamination (not Sparks' fault); later merged |
| #523 | BlueSkyHandle schema (Morpheus) | MERGED (user) |
| #524 | Privacy page (Sparks) | APPROVED & MERGED |
| #525 | ViewModel fix | MERGED |
| #526 | Fine-grained API scopes (Ghost) | MERGED by jguadagno + Azure AD updated |

### Review Patterns Established

- BOM (UTF-8 U+FEFF) can appear on line 1 of C# files — watch for it in multi-file PRs
- Request DTOs must never include route parameters (route is ground truth)
- `PagedResponse.TotalPages` must guard against divide-by-zero; pagination endpoints must clamp page≥1, pageSize 1–100
- Self-authored PRs cannot be approved via `gh pr review --approve` — merge directly when CI green
- `ToResponse(null)` must never be called — guard with null check returning `NotFound()` first
- PR rejection protocol: different agent must fix (not original author)
- Cross-PR CI interference: CI failure may be from another PR's unrelated changes, not the PR author
- `SaveChangesAsync(CancellationToken)` override covers both DbContext overloads (no-arg delegates to it)

## Learnings

<!-- Append learnings below -->

### 2026-03-20: PR #521 Review — Null Guard / 404 Fix

**Review verdict:** APPROVED (already merged by jguadagno)
**PR:** #521 `squad/519-fix-null-ref-404`
**Issue closed:** #519 (auto-closed by PR merge)
**Status:** Squash-merged to main, branch deleted, issue closed

**Findings:**
1. ✅ **Null guard pattern consistent**: All 3 affected endpoints use `if (x is null) return NotFound(); return Ok(ToResponse(x));`
2. ✅ **Return type corrected**: `GetTalkAsync` changed from `Task<TalkResponse>` to `Task<ActionResult<TalkResponse>>` — necessary for `ActionResult` to carry the `NotFound()` response
3. ✅ **OkObjectResult test pattern**: Success tests updated to `result.Result.Should().BeOfType<OkObjectResult>().Subject` — correct for explicit `return Ok(...)` endpoints
4. ✅ **NotFound tests correct**: `ThrowsNullReferenceException` → `ReturnsNotFound` tests use `result.Result.Should().BeOfType<NotFoundResult>()`
5. ✅ **Bonus scope cleanup**: `EngagementsController` GET/Create now accept `.List`/`.View`/`.Modify` + `.All` instead of `.All` only — good security improvement
6. ⚠️ **Minor gap**: `GetTalkAsync` scope still only uses `Talks.All` (`.View` remains commented) — pre-existing, not introduced by this PR, not a blocker

**Pattern confirmed for future reviews:**
- When a controller explicitly calls `return Ok(value)`, use `((OkObjectResult)result.Result).Value` (or FluentAssertions: `result.Result.Should().BeOfType<OkObjectResult>().Subject`) — `result.Value` returns null in this case
- Null guard before `ToResponse()` is the correct pattern — never pass null into mapping helpers

### 2026-03-21: PR #516 Review — Functions Retry Policies & DLQ

**Review verdict:** APPROVED & MERGED
**PR:** #516 `squad/319-functions-retry-policies`
**Issue closed:** #319 (auto-closed by PR merge)
**Status:** Squash-merged to main, branch deleted

**Findings:**
1. ✅ **host.json schema valid**: `retry.strategy: exponentialBackoff` with `minimumInterval`/`maximumInterval` in TimeSpan format (`hh:mm:ss`) — correct Azure Functions v4 schema
2. ✅ **Consistent retry/DLQ counts**: `maxRetryCount: 3` (function retries) matches `maxDequeueCount: 3` (queue-level DLQ threshold) — coherent failure handling
3. ✅ **No visibility race**: `visibilityTimeout: 30s` ≥ `maximumInterval: 30s` — message won't re-appear on queue before retry backoff completes
4. ✅ **Valid queue extension settings**: `maxPollingInterval`, `batchSize`, `newBatchThreshold` all valid Azure Storage Queue extension properties
5. ✅ **Minimal risk**: Single-file change to host.json, no code changes

**Note:** Cannot use `gh pr review --approve` on self-authored PRs — merged directly per established protocol.

### 2026-03-21: PR #517 Review — SQL Size Cap Fix

**Review verdict:** APPROVED & MERGED
**PR:** #517 `squad/324-sql-size-cap`
**Issue closed:** #324 (auto-closed by PR merge)
**Status:** Squash-merged to main, branch deleted

**Findings:**
1. ✅ **SQL error 1105 correct**: SQL Server error 1105 = "filegroup is full / cannot allocate space" — exact right error for capacity failures
2. ✅ **MAXSIZE = UNLIMITED correct**: Removes arbitrary 50MB/25MB caps; appropriate for production; disk and SQL Server edition are the real limits
3. ✅ **SaveChangesAsync override idiomatic**: `when (ex.InnerException is SqlException sqlEx)` pattern is efficient; overriding CancellationToken variant covers both overloads (base no-arg delegates to it)
4. ✅ **Original exception preserved**: `throw new InvalidOperationException(..., ex)` — stack trace intact for debugging
5. ✅ **Migration script safe**: `ALTER DATABASE MODIFY FILE` is non-destructive DDL; runs on live databases; includes verification SELECT
6. ✅ **Two-layer defense**: Preventive (remove caps) + defensive (surface errors) — correct architecture for infrastructure constraints

**Pattern confirmed for future reviews:**
- SQL error number verification is critical when catching specific SqlException codes
- `ALTER DATABASE MODIFY FILE` is the correct zero-downtime approach for size constraint migrations
- Overriding `SaveChangesAsync(CancellationToken)` in DbContext covers both overloads — the no-arg variant delegates to it

### 2026-03-20: Sprint 9 Final — All Issues Closed

**Sprint 9 status:** ALL 7 ISSUES CLOSED  
All PRs merged this session: #516 (#319), #517 (#324), #521 (#519)  
User-merged this session: #526 (#170) + Azure AD updated  
Previously merged: #520 (#333), #522 (#332), #523 (#167/#166), #524 (#191), #525  
**Follow-up created:** Issue #527 — GetTalkAsync only accepts Talks.All scope; Talks.View still commented (pre-existing gap flagged during #521 review)

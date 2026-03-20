# Neo — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Lead
- **Joined:** 2026-03-14T16:37:57.748Z

## Learnings

<!-- Append learnings below -->

### 2026-03-20: Sprint 7 & 8 Planning

**Milestones created:**
- **Sprint 7** ([#2](https://github.com/jguadagno/jjgnet-broadcast/milestone/2)): Message Templating & Testing Foundations
  - 6 issues: #474, #475, #476, #477, #478, #302
  - Theme: Implement Scriban-based message templates for all 4 platforms (Twitter, Facebook, LinkedIn, Bluesky) + establish JsonFeedReader test project
  - Ready to start immediately (no Sprint 6 blockers)

- **Sprint 8** ([#3](https://github.com/jguadagno/jjgnet-broadcast/milestone/3)): API Improvements, Security, & Infrastructure
  - 7 issues: #315, #316, #317, #303, #336, #328, #335
  - Theme: Production-ready API (DTOs, pagination, REST compliance) + security hardening (HTTP headers for API, cookie security) + observability (App Insights, vulnerable package scanning)
  - Builds on Sprint 6's security foundation

**Planning rationale:**
- Sprint 7 delivers user-facing value (better social media messages) and establishes testing patterns (#302 is first test project for a collector library)
- Sprint 8 prepares infrastructure for external API consumers and completes security hardening started in Sprint 6
- Deferred 30+ issues to future sprints — clustered database work (#322-325), larger test efforts (#300-301), and architectural refactors (#309-312) for dedicated sprints
- Noted potential duplicate issues (#306, #308, #327) for closure verification

**Sprint 6 status:** 1 open PR (#500, HTTP security headers for Web), 9 closed issues. Should be wrapped up before Sprint 7 starts.

### 2026-03-20: Sprint 9 Planning

**Milestone created:**
- **Sprint 9** ([#4](https://github.com/jguadagno/jjgnet-broadcast/milestone/4)): Test Coverage Expansion — Azure Functions & Managers
  - 5 issues: #300, #301, #330, #331, #319
  - Theme: Comprehensive unit tests for collectors, publishers, and manager business logic + removal of external network dependencies in tests + Functions retry/DLQ configuration
  - All 4 testing issues are priority: high; #319 adds Functions reliability (retry policies + dead-letter queues)
  - Builds on Sprint 7's testing foundation (#302) and follows Sprint 8's API/security work

**Planning rationale:**
- Testing Cluster was identified as deferred high-priority work during Sprint 7/8 planning
- Natural progression: Sprint 7 establishes test patterns → Sprint 8 hardens API/security → Sprint 9 expands test coverage
- Cohesive theme: All issues focus on Azure Functions reliability (testing + error handling)
- Addresses flaky tests: #331 removes network dependency from SyndicationFeedReader tests (noted as expected failures in CI)
- Deferred Database Improvements Cluster (#322-325) and Architectural Refactors (#309-312, #314) to future sprints for dedicated focus

**Session note (2026-03-20):** Sprint 9 milestone (#4) created this session as part of Sprint 7 kickoff + Sprint 9 planning orchestration. Neo's sprint planning work (trilogy: S7→S8→S9) documented in orchestration-log/2026-03-20T00-51-00-neo.md.

### 2026-03-21: PR #512 Review — DTO Layer Implementation

**Review verdict:** CHANGES REQUESTED  
**PR:** #512 `feature/s8-315-api-dtos` (Trinity's work)  
**Status:** 2 issues found, different agent required for fixes per team protocol

**Findings:**
1. ✅ **Pattern compliance**: Correctly implements decision from `.squad/decisions/inbox/trinity-pr512-dtos.md` — private static `ToResponse`/`ToModel` helpers, no AutoMapper, route IDs as ground truth
2. ✅ **Clean validation removal**: Old "route id must match body id" checks eliminated from `EngagementsController` and `SchedulesController`
3. ✅ **Proper null handling**: `EngagementResponse.ToResponse` uses `?.` operator for optional Talks collection
4. ❌ **BOM character**: `MessageTemplatesController.cs` line 1 has UTF-8 BOM (U+FEFF) before first `using` statement
5. ❌ **Pattern violation**: `TalkRequest.EngagementId` property contradicts "route as ground truth" — engagementId comes from route parameter in `POST /engagements/{engagementId}/talks`, not request body

**Pattern observation for future reviews:**
- Request DTOs should **never** include route parameters as properties — creates misleading API contract and violates single source of truth principle
- Watch for file encoding issues (BOM characters) when reviewing multi-file PRs

**Next step:** Coordinator to assign different agent for fixes (not Trinity per rejection protocol).

### 2026-03-19T20:47:12: PR #514 Review — Pagination Implementation

**Review verdict:** CHANGES REQUESTED  
**PR:** #514 `feature/s8-316-pagination` (Trinity's work)  
**Status:** 2 blocking edge case issues found

**Findings:**
1. ✅ **Pattern compliance**: Correctly implements PagedResponse<T> wrapper pattern with consistent defaults (page=1, pageSize=25)
2. ✅ **Complete coverage**: All 9 list endpoints updated (Engagements, Talks, MessageTemplates, ScheduledItems + 5 schedule variants)
3. ✅ **DTO usage**: All endpoints return Response DTOs wrapped in PagedResponse<T>, ProducesResponseType correctly updated
4. ✅ **No BOM issues**: All files clean UTF-8 (learned from PR #512)
5. ❌ **Division by zero**: PagedResponse.TotalPages calculation throws when pageSize=0 (`TotalCount / PageSize` with no guard)
6. ❌ **Negative Skip()**: `Skip((page - 1) * pageSize)` produces negative value when page=0, causing undefined behavior

**Edge case test scenarios:**
- `GET /engagements?page=1&pageSize=0` → 💥 DivideByZeroException
- `GET /engagements?page=0&pageSize=25` → Returns page 1 data but client thinks it's page 0

**Fix required:**
- Add guard to TotalPages: `PageSize > 0 ? (int)Math.Ceiling(...) : 0`
- Validate parameters in controllers: `if (page < 1) page = 1; if (pageSize < 1) pageSize = 25;`

**Pattern observation for future reviews:**
- Query parameter validation is critical for pagination — defaults are not enough when clients can pass 0 or negative values
- TotalPages calculated properties must guard against division by zero
- Skip/Take patterns assume valid page/pageSize; always validate at controller entry

**Next step:** Coordinator to assign different agent for fixes (not Trinity per rejection protocol).

# Trinity — History

## Summary

Backend dev. Primary domain: API layer, pagination, DTOs, message templates, scope audits.

**Current focus:** Pagination implementation (#316) and scope audit regression tests (#527).

**Key learnings:**
- Always use feature branch + PR workflow; never commit directly to main
- Check if concurrent PRs already fixed issue before implementing
- Scriban templates are database-backed via MessageTemplates table
- Sealed 3rd-party types require typed null in tests, not Mock.Of<T>()

**Implementation summary:**
- Pagination: 8 list endpoints updated with page/pageSize params (defaults: 1, 25)
- Message templates: 20 templates seeded (5 per platform) matching existing fallback logic
- Scope audit: All 34 endpoints verified for fine-grained scope support (Talks.View/All dual pattern)

## Recent Work

### 2026-03-21: Sprint 11 Closeout — All PRs Merged

Sprint 11 complete. All 5 PRs (#551–#555) merged to main. All 5 issues (#544–#548) closed. Three-layer auth exception defence for issue #85 is live on main:
- Layer 1: `RejectSessionCookieWhenAccountNotInCacheEvents` handles `multiple_matching_tokens_detected` (PR #555)
- Layer 2: `MsalExceptionMiddleware` catches MSAL exceptions globally (PR #554)
- Layer 3: `Program.cs` OIDC event handlers map AADSTS codes to friendly messages (PR #553)
- AuthError page (`[AllowAnonymous]`, ResponseCache(NoStore)) serves as the landing page (PR #551)
- Error.cshtml gated by `IsDevelopment()` — 8-char reference ID in production (PR #552)

Sprint 12 tagged with 13 issues.

---

### 2026-03-21: Fix PR #553 — Correct Branch with OIDC Event Handlers (Trinity)

- **Task:** Branch `issue-544` had wrong files committed (AuthError page, HomeController, Error.cshtml from other PRs). Program.cs changes were missing.
- **Root cause:** Ghost committed duplicate work from issues #545 and #547 into issue-544.
- **What the Scribe already did:** Reverted HomeController.cs, deleted AuthErrorViewModel.cs and AuthError.cshtml, restored Error.cshtml in local commits.
- **What I implemented:** Added `Configure<OpenIdConnectOptions>` block to `Program.cs` wiring `OnRemoteFailure` (maps AADSTS650052/700016/invalid_client to friendly messages) and `OnAuthenticationFailed` (generic error redirect). Both handlers call `context.HandleResponse()` before redirecting to `/Home/AuthError`.
- **Build:** ✅ 0 errors. Pushed to origin, commented on PR #553.
- **Lesson:** Scribe may already have partially cleaned up a branch before I work it — check local HEAD vs main carefully before re-reverting.

### 2026-03-21: Scope Audit & Regression Test for Issue #527 (Trinity)

- **Task:** Verify and add regression test for GetTalkAsync fine-grained scope support
- **Finding:** Scope was already fixed in PR #526; issue filed based on pre-merge state
- **What I Implemented:**
  - Regression test GetTalkAsync_WithViewScope_ReturnsTalk added to ensure Talks.View is accepted
  - Full audit of all 34 endpoints across 3 controllers (Engagements, Schedules, MessageTemplates)
  - No gaps found; fine-grained scope rollout from PR #526 is complete
- **PR #531 opened** with full audit table (22 Engagements endpoints, 9 Schedules, 3 MessageTemplates)
- **Lesson:** Check whether concurrent PRs already fixed the issue before adding new code

### 2026-04-01 — Issue #575: Complete AutoMapper Migration — PR #593 Merged

**Status:** ✅ COMPLETE | Branch merged to main

**Orchestration Log:** `.squad/orchestration-log/2026-04-01T171041Z-issue-575.md`

**What I Implemented:**
- Registered `ApiBroadcastingProfile` in `Program.cs` via `AddAutoMapper()`
- Injected `IMapper` into 3 API controllers (EngagementsController, SchedulesController, TalksController)
- Replaced all 8 private static DTO helper methods with `_mapper.Map<T>()` calls
- Route-param fields (Id, EngagementId, Platform, MessageType) set manually post-map per Decision D3
- Removed all 8 TODOs related to AutoMapper placeholders

**Tank's Follow-up:**
- Verified API controller tests work correctly with injected IMapper dependency
- All 43 API tests passing after integration
- Test setup pattern: constructor injection + mock IMapper in test fixture

**Build:** ✅ API project compiles cleanly; 0 errors  
**Tests:** ✅ 43/43 API tests passing  
**Branch:** issue-575-complete-automapper-migration  

**Key Learning:** AutoMapper dependency injection in controllers requires corresponding test fixture adjustments. Always verify test setup matches production DI container registration.

**Unblocked:** #574 Phase 2 (controller paging overloads, pending Morpheus data-store completion)

---

### 2026-03-21: Sprint 11 Closeout — All PRs Merged

- **Task:** Delete all 5 sprint 11 local branches after their PRs were squash-merged to main.
- **Branches deleted:** `issue-544` (-D), `issue-545` (-d), `issue-546` (-D), `issue-547` (-D), `issue-548` (-d)
- **Note:** `issue-545` and `issue-548` deleted cleanly with `-d`. The other three required `-D` because squash merges leave branch tips unrecognized by `git branch --merged`; confirm via `git log --oneline` on main before force-deleting.
- **Remote tracking refs:** Pruned via `git remote prune origin`; no issue-54x refs remained after.
- **Complication:** Local main had a diverged commit, requiring a merge commit during `git pull`. Also had to stash uncommitted changes on a feature branch before switching to main.

---

*For earlier work, see git log and orchestration-log/ records.*


### 2026-04-01 — Issue Specs #575 and #574 (API layer)
- **Relevant specs:** `.squad/sessions/issue-specs-591-575-574-573.md`
- **Issue #575** — AutoMapper migration: replace manual property-by-property mapping in API controllers with AutoMapper profiles. Introduce `ApiBroadcastingProfile`. Route-derived fields (`Id`, `EngagementId`, `Platform`, `MessageType`) must be set manually after mapping (Decision D3).
- **Issue #574 (API layer)** — Add paged action overloads to API controllers once Morpheus completes data store work. Controllers return `PagedResponse<T>` assembled from `PagedResult<T>`.
- **Dependency:** #574 API work is blocked on Morpheus completing data store paging (#574 data layer).

---

### 2026-04-01 — Issue #575: AutoMapper Profile Implementation Complete (Trinity)

- **Task:** Create AutoMapper profile to replace manual ToResponse/ToModel helper methods in API controllers
- **What I Implemented:**
  - Created MappingProfiles/ApiBroadcastingProfile.cs with 8 bidirectional mappings (Engagement, Talk, ScheduledItem, MessageTemplate ↔ DTOs)
  - Registered profile in Program.cs via AddAutoMapper()
  - Injected IMapper into EngagementsController, SchedulesController, MessageTemplatesController
  - Replaced all 8 private static helper methods with _mapper.Map<T>() calls
  - Route-param fields (Id, EngagementId, Platform, MessageType) set manually post-map per Decision D3
- **Build:** ✅ API project compiles cleanly; 0 errors
- **PR:** #593 created (issue-575-automapper-profile-v2 → main)
- **Key Learning:** AutoMapper ForMember(..., opt => opt.Ignore()) required for properties that cannot be resolved by convention (e.g., route params, computed properties like ItemTableName). Manual assignment post-map is the correct pattern for route-derived fields.

---

### 2026-04-01 — Issue #574 Phase 2: Manager Paging + Controller Rewrites (Trinity)

- **Task:** Add paged manager interfaces and rewrite 8 controller paging blocks (Phase 2 of SQL-level paging)
- **Dependency:** Morpheus completed Phase 1 (data store paged methods, PagedResult<T>)
- **What I Implemented:**
  - Added 5 paged methods to IScheduledItemManager, 2 to IEngagementManager (mirroring data store signatures)
  - Implemented all paged methods in ScheduledItemManager and EngagementManager as pure delegators (zero logic)
  - Rewrote 8 controller actions: replaced `GetAllAsync() + Skip((page-1)*pageSize).Take(pageSize)` in-memory paging with `GetAllAsync(page, pageSize)` calls
  - Controllers: SchedulesController (5), EngagementsController (2), MessageTemplatesController (1 - direct data store call)
- **Build:** ✅ 0 errors, unit tests pass
- **PR:** #595 created (issue-574-paging-data-store → main)
- **Key Learning:** Manager layer is pure pass-through for paging; all filtering, ordering, and pagination logic lives in the data store (EF Core queries). PagedResult<T> (data layer) vs PagedResponse<T> (API layer) distinction is critical.

## Team Standing Rules (2026-04-01)
Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only

## Learnings

- PagedResult<T> lives in Domain.Models; used for data layer contracts (List<T> Items, int TotalCount)
- PagedResponse<T> lives in Domain.Models; used for API contracts (IEnumerable<T> Items, int Page, int PageSize, int TotalCount, int TotalPages calculated property)
- Manager paging pattern: pure delegation to data store, no logic, no Skip/Take
- 8 controller actions with in-memory paging identified in SchedulesController (5), EngagementsController (2), MessageTemplatesController (1)
- AutoMapper profiles must be registered in Program.cs via AddProfile<T>() for dependency injection to work
- Manual field assignment post-map is the correct pattern for route-derived fields (Id, EngagementId, Platform, MessageType)
- AutoMapper 16.1.1 requires explicit package reference in test projects even when transitively available

---

### 2026-04-01 — Issue #575: Complete AutoMapper Migration (Trinity)

- **Context:** Issue #575 reopened after PR #593 was merged. PR created ApiBroadcastingProfile but didn't register it or update controllers.
- **Gap found:** ApiBroadcastingProfile existed but wasn't wired up; all 8 manual ToResponse/ToModel helper methods still in controllers with TODO comments.
- **What I Implemented:**
  - Registered `ApiBroadcastingProfile` in `Program.cs` alongside existing `BroadcastingProfile`
  - Injected `IMapper` into EngagementsController, SchedulesController, MessageTemplatesController constructors
  - Replaced all manual `ToResponse(entity)` calls with `_mapper.Map<TResponse>(entity)`
  - Replaced all manual `ToModel(request, id)` calls with `_mapper.Map<TEntity>(request)` + manual `entity.Id = id` assignments
  - Removed all 8 private static helper methods (4 from EngagementsController, 2 from SchedulesController, 2 from MessageTemplatesController)
  - Removed all 8 `TODO: Move to a Automapper profile` comments
  - Added AutoMapper 16.1.1 to API.Tests project to fix test compilation errors
- **Build & Test:** ✅ API project compiles cleanly; all 43 API controller tests passing
- **Branch:** `issue-575-complete-automapper-migration` → pushed to origin
- **Lesson:** PR #593 was incomplete — profile created but not registered, controllers not refactored. Always verify end-to-end integration when completing AutoMapper migrations.
# Trinity - History

### 2026-04-11 — Issue #708: Duplicate API Call Investigation

**Status:** ✅ ROOT CAUSE IDENTIFIED

**Finding:** Issue #708 (duplicate `AddPlatformToEngagementAsync` API calls) is **NOT a backend bug**. Root cause is a **client-side JavaScript bug** in `site.js` form double-submit prevention logic.

**Details:** The form submit event handler returns early on disabled button without calling `event.preventDefault()`, allowing the browser's default form submission to occur even when the button is already disabled.

**Ownership:** Fix belongs to Sparks (Web/UI specialist). Trinity has verified API, routing, and middleware are functioning correctly.

**Decision:** Trinity will not implement the fix—it's out of domain. Coordinator should route to Sparks for implementation.

## Core Context

**Role:** Backend Domain Architect | API design, data models, RBAC, database integration, AutoMapper

**Critical patterns:**
- NO EF Core migrations - schema via raw SQL in scripts/database/migrations/ (naming: YYYY-MM-DD-description.sql)
- AutoMapper for all DTOs/models (registered in Program.cs via profiles); Paging/sorting/filtering at DB level only
- Log injection: value?.Replace("\r", "").Replace("\n", "") ?? "" before logging
- JWT Bearer API controllers: [IgnoreAntiforgeryToken] at class level (cookie auth: do NOT)
- EF Core bool defaults: never .HasDefaultValueSql() on non-nullable value types
- All data stores inject ILogger and log exceptions before returning null/false
- [LoggerMessage] source gen requires Microsoft.Extensions.Logging.Abstractions as DIRECT package reference
- QueueServiceClient: TryAddSingleton with factory reading ConnectionStrings:QueueStorage
- JosephGuadagno.AzureHelpers.Storage.Queues conflicts with Domain.Constants.Queues - use fully qualified name

**Key files:**
- BroadcastingContext.cs: src/JosephGuadagno.Broadcasting.Data.Sql/BroadcastingContext.cs - EF configs in OnModelCreating() line 47+
- BuildHashTagList: All 15 call sites in Functions verified correct - string.Join patterns for persistence/templates intentional

**Completed work:**
- RBAC Phase 1 (#604): 24 files - domain models, EF repos, UserApprovalManager, service registrations
- RBAC Phase 2 (#607): Role management UI, ownership-based delete, CreatedByEntraOid flow
- AutoMapper migration (#575): ApiBroadcastingProfile, IMapper in 3 controllers
- Pagination (#574): Paged interfaces + pass-through managers
- Email domain (#616): Email model, IEmailSender, IEmailSettings, queue constants
- Email managers (#617): EmailSender (Base64 queue), EmailTemplateManager
- EF Core fix (#639): Removed .HasDefaultValueSql("0") from BroadcastingContext.cs
- Epic #667 Sprint 2: SocialMediaPlatformManager, SocialMediaPlatformsController, DTOs, AutoMapper, breaking change fixes
- Epic #667 Sprint 2 test fixes: .Ignore() nav properties in profiles, ISocialMediaPlatformManager param
- CodeQL hardening: SanitizeForLog, CSRF [IgnoreAntiforgeryToken], DB-level filtering, exception logging

**Team standing rules:** Only Joseph merges PRs; All mapping via AutoMapper; Paging at data layer only
### 2026-04-07 — Issue #67: Schedule Item Validation Backend (PR #665 + #665-fix)

**Status:** ✅ COMPLETE & MERGED (after build fix)

**What I Implemented:**

**Core Validation Service:**
1. `ScheduledItemValidationService.cs` — validates source items (Engagements, Talks, SyndicationFeedSources, YouTubeSources) exist before scheduling
2. `IScheduledItemValidationService.cs` — interface for DI
3. `ScheduledItemLookupResult.cs` — response DTO (IsValid, ItemTitle, ItemDetails, ErrorMessage)

**API Endpoint:**
- `SchedulesController.ValidateItem()` — GET `/Schedules/ValidateItem?itemType={0-3}&itemPrimaryKey={id}`
- Returns JSON validation result

**ViewModel Updates:**
- `ScheduledItemViewModel.cs` — added `ItemType` property (ScheduledItemType enum)
- AutoMapper profile updated for bidirectional mapping

**Service Registration:**
- `Program.cs` (Web) — registered `IScheduledItemValidationService` + required managers/datastores

**Build Issue + Fix:**
- PR #665: Build succeeded
- PR #665-fix: Added missing `IScheduledItemValidationService` mock to `SchedulesControllerTests.cs` constructor
- Both PRs merged

**Verification:**
- ✅ Build: 0 errors (both PRs)
- ✅ Tests: 84/84 Web.Tests passing
- ✅ No breaking changes
- ✅ Backward compatible with existing endpoints

**Backend Contract (Ready for UI):**
```
GET /Schedules/ValidateItem?itemType=0&itemPrimaryKey=1

Response:
{
  "isValid": true,
  "itemTitle": "NDC Sydney 2025",
  "itemDetails": "2025-02-10 - 2025-02-14",
  "errorMessage": null
}
```

**Outstanding Work:** Sparks needs to implement UI changes (ItemType dropdown + AJAX validation + results display) in `Views/Schedules/Add.cshtml` and `Views/Schedules/Edit.cshtml`. Full guide in `.squad/decisions.md`.


### 2026-04-08 — Epic #667 Assigned: Social Media Platforms (API Layer)
- **Task:** CRUD endpoints for SocialMediaPlatforms and EngagementSocialMediaPlatforms; DTOs and AutoMapper profiles
- **Dependency:** Morpheus DB work must complete first (blocked on Joseph's architecture answers)
- **Status:** 🔴 BLOCKED — waiting on Morpheus
- **Triage source:** Neo (issue #667)


### 2026-04-08 — Epic #667 Architecture Decisions Resolved
- **Status change:** 🟡 WAITING ON MORPHEUS (unblocked from Joseph's answers)
- **Key decisions affecting Trinity (API):**
  - CRUD endpoints needed: SocialMediaPlatforms (admin) + EngagementSocialMediaPlatforms (per-engagement associations)
  - DTOs: SocialMediaPlatformDto (Id, Name, Url, Icon, IsActive), EngagementSocialMediaPlatformDto (EngagementId, PlatformId, Handle)
  - ScheduledItems endpoints: SocialMediaPlatformId replaces Platform string field
  - MessageTemplates endpoints: SocialMediaPlatformId replaces Platform string field
- **Next:** Begin API work after Morpheus delivers DB migration
=======

### 2026-04-09 — CodeQL Fixes Session Consolidated

**Status:** ✅ CONSOLIDATED | Session log: .squad/log/2026-04-09T00-43-53Z-codeql-fixes.md

**Work Summary:**
- Orchestration log: .squad/orchestration-log/2026-04-09T00-43-53Z-trinity.md (Trinity CodeQL + Neo review fixes documented)
- Session log: .squad/log/2026-04-09T00-43-53Z-codeql-fixes.md (brief summary of security/performance hardening)
- 3 inbox decisions merged to decisions.md:
  - trinity-codeql-fixes.md (log sanitization, CSRF handling, DB filtering, exception logging patterns)
  - neo-pr683-review-complete.md (PR #683 APPROVED for merge)
  - tank-test-platform-id-pattern.md (integer platform IDs in tests, 40 compile errors fixed)
- Deleted 3 inbox files after merge
- Appended team updates to Trinity, Neo, Tank history.md files
- Prepared git commit

**Key Patterns Established:**
1. Log sanitization: `SanitizeForLog()` helper strips `\r\n` (attack prevention)
2. JWT Bearer CSRF: Use `[IgnoreAntiforgeryToken]` at class level (false positive suppression)
3. Data store optimization: DB-level filtering via `GetByNameAsync()` (performance)
4. Exception visibility: Inject `ILogger` and log before returning null (troubleshooting)

**Next:** PR #683 merge approval; Epic #667 Sprints 3-6 ready for Switch/Sparks.

## Learnings

### Backend Audit Patterns (2025-01-XX)

**Audit Scope:** API Controllers, Managers, Azure Functions, Data Layer

**Key Patterns Confirmed:**

1. **Rate Limiting Implementation**
   - Applied globally via `app.MapControllers().RequireRateLimiting(RateLimitingPolicies.FixedWindow)` in Program.cs
   - NO per-action attributes needed — centralized configuration is correct approach
   - Health check endpoints should use `.DisableRateLimiting()` when added (noted in Program.cs:158-160)

2. **Manager Error Handling**
   - Save/Delete operations: Return `OperationResult<T>` with IsSuccess flag
   - GET operations: Return `null` for not-found (simpler pattern for read-only)
   - Controllers check both patterns: `if (result.IsSuccess)` for saves, `if (item is null)` for gets

3. **EventPublisher Exception Handling in Functions**
   - Timer-triggered publishers wrap `IEventPublisher` calls in try/catch(EventPublishException)
   - Log the error with details, then re-throw — don't swallow exceptions
   - Example pattern: `RandomPosts.cs:43-69`, `ScheduledItems.cs:45-59`

4. **Queue Trigger Default Connection**
   - All queue-triggered functions use default `AzureWebJobsStorage` (no explicit Connection= parameter)
   - This is correct — only specify Connection= when using non-default storage account

5. **Data Layer Security**
   - No `FromSqlRaw` or string concatenation found — all queries use LINQ to Entities
   - Paging/filtering/sorting done at database level (not in-memory)
   - AutoMapper separates Domain models from EF models (security boundary)

6. **API Controller Authorization Pattern**
   - `[Authorize]` at class level (all endpoints require auth)
   - `HttpContext.VerifyUserHasAnyAcceptedScope()` on every endpoint (fine-grained RBAC)
   - `[IgnoreAntiforgeryToken]` on API controllers (JWT Bearer auth, not cookies)

**Opportunities for Future Enhancement:**
- Consider standardizing GET operations to return `OperationResult<T>` (currently return null)
- Add Polly circuit breaker to Twitter/Facebook/LinkedIn managers (resilience improvement)
- Timer-triggered functions should return `Task` or `Task<bool>`, not `IActionResult` (semantic correctness)

**Audit Deliverable:** `.squad/decisions/inbox/trinity-backend-audit-findings.md` (comprehensive report with file/line references)

### Issue #708 Root Cause Analysis — Duplicate API Calls (2026-04-11)

**Status:** ✅ ROOT CAUSE IDENTIFIED

**Issue:** `AddPlatformToEngagementAsync` in `EngagementsController` (API) appears to be called twice on form submission.

**Root Cause:** Client-side JavaScript bug in `Web\wwwroot\js\site.js` lines 8-13.

**Technical Analysis:**
The form submit handler attempts to prevent double-submission by disabling the submit button, but has a critical flaw:

```javascript
form.addEventListener('submit', function () {
    if (btn.disabled) return;  // ❌ BUG: Returns without preventing form submission
    btn.disabled = true;
});
```

**Why It Fails:**
1. User double-clicks submit button quickly
2. First click: Button not disabled → disables button → form submits
3. Second click: Button IS disabled → `return` executes → **form still submits because `event.preventDefault()` was not called**

**The Fix:**
Add `event.preventDefault()` when button is already disabled:

```javascript
form.addEventListener('submit', function (e) {
    if (btn.disabled) {
        e.preventDefault();  // ✅ Prevents duplicate submission
        return;
    }
    btn.disabled = true;
});
```

**Impact:**
- NOT an API routing issue
- NOT a middleware issue
- NOT a controller/manager issue
- Client-side only — affects ALL forms in the Web application

**Files Involved:**
- `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/site.js:8-13` (bug location)
- `src/JosephGuadagno.Broadcasting.Web/Views/Engagements/AddPlatform.cshtml` (affected form)
- All other forms using site.js are potentially affected

**Recommendation:** Fix belongs to Sparks (Web/UI specialist). Backend API is functioning correctly.

### 2026-04-11 — Issue #708: Real 400 Error Cause Fixed

**Status:** ✅ RESOLVED

**Finding:** The JavaScript double-submit fix was correct, but a second issue remained: the Web layer ViewModel lacked validation, allowing `SocialMediaPlatformId=0` to be sent to the API, which correctly rejected it with 400 BadRequest.

**Root Causes:**
1. **Missing validation:** `EngagementSocialMediaPlatformViewModel` had no `[Range]` attribute on `SocialMediaPlatformId`
2. **No exception handling:** Web controller didn't catch `HttpRequestException` from API calls

**Fix Applied:**
1. Added `[Range(1, int.MaxValue, ErrorMessage = "Please select a platform.")]` to ViewModel
2. Added try/catch in `EngagementsController.AddPlatform()` to handle API exceptions gracefully

**Files Modified:**
- `src/JosephGuadagno.Broadcasting.Web/Models/EngagementSocialMediaPlatformViewModel.cs`
- `src/JosephGuadagno.Broadcasting.Web/Controllers/EngagementsController.cs`

**Result:** Users now see clear validation errors instead of HTTP exceptions. Defense-in-depth: both Web validation and API validation work together.

**Branch:** `social-media-708` | **Commit:** `0a60493`

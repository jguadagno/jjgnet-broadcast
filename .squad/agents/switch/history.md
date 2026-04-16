# Switch — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Frontend Engineer
- **Joined:** 2026-03-14T16:55:20.779Z

## Learnings

### 2026-04-14T00-30-00Z — Issue #708: Web Service Contract Audit
- **Task:** Audit `EngagementService.AddPlatformToEngagementAsync` after manual testing still failed in the downstream API call path.
- **Outcome:** ✅ Web-side contract hardening complete.
- **What I found:**
  - The Web controller/form flow was already correct; the remaining Web risk was the service depending on anonymous request payloads and direct Domain-model deserialization for the add-platform endpoints.
  - The API now returns a created resource shape (`EngagementSocialMediaPlatformResponse`) and a nested `SocialMediaPlatform` DTO, so I made the Web service consume that response shape explicitly instead of assuming Domain-model JSON.
- **What I changed:**
  - `src\JosephGuadagno.Broadcasting.Web\Services\EngagementService.cs`
    - Added explicit internal request/response contract types for engagement-platform API calls.
    - Mapped API DTO-shaped responses into Domain models before returning to controllers.
    - Applied the same contract mapping to both `GetPlatformsForEngagementAsync` and `AddPlatformToEngagementAsync`.
  - `src\JosephGuadagno.Broadcasting.Web.Tests\Services\EngagementServiceTests.cs`
    - Added/updated service-level tests to verify the request payload, endpoint path, and DTO→Domain mapping.
- **Testing:** `dotnet build .\src\ --no-restore --configuration Release` ✅ and `dotnet test .\src\JosephGuadagno.Broadcasting.Web.Tests\JosephGuadagno.Broadcasting.Web.Tests.csproj --no-build --configuration Release` ✅ (149 passing).

### 2026-04-14T00-00-00Z — Issue #708: Web Audit on social-media-708social-media-708
- **Task:** Audit the Web-side add-platform flow after manual testing still reported HttpRequestException/BadRequest.
- **Outcome:** ✅ Web audit complete; no additional Web-layer fix was needed.
- **What I verified:**
  - `site.js` already uses button-click disabling, so the earlier double-submit race fix is present.
  - `_Layout.cshtml` already renders `TempData["WarningMessage"]`, so warning UX support is present.
  - `EngagementsController.AddPlatform(EngagementSocialMediaPlatformViewModel vm)` already uses the ViewModel-only POST pattern, so the old route/model-binding mismatch is not the active Web issue.
  - The AddPlatform Razor form posts the ViewModel payload expected by the current controller and service flow.
- **Assessment:** If a valid single submit still saves the association and then ends in BadRequest/HttpRequestException, the remaining fault is downstream of Web (API response/contract behavior), not the Razor/JS form flow.
- **Testing:** Focused Issue #708 coverage still passed — 9 Web tests and 10 API platform tests.

### 2026-04-13T17-34-54Z — Issue #708: Double-Submit Race Condition Fix
- **Task:** Fix actual duplicate-submit path for issue #708
- **Outcome:** ✅ Complete
- **Changes:**
  - `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/site.js` (lines 8-26)
  - Moved button disable from form submit event to button click event
  - Rationale: Click event fires BEFORE form submit, preventing race condition
  - Validation-aware: Checks client validation BEFORE disabling
  - Pattern: All future forms automatically protected via site.js
- **Testing:** All 147 Web.Tests pass; build clean
- **Decisions documented:** `switch-real-fix-708.md` (double-submit prevention), `switch-708-conflict-handling.md` (409 handling)
- **Team:** Coordinated with Tank (regression tests) and Trinity (backend validation)
- **Status:** Ready for merge. Complements Tank's regression coverage and Trinity's backend 409 handling.

### 2026-03-16: Issue #105 - Conference Social Fields UI
- Added `ConferenceHashtag` and `ConferenceTwitterHandle` fields to Engagement Create/Edit/Details views
- These fields are nullable `string?` properties added to `EngagementViewModel` in PR #529
- Form pattern: Bootstrap `mb-3` div with label, input, validation span using `asp-for` helpers
- Details pattern: Show only if `!string.IsNullOrEmpty()` to avoid displaying empty rows
- **CRITICAL**: In Razor views, escape `@` in HTML attributes with `@@` (e.g., `placeholder="@@MyConference"`)
- PR #534 created, builds successfully with 0 errors

### 2026-03-20T20:11:20Z — Orchestration Log & Session Completion
- **Task:** Record engagement social fields Web UI completion
- **Orchestration log:** Created 2026-03-20T20-11-20Z-switch.md documenting PR #534 (Engagement Create/Edit/Details views with ConferenceHashtag and ConferenceTwitterHandle)
- **Build status:** Clean build, 0 errors
- **PR status:** #534 open, ready for review
- **Vertical slice completion:** Full engagement social fields feature now spans Domain → Data → API → Web UI (all layers in sync)
- **Pattern documented:** Form accessibility improvements (PR #522) blocked on ViewModel updates, to be rebase once BlueSkyHandle props added


### 2026-04-01 — Issue Spec #573 (Web paging UI)
- **Relevant specs:** `.squad/sessions/issue-specs-591-575-574-573.md`
- **Issue #573** — Add paging UI to Web controllers and views. Pass paging metadata via `ViewBag` (Decision D4). Do not change existing `@model List<T>` view types. Shared `_PaginationPartial.cshtml` reads from `ViewBag`.
- **Dependency:** Blocked on Trinity completing #574 API layer work.


### 2026-04-01 — Issue #605 (RBAC Phase 1 UI)
- **Task:** Implement user approval UI for RBAC Phase 1
- **Created:**
  - AccountController: PendingApproval and Rejected pages (AllowAnonymous)
  - AdminController: Users list, ApproveUser, RejectUser actions (RequireAdministrator)
  - ViewModels: UserListViewModel, ApplicationUserViewModel, RejectUserViewModel
  - Views: Account/PendingApproval, Account/Rejected, Admin/Users
  - AutoMapper mapping: ApplicationUser → ApplicationUserViewModel
  - Admin nav link in _Layout.cshtml (visible to Administrator role only)
- **Patterns learned:**
  - AllowAnonymous required on Account pages accessed by Pending/Rejected users (before approval gate)
  - Bootstrap collapse component for inline forms (rejection notes)
  - Get current user ID from Entra oid claim via IUserApprovalManager.GetUserAsync()
  - Server-side validation of required fields (rejection notes) before calling manager
  - CSRF protection on all POST forms with @Html.AntiForgeryToken()
  - Three-section layout for categorized lists (Pending/Approved/Rejected)
  - Badge counts in card headers for quick overview
  - Consistent use of `<local-time>` component for date display
- **Build status:** Clean build, 0 errors (322 warnings expected baseline)
- **Commit:** 0e43b09 on squad/rbac-phase1 branch
- **Decision log:** `.squad/decisions/inbox/switch-rbac-ui-decisions.md`
- **Dependencies:** Trinity (#604 - IUserApprovalManager), Ghost (#603 - UserApprovalMiddleware, policies)


### 2026-04-01 — RBAC Phase 2 Followup
- **Task:** Implement RoleViewModel, self-demotion guard, fix GetCalendarEvents authorization
- **Created:**
  - RoleViewModel: Web-layer ViewModel for Domain.Models.Role (eliminates Domain reference from Web layer)
  - AutoMapper mapping: Domain.Models.Role → RoleViewModel
- **Updated:**
  - ManageRolesViewModel: Changed IList<Role> → IList<RoleViewModel> (removed Domain.Models reference)
  - AdminController.ManageRoles: Map roles through AutoMapper before assigning to viewmodel
  - AdminController.RemoveRole: Added self-demotion guard (prevents admin from removing their own Administrator role)
  - EngagementsController: Changed class-level auth from RequireContributor → RequireViewer
  - EngagementsController write actions: Added RequireContributor attribute to Edit POST, Add POST, DeleteConfirmed
- **Patterns learned:**
  - Web layer should never directly reference Domain models in ViewModels - always create Web-layer ViewModels
  - Self-demotion guards prevent accidental lockouts (check if user is removing their own critical role)
  - Controller authorization can be layered: class-level for read, method-level for write
  - GetCalendarEvents is a read-only API endpoint, should be accessible to Viewers
  - Razor views don't need changes if ViewModel property names match
- **Build status:** Clean build, 27 warnings (expected baseline)
- **Commit:** fc000a3 on squad/rbac-phase2-followup branch
- **Decision log:** `.squad/decisions/inbox/switch-role-viewmodel-and-auth-fixes.md`


### 2026-04-01 — Issue #613 (Authorization UX Gap Fix)
- **Task:** Fix UX gap where Viewers could access Add/Edit/Delete forms but get 403 on submit
- **Changed:** Added `[Authorize(Policy = "RequireContributor")]` to GET actions in EngagementsController:
  - GET `Add()` action (line 189)
  - GET `Edit(int id)` action (line 75)
  - GET `Delete(int id)` action (line 114)
- **Pattern learned:**
  - Authorization can be layered: class-level for general access (RequireViewer), method-level for elevated access (RequireContributor)
  - GET form actions should enforce the same authorization as their corresponding POST actions to prevent confusing UX
  - Stack multiple attributes on same action (e.g., `[HttpGet]` + `[Authorize(Policy = "RequireContributor")]`)
  - POST actions already had RequireContributor from PR #612 - verified and left unchanged
- **Build status:** Clean build, 0 errors (exit code 0)
- **Branch:** issue-613 (pushed, ready for Tank to add tests)
- **Commit:** f794764

## Team Standing Rules (2026-04-01)
Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only

### 2026-04-09 — Issue #323 (Tags Junction Table Normalization)
- **Task:** Normalize delimited Tags string columns on SyndicationFeedSources and YouTubeSources into a `dbo.SourceTags` junction table
- **Migration:** `scripts/database/migrations/2026-04-09-sourcetags-junction.sql` — creates SourceTags, indexes it, migrates existing data from STRING_SPLIT
- **SourceType enum values:** `'SyndicationFeed'` for SyndicationFeedSources; `'YouTube'` for YouTubeSources
- **EF entity:** `Data.Sql/Models/SourceTag.cs` with `Id`, `SourceId`, `SourceType`, `Tag` properties
- **Navigation:** Both `SyndicationFeedSource` and `YouTubeSource` EF models gained `ICollection<SourceTag> SourceTags`
- **Domain model change:** `Tags` on `SyndicationFeedSource` and `YouTubeSource` changed from `string?` → `IList<string>`
- **AutoMapper:** Forward (EF→Domain) maps `SourceTags.Select(st => st.Tag).ToList()` → `Tags`; Reverse (Domain→EF) writes comma-joined string back to old `Tags` column for backward compat; ignores `SourceTags` nav property
- **Data stores:** All reads use `Include(s => s.SourceTags)`; `SaveAsync` syncs SourceTags via `SyncSourceTagsAsync`; `GetRandomSyndicationDataAsync` uses `!s.SourceTags.Any(st => st.Tag == excludedCategory)` for proper relational filtering
- **HashTagLists:** Added `BuildHashTagList(IList<string>?)` overload; old string overload delegates to it
- **Functions updated:** All callers of `Tags.Split(',')` / `Tags ?? ""` updated for list type; template rendering uses `string.Join(",", tags)`
- **Pattern learned:** For backward-compat migrations, keep old denormalized column and write to both simultaneously; remove in a follow-up migration
- **Branch:** squad/323-tags-junction-table
- **Build status:** All production projects build clean (0 errors); pre-existing test errors in EventPublisherTests and LoadNewPostsTests unrelated to this change

### 2026-04-08 — Epic #667 Assigned: Social Media Platforms (Web Controllers)
- **Task:** Controller actions for managing platform associations in the Web project
- **Dependency:** Trinity API work must complete first
- **Status:** 🔴 BLOCKED — waiting on Trinity → Morpheus → Joseph's answers
- **Triage source:** Neo (issue #667)


### 2026-04-08 — Epic #667 Architecture Decisions Resolved
- **Status change:** 🟡 WAITING ON TRINITY (unblocked from Joseph's answers)
- **Key decisions affecting Switch (Web Controllers):**
  - Engagement detail/edit pages need platform association management (add/remove per-platform handle)
  - IsActive toggle action for SocialMediaPlatforms admin page
  - ScheduledItems and MessageTemplates forms: platform dropdown (FK) replaces free-text Platform field
- **Next:** Begin controller work after Trinity delivers API layer

### 2026-04-13 — Issue #708: AddPlatform Duplicate Submit Handling
- **Problem:** Web controller treated all HttpRequestException errors the same, including 409 Conflict (duplicate platform association)
- **Root cause:** When API returns 409 Conflict (platform already added), user would see error and might retry, causing confusion
- **Solution implemented:**
  - Web controller now catches HttpRequestException and checks StatusCode
  - 409 Conflict → `TempData["WarningMessage"]` with user-friendly message "This platform is already associated with this engagement"
  - Other HTTP errors → `TempData["ErrorMessage"]` with technical details
  - Added `WarningMessage` support to `_Layout.cshtml` (Bootstrap alert-warning styling)
- **Files changed:**
  - `EngagementsController.cs`: AddPlatform POST action now differentiates 409 from other errors
  - `_Layout.cshtml`: Added WarningMessage alert display between Success and Error alerts
  - `EngagementsControllerTests.cs`: Updated and added tests for 409 Conflict and other HTTP error scenarios
- **Pattern learned:**
  - HttpRequestException.StatusCode property available in .NET 5+ enables graceful handling of specific HTTP status codes
  - Warning-level user feedback (alert-warning) is appropriate for "already done" scenarios vs error-level for failures
  - ~~site.js submit handler prevents double-click correctly — no changes needed there (disables button on submit, re-enables on validation failure)~~ ← INCORRECT, see 2026-04-13 followup below
- **Tests:** 7 AddPlatform tests pass, all 147 Web.Tests pass
- **Branch:** social-media-708 (shared with Trinity and Tank for coordinated fix)

### 2026-04-13 — Issue #708 REAL Fix: Double-Submit Race Condition
- **Problem:** Initial #708 fix only addressed UX messaging for 409 Conflicts, but did NOT fix the actual double-submit bug
- **Root cause:** `site.js` had a race condition — using `form.addEventListener('submit')` to disable button allowed multiple rapid clicks to queue multiple submit events before the first one could disable the button
- **Solution implemented:**
  - Changed from `form.addEventListener('submit')` to `btn.addEventListener('click')`
  - Button disables IMMEDIATELY on first click, before form submit event fires, preventing race condition
  - Added client-side validation check BEFORE disabling button (calls `$(form).valid()` if jQuery validation exists)
  - Preserved `invalid-form.validate` handler to re-enable button if validation fails asynchronously
- **Files changed:**
  - `site.js`: Submit prevention moved from form submit event to button click event (lines 8-26)
- **Pattern learned:**
  - To prevent double-submit, disable button on CLICK event, not SUBMIT event (submit fires too late)
  - Check client validation BEFORE disabling to avoid UX issues with invalid forms showing disabled buttons
  - Button click → validate → disable → form submits (atomic operation, no race)
  - Form submit event is too late to prevent race conditions from rapid double-clicks
- **Tests:** All 147 Web.Tests pass (no JS unit tests in this project)
- **Branch:** social-media-708 (same branch as initial 409 messaging work)

## 2026-04-14 — Issue #708: Final Orchestration & Service Contract Hardening

**Status:** ✅ ORCHESTRATION COMPLETE

**Role in Multi-Agent Investigation:** Web/Frontend validation layer — audited Web flow (confirmed correct), identified and fixed service/API contract gap.

**Two-Phase Approach:**
1. **Web Flow Audit:** Confirmed Razor/JS flow is correct (double-submit guard present, warning rendering present, controller binding shape correct)
2. **Service-Layer Hardening:** Added explicit internal DTO types for API request/response contract to remove guesswork and provide stable adapter at Web boundary

**Changes Delivered:**
- File: src\JosephGuadagno.Broadcasting.Web\Services\EngagementService.cs
  - Added explicit internal request/response types for engagement-platform endpoints
  - Implemented DTO-to-Domain mapping before controller handoff
  - Applied to both GET and POST operations
- File: src\JosephGuadagno.Broadcasting.Web.Tests\Services\EngagementServiceTests.cs
  - New focused tests for service layer
  - Verified request payload shape, endpoint path, and mapping behavior
- File: .squad/skills/frontend-patterns/SKILL.md
  - Corrected stale guidance about ViewModel-only POST route duplication

**Coordination with Team:**
- Trinity: Backend duplicate handling confirmed complete (409 Conflict correct)
- Tank: Regression coverage confirmed complete, identified service-layer gap and closure
- Scribe: Orchestration logging and decision documentation

**Findings:**
Real #708 failure was API response generation issue after successful save, not Web-side bug. Web flow is correct. Service/API contract now has explicit boundary contract to prevent future misalignment.

**Status:** Ready for merge. All Web-owned work complete and validated.

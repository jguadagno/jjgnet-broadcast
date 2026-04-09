# Switch — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Frontend Engineer
- **Joined:** 2026-03-14T16:55:20.779Z

## Learnings

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

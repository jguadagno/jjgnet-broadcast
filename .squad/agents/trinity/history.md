# Trinity — History

## Core Context

**Role:** Backend Domain Architect  
**Specialty:** API design, data models, RBAC, database integration, AutoMapper patterns  
**Key Projects:**
- RBAC Phase 1 backend (#604) — 24 files, all scopes audited
- Pagination (#575) — 8 endpoints with paging/sorting at DB level
- Message templates — 20 seeded templates per platform

**Critical patterns:**
- NO EF Core migrations — all schema via raw SQL scripts in `scripts/database/migrations/`
- AutoMapper for all DTOs/models (registered in Program.cs)
- Paging at DB level only (not in managers/controllers)
- Message content: database-backed (MessageTemplates table), not hardcoded
- Sealed 3rd-party types in tests: use typed null, not Mock.Of<T>()

**Active issues:** #615, #616 (email notifications domain layer)

## Summary

Backend dev. Primary domain: API layer, pagination, DTOs, message templates, scope audits, RBAC backend implementation.

**Current focus:** RBAC Phase 1 backend (#604) complete and pushed.

**Key learnings:**
- Always use feature branch + PR workflow; never commit directly to main
- Check if concurrent PRs already fixed issue before implementing
- Scriban templates are database-backed via MessageTemplates table
- Sealed 3rd-party types require typed null in tests, not Mock.Of<T>()
- NO EF migrations — SQL schema managed by raw scripts in `scripts/database/migrations/`
- ALL mapping uses AutoMapper profiles (registered in Program.cs)
- Paging/sorting/filtering at DB level only (not in managers/controllers)

**Implementation summary:**
- Pagination: 8 list endpoints updated with page/pageSize params (defaults: 1, 25)
- Message templates: 20 templates seeded (5 per platform) matching existing fallback logic
- Scope audit: All 34 endpoints verified for fine-grained scope support (Talks.View/All dual pattern)
- RBAC Phase 1: 24 files created (domain models, repositories, managers, AutoMapper profiles, service registrations)

## Recent Work

### 2026-04-02 — PR #610 Round 2: ApplicationClaimTypes constants + SQL CHECK constraints (#603 #606)

**Status:** ✅ COMPLETE | Branch squad/rbac-phase1 | Commit d0aa61a

**What I Fixed (blocking):**

**Fix — UserApprovalMiddleware hardcoded constant (HIGH):**
- Removed `private const string ApprovalStatusClaimType = "approval_status"` from `UserApprovalMiddleware`
- Added `using JosephGuadagno.Broadcasting.Domain.Constants;`
- All usages now reference `ApplicationClaimTypes.ApprovalStatus`

**What I Fixed (non-blocking):**

**Fix — Test files hardcoded claim strings (Finding #2):**
- `UserApprovalMiddlewareTests.cs`: Removed local `ApprovalStatusClaimType` const; all usages replaced with `ApplicationClaimTypes.ApprovalStatus`; added `using` for Domain.Constants
- `AccountControllerTests.cs`: Replaced `"approval_notes"` literal with `ApplicationClaimTypes.ApprovalNotes`; added `using` for Domain.Constants
- `EntraClaimsTransformationTests.cs`: Already used `ApplicationClaimTypes.*` — no changes needed
- Added explicit `ProjectReference` to Domain in `Web.Tests.csproj`

**Fix — SQL CHECK constraints (Finding #3):**
- `table-create.sql`: Added `CK_ApplicationUsers_ApprovalStatus` CHECK (`'Pending', 'Approved', 'Rejected'`) and `CK_UserApprovalLog_Action` CHECK (`'Registered', 'Approved', 'Rejected', 'RoleAssigned', 'RoleRemoved'`)
- `migrations/2026-04-02-rbac-user-approval.sql`: Added idempotent `ALTER TABLE ... ADD CONSTRAINT` blocks for both CHECK constraints (guarded with `IF NOT EXISTS` on `sys.check_constraints`)

**Build:** ✅ 0 errors (279 pre-existing warnings)
**Tests:** ✅ 84/84 Web.Tests passing

---

### 2026-04-03 — PR #611: RBAC Phase 2 Backend Implementation (Trinity)

**Status:** ✅ COMPLETE | Branch squad/rbac-phase2

**What I Implemented:**

**Dead Code Cleanup:**
- Deleted `RejectUserViewModel.cs` (flagged in Phase 1 review, confirmed unused)

**Part 1: AdminController Role Management:**
- Added `GetUserByIdAsync(int userId)` to `IUserApprovalManager` interface and `UserApprovalManager` implementation
- Created `ManageRolesViewModel` with User, CurrentRoles, and AvailableRoles properties
- Added three new actions to `AdminController`:
  - `ManageRoles(int userId)` [GET] - displays role management UI
  - `AssignRole(int userId, int roleId)` [POST] - assigns role with audit logging
  - `RemoveRole(int userId, int roleId)` [POST] - removes role with audit logging

**Part 2: CRUD Controllers Authorization + Ownership-Based Delete:**
- Updated 4 controllers with `[Authorize(Policy = "RequireContributor")]` class-level attribute:
  - `EngagementsController`
  - `SchedulesController`
  - `MessageTemplatesController`
  - `TalksController`
- Implemented ownership-based delete pattern in all Delete actions:
  - Load item first
  - Check if user is Administrator OR is the owner (via `CreatedByEntraOid`)
  - Return `Forbid()` if unauthorized
  - Proceed with delete if authorized
- Set `CreatedByEntraOid` in all Create (POST) actions using `User.FindFirstValue("oid")`
- Added required using statements: `System.Security.Claims`, `Microsoft.AspNetCore.Authorization`

**Key Patterns:**
- Ownership check pattern: Administrators bypass ownership; Contributors require match
- Claim-based ownership: Use `"oid"` claim (Entra Object ID) for user identification
- CreatedByEntraOid assumed present on domain models (Engagements, Talks, ScheduledItems, MessageTemplates) per Morpheus work
- All ownership checks happen in DELETE actions; CREATE sets ownership on insert

**Build:** ⚠️ Expected errors until Morpheus adds `CreatedByEntraOid` to domain models and Switch creates `ManageRoles.cshtml` view
**Dependencies:** 
- Morpheus: Add `CreatedByEntraOid` string property to 4 domain models
- Switch: Create `ManageRoles.cshtml` view

---



**Status:** ✅ COMPLETE | Branch squad/rbac-phase1

**Problem:** Web project crashed at startup with DI validation errors:
`Unable to resolve service for type 'BroadcastingContext' while attempting to activate 'ApplicationUserDataStore'`

**Root cause:** Web's `Program.cs` registered RBAC data stores and managers (which depend on `BroadcastingContext`) but never registered `BroadcastingContext` itself. The API project did this via `builder.AddSqlServerDbContext<BroadcastingContext>("JJGNetDatabaseSqlServer")`.

**Fix:** Added `builder.AddSqlServerDbContext<BroadcastingContext>("JJGNetDatabaseSqlServer");` to Web `Program.cs` directly before `ConfigureApplication(builder.Services)`. No new packages or project references needed — `Aspire.Microsoft.EntityFrameworkCore.SqlServer` and the `Data.Sql` project reference were already present, and AppHost already injected `ConnectionStrings__JJGNetDatabaseSqlServer` into the Web project.

**Build:** ✅ 0 errors

---

### 2026-04-02 — PR #610 Review Fixes (Issues #602–#605)

**Status:** ✅ COMPLETE | Branch squad/rbac-phase1

**What I Fixed:**

**Fix #1 — Middleware Ordering (HIGH):**
- Moved `UseUserApprovalGate()` in `Program.cs` to run AFTER `UseAuthentication()` but BEFORE `UseAuthorization()`. Previously placed after auth, so pending/rejected users hit 403 instead of redirect.

**Fix #2 — DB-level Filtering (MEDIUM):**
- Added `GetUsersByStatusAsync(ApprovalStatus status)` to `IUserApprovalManager` and `UserApprovalManager` (delegates to `applicationUserDataStore.GetByApprovalStatusAsync()` — DB-level `.Where()`)
- Removed in-memory `.Where()` filtering from `AdminController.Users()`. Now makes 3 separate DB calls (one per status).

**Fix #3 — Clean Architecture (MEDIUM):**
- Removed `IRoleDataStore` direct injection from `EntraClaimsTransformation`
- Now uses `userApprovalManager.GetUserRolesAsync(user.Id)` — proper Manager layer call
- Updated tests accordingly

**Fix #4 — Dead Code / approval_notes (LOW):**
- Added `approval_notes` claim injection in `EntraClaimsTransformation` for rejected users (reads `user.ApprovalNotes`). `AccountController.Rejected()` now works correctly.

**Fix #5 — Duplicated Constant (LOW):**
- Created `Domain/Constants/ApplicationClaimTypes.cs` with `EntraObjectId`, `ApprovalStatus`, `ApprovalNotes` constants
- Replaced all hardcoded claim type strings in `EntraClaimsTransformation`, `AdminController`, `AccountController`

**Build:** ✅ 0 errors
**Tests:** ✅ EntraClaimsTransformationTests updated — removed `IRoleDataStore` mock, added `approval_notes` assertion for rejected user test

---

### 2026-04-01 — Issue #604: RBAC Phase 1 Backend Implementation Complete (Trinity)

**Status:** ✅ COMPLETE | Branch pushed to origin/squad/rbac-phase1

**What I Implemented:**

**Domain Layer (7 files):**
- Created ApplicationUser, Role, UserRole, UserApprovalLog models in Domain/Models
- Created ApprovalStatus, ApprovalAction enums in Domain/Enums
- Created RoleNames constants in Domain/Constants (Administrator, Contributor, Viewer)
- Created IApplicationUserDataStore, IRoleDataStore, IUserApprovalLogDataStore interfaces
- Created IUserApprovalManager interface with approve/reject/role-assign operations

**Data Layer (8 files):**
- Created EF entities (Models/ApplicationUser, Role, UserRole, UserApprovalLog)
- Added DbSets to BroadcastingContext (ApplicationUsers, Roles, UserRoles, UserApprovalLogs)
- Configured entity relationships in OnModelCreating (composite PKs, FKs, unique indexes)
- Created ApplicationUserDataStore, RoleDataStore, UserApprovalLogDataStore implementations
- Created RbacProfile AutoMapper profile with custom role mapping logic

**Manager Layer (1 file):**
- Created UserApprovalManager with full business logic:
  - GetOrCreateUserAsync: idempotent user registration with auto-logging
  - ApproveUserAsync/RejectUserAsync: status updates with audit trail
  - AssignRoleAsync/RemoveRoleAsync: role management with validation and logging
  - GetUserRolesAsync, GetAllRolesAsync, GetUserAuditLogAsync: query methods

**Service Registrations (3 files):**
- Added Scoped registrations in Api/Program.cs, Functions/Program.cs, Web/Program.cs
- All RBAC services follow existing DI patterns

**Build Status:** ✅ 0 errors, only expected CS8618 nullable warnings

**Key Patterns Matched:**
- NO EF migrations (schema by Morpheus in SQL scripts)
- Primary constructor pattern: `ClassName(Dep1 dep1, Dep2 dep2)`
- Repository naming: `I[Entity]DataStore` (not IRepository)
- Enum-to-string conversion in manager layer
- Navigation properties in EF entities, ignored in AutoMapper reverse mappings
- Non-clustered PKs, unique indexes, SQL default values in DbContext
- Full audit logging for all user actions (Registered, Approved, Rejected, RoleAssigned, RoleRemoved)

**Decision Document:** `.squad/decisions/inbox/trinity-rbac-phase1-decisions.md`

**Branch:** squad/rbac-phase1 (commit a61d223)

**Next Phase:** API endpoints and Web UI (issues #605, #606 — not in this PR)

---

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

### 2026-04-02 — Issue #616: Email domain models, IEmailSender, IEmailSettings, queue constants

**Status:** COMPLETE | Branch issue-616 | PR #620 | Commit eb01c8a

**What I built:**
- Models/Messages/Email.cs — queue message model (To, From, ReplyTo, Subject, Body)
- Interfaces/IEmailSender.cs — queues emails to Azure Storage Queue (not ACS directly)
- Interfaces/IEmailSettings.cs — ACS config interface (FromAddress, ReplyToAddress, ConnectionString)
- Constants/Queues.cs — added SendEmail = "send-email" and SendEmailPoison = "send-email-poison"
- Note: EmailTemplate.cs was already present from prior work

**Learnings:**
- git stash + checkout race condition can land commits on wrong local branch — always verify HEAD before committing
- EmailTemplate.cs was already created by prior squad work; always check before creating new files
- IEmailSender does NOT inherit from ASP.NET Identity IEmailSender (this project uses Entra ID)
- Email delivery is queue-first: IEmailSender → Azure Storage Queue → Azure Function → ACS

**Dependencies:**
- Issue #617 (EmailSender manager implementation) depends on this PR

---

### 2026-04-05 — Issue #617: EmailSender and EmailTemplateManager

**Status:** COMPLETE | Branch issue-617 | PR #623

**What I built:**
- `EmailSender.cs` — partial class, `QueueServiceClient` + `JosephGuadagno.AzureHelpers.Storage.Queue`, enqueues `Email` as Base64 JSON for Azure Functions compatibility
- `EmailSender.logger.cs` — `[LoggerMessage]` source-generated structured logging (EventId 3000/3001)
- `EmailTemplateManager.cs` — implements `IEmailTemplateManager`, delegates to `IEmailTemplateDataStore`
- `IEmailTemplateManager.cs` in Domain/Interfaces (was already present from prior work; kept existing `GetTemplateAsync(int id)` overload)
- `JosephGuadagno.AzureHelpers.Storage` 1.1.9 + `Microsoft.Extensions.Logging.Abstractions` added to Managers.csproj
- Each project's `ISettings` now extends `IEmailSettings` (adds FromAddress, FromDisplayName, ReplyToAddress, ReplyToDisplayName, AzureCommunicationsConnectionString)
- DI: `QueueServiceClient` registered via factory reading `ConnectionStrings:QueueStorage`; `IEmailSettings`, `IEmailSender`, `IEmailTemplateManager` registered in all 3 projects
- AppHost: added `WithReference(queueStorage)` to Api and Web projects
- Fixed `EmailSenderTests.cs` duplicate class, updated for `QueueServiceClient` constructor pattern

## Learnings
- `JosephGuadagno.AzureHelpers.Storage.Queues` type creates a naming conflict with `Domain.Constants.Queues` — use fully qualified `JosephGuadagno.Broadcasting.Domain.Constants.Queues.SendEmail` in EmailSender.cs
- `[LoggerMessage]` source gen requires `Microsoft.Extensions.Logging.Abstractions` as a DIRECT (not transitive) package reference in the project; it does NOT flow transitively
- `QueueServiceClient` registration pattern: use `TryAddSingleton` with factory lambda reading `ConnectionStrings:QueueStorage` — works for all three project types
- Each project's `Settings` class implements both the project-specific `ISettings` AND (via interface inheritance) `IEmailSettings` from Domain — register both in DI from the same settings instance
- When test files have duplicate class declarations, the compiler reports errors at confusing line numbers — always rewrite the entire file cleanly

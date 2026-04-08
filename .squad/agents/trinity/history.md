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

### Prior Work Archive (Sprint 11 + AutoMapper/Paging)

- **Sprint 11 (#544, #553):** Fixed PR #553 branch (Ghost committed wrong files; Trinity added Configure<OpenIdConnectOptions> to Program.cs with OnRemoteFailure/OnAuthenticationFailed handlers). Sprint 11 PRs merged then reverted via PR #572.
- **Issue #527 scope audit:** Fine-grained scope for GetTalkAsync already fixed in PR #526. Added regression test GetTalkAsync_WithViewScope_ReturnsTalk. All 34 endpoints audited — no gaps.
- **Issue #575 AutoMapper (PR #593 + complete migration):** Created ApiBroadcastingProfile, registered in Program.cs, injected IMapper into 3 API controllers, replaced 8 static helper methods with _mapper.Map<T>(). Route-param fields set manually post-map. Key lesson: PR #593 was incomplete — always verify end-to-end integration.
- **Issue #574 Phase 2 (PR #595):** Added paged manager interfaces, implemented as pure pass-through delegators in ScheduledItemManager + EngagementManager. Rewrote 8 controller actions. PagedResult<T> (data layer) vs PagedResponse<T> (API layer) distinction is critical.
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
- **EF Core bool defaults**: Never use `.HasDefaultValueSql()` on non-nullable `bool` properties. EF Core 8+ cannot distinguish explicit `false` from CLR default, causing startup warnings. The DB default is always redundant for value types — EF Core inserts the C# value directly.
- `BroadcastingContext.cs` location: `src/JosephGuadagno.Broadcasting.Data.Sql/BroadcastingContext.cs` — all entity configurations are in `OnModelCreating()` method starting at line 47

---

### 2026-04-06 — Issue #639: Fix EF Core MessageSent warning

**Status:** ✅ COMPLETE | Branch squad/639-fix-messagesentt-ef-warning | PR #640 | Commit 2bbeb2f

**What I Fixed:**
- Removed `.HasDefaultValueSql("0")` from `ScheduledItem.MessageSent` property configuration in `BroadcastingContext.cs` (line 115-116)
- Root cause: EF Core 8+ cannot distinguish explicit `false` from CLR default for non-nullable `bool` properties with database-generated defaults
- The `.HasDefaultValueSql("0")` call was entirely redundant — EF Core inserts the C# value directly for value types

**Build:** ✅ 0 errors (59 pre-existing warnings)

**Key Pattern:** Never use `.HasDefaultValueSql()` on non-nullable value types — it serves no purpose and triggers warnings in modern EF Core

---

### 2026-04-07 — Issue #323: HashTagLists.BuildHashTagList Caller Audit

**Status:** ✅ COMPLETE | Branch squad/323-tags-junction-table | Audit only, no code changes needed

**What I Audited:**
- Verified all 15 `BuildHashTagList` call sites in Functions project
- Confirmed all callers passing domain model `.Tags` properties (now `IList<string>` after PR #662) correctly use the `IList<string>?` overload via polymorphism
- Call sites verified:
  - **Bluesky:** ProcessScheduledItemFired (2 calls - SyndicationFeedSource, YouTubeSource)
  - **Facebook:** ProcessNewRandomPost, ProcessNewSyndicationDataFired, ProcessNewYouTubeDataFired, ProcessScheduledItemFired (4 calls total)
  - **LinkedIn:** ProcessNewRandomPost, ProcessNewSyndicationDataFired, ProcessNewYouTubeDataFired (3 calls)
  - **Twitter:** ProcessNewRandomPost, ProcessNewSyndicationDataFired, ProcessNewYouTubeData, ProcessScheduledItemFired (4 calls total)

**Legitimate string.Join(",", tags) Patterns Found:**
1. **BroadcastingProfile.cs (AutoMapper):** Converting Domain `IList<string>` → SQL `string` (comma-separated) for persistence — CORRECT
2. **ProcessScheduledItemFired files:** Converting `IList<string>` to comma-separated string for Scriban template variables — CORRECT (templates expect string values)
3. **JsonFeedReader.cs:** Converting JSON array to comma-separated string for `JsonFeedSource.Tags` (still `string?` type) — CORRECT (not yet normalized to IList)

**Key Findings:**
- ✅ NO migration issues found — all callers already using correct overload
- ✅ All `string.Join` patterns are legitimate conversions for specific purposes (DB persistence, template rendering, non-normalized models)
- ✅ Build succeeded with 0 errors (518 pre-existing warnings)
- ✅ The compiler guards this via type safety — `IList<string>` can only call the list overload

**Neo's Suggestion S3:** VERIFIED — all Function callers correctly migrated to `IList<string>?` overload

---

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

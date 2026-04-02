# Morpheus — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Data Engineer
- **Joined:** 2026-03-14T16:37:57.749Z

## Learnings

### 2026-03-20 — PR #512 Merge Conflict Resolution (feature/s8-315-api-dtos → main)
- **Task:** Resolved merge conflicts between PR #512 (DTO pattern) and PR #514 (pagination) that was merged to main first
- **Conflicts:** Three controllers (EngagementsController, MessageTemplatesController, SchedulesController) had overlapping changes
- **Resolution strategy:** Kept BOTH sets of changes — pagination parameters/guards/PagedResponse wrappers from main AND DTO ToResponse/ToModel helpers from PR #512
- **Controllers merged:**
  - EngagementsController: 2 endpoints (GetEngagementsAsync, GetTalksForEngagementAsync)
  - MessageTemplatesController: 1 endpoint (GetAllAsync)
  - SchedulesController: 5 endpoints (GetScheduledItemsAsync, GetUnsentScheduledItemsAsync, GetScheduledItemsToSendAsync, GetUpcomingScheduledItemsForCalendarMonthAsync, GetOrphanedScheduledItemsAsync)
- **History files:** Merged .squad/agents/link/history.md and .squad/agents/neo/history.md preserving all team learnings
- **Outcome:** PR #512 merged successfully via squash merge after conflict resolution
- **Pattern:** When merging complementary refactors (DTOs + pagination), preserve ALL layers — they work together, not against each other

### 2025-01-XX — PR #512 Review Fixes
- **Task:** Fixed blocking issues in Trinity's PR #512 (feature/s8-315-api-dtos)
- **Issue 1 (BOM):** Removed UTF-8 BOM from MessageTemplatesController.cs line 1 using PowerShell UTF8Encoding without BOM
- **Issue 2 (Route-as-ground-truth):** Removed `EngagementId` property from `TalkRequest` DTO per team decision that route parameters are authoritative and should not be duplicated in request body. The `ToModel` method in EngagementsController already injects `engagementId` from the route parameter.
- **Verification:** Build passed with only expected NU1903 and CS8618 warnings (tracked, safe to ignore)
- **Pattern:** DTOs should NOT include fields that come from route parameters — the controller mapping layer injects them at the call site

### 2026-03-20 — PR #514 Pagination Validation Fixes
- **Task:** Fixed Neo's blocking review comments on Trinity's PR #514 (feature/s8-316-pagination)
- **Issue 1:** pageSize=0 caused division-by-zero errors in Skip/Take logic
- **Issue 2:** page=0 caused negative Skip values: `(page - 1) * pageSize` produces -1 * pageSize
- **Solution:** Added consistent inline guards at the top of all 8 paginated endpoints:
  - `if (page < 1) page = 1;`
  - `if (pageSize < 1) pageSize = 1;`
  - `if (pageSize > 100) pageSize = 100;`
- **Controllers updated:**
  - EngagementsController: GetEngagementsAsync, GetTalksForEngagementAsync
  - SchedulesController: GetScheduledItemsAsync, GetUnsentScheduledItemsAsync, GetScheduledItemsToSendAsync, GetUpcomingScheduledItemsForCalendarMonthAsync, GetOrphanedScheduledItemsAsync
  - MessageTemplatesController: GetAllAsync
- **Verification:** Build passed (no-restore build succeeded with expected warnings)
- **Pattern:** All paginated endpoints now clamp page and pageSize before any Skip/Take calculations to prevent runtime errors

### 2026-03-20 — Merge Conflict Resolution (main → feature/s8-316-pagination)
- **Task:** Resolved merge conflicts between PR #512 (DTO layer fixes) and PR #514 (pagination)
- **Conflicts resolved:**
  1. **EngagementsController.cs**: Kept both DTO helper methods (ToResponse/ToModel) from main AND pagination parameters/PagedResponse wrappers
  2. **MessageTemplatesController.cs**: Kept both DTO fixes (BOM removal, ToResponse/ToModel helpers) AND pagination logic (page/pageSize params, guards, PagedResponse wrapper)
  3. **TalkRequest.cs**: Used main's version — NO EngagementId property per route-as-ground-truth decision (Neo's PR #512 fix)

### 2026-03-20T20:11:20Z — Orchestration Log & Session Wrap-Up
- **Task:** Record completion of Sprint 8 work and document team decisions/patterns
- **Orchestration log:** Created 2026-03-20T20-11-20Z-morpheus.md documenting PR #529 merge (Engagement social fields)
- **Decisions consolidated:** 19 inbox files merged into decisions.md, documenting:
  - ViewModel/DTO completeness pattern (all layers update when domain properties added)
  - BlueSkyHandle schema addition (Engagements and Talks tables)
  - SQL Server size cap removal and error surfacing (Issue #324)
  - Pagination parameter validation pattern
- **Session log:** Created 2026-03-20T20-11-20Z-ralph-round2.md summarizing squad status
- **Pattern reinforced:** Domain model changes require simultaneous updates to EF entity, Web ViewModel, and API DTOs to prevent AutoMapper validation failures (PR #529 example)
  4. **link/history.md**: Merged all entries from both branches (append-only)
- **Verification:** Build passed with exit code 0 (expected warnings only)
- **Pattern:** When merging DTO refactors + pagination features, always preserve BOTH layers — DTOs handle input/output shape, pagination adds page/pageSize guards and PagedResponse wrappers

### 2026-03-21 — Issue #324: SQL Server 50MB Size Cap Fix
- **Task:** Fixed SQL Server 50MB database size cap causing silent INSERT failures
- **Root cause:** database-create.sql had `MAXSIZE = 50` (50MB) for data file and `MAXSIZE = 25MB` for log file
- **Two-part solution:**
  1. **Preventive:** Changed database-create.sql to `MAXSIZE = UNLIMITED` for both data and log files
  2. **Defensive:** Added `SaveChangesAsync` override in BroadcastingContext to catch SQL error 1105 (out of space) and throw meaningful InvalidOperationException
- **Migration:** Created 2026-03-21-increase-database-size-limits.sql using ALTER DATABASE MODIFY FILE to update existing databases without data loss or recreation
- **Error handling pattern:** Override SaveChangesAsync in DbContext to catch DbUpdateException with inner SqlException and check for specific error numbers (1105 = insufficient space)
- **Pattern:** Two-layer defense for database capacity issues: (1) remove arbitrary limits in provisioning scripts, (2) surface capacity errors with clear messages if they occur
- **PR:** #517
- **Outcome:** New databases will be provisioned without size caps, existing databases can be migrated, and capacity errors will no longer fail silently

<!-- Append learnings below -->

### 2026-03-21 — PR #529 Feature: ConferenceHashtag and ConferenceTwitterHandle on Engagement (Issue #105)
- **Task:** Add social media metadata fields to Engagements
- **Implementation:** 
  - Added `ConferenceHashtag NVARCHAR(255) NULL` and `ConferenceTwitterHandle NVARCHAR(255) NULL` to `dbo.Engagements`
  - Migration idempotent with IF NOT EXISTS guard
  - Domain model uses nullable string types (`string?`)
  - EF HasMaxLength(255) configured per team convention
- **Status:** PR #529 opened; Neo requested changes for CI blockers:
  - AutoMapper `EngagementViewModel → Engagement` missing new fields (fails `AssertConfigurationIsValid()`)
  - `Data.Sql/Models/Engagement.cs` uses non-nullable `string` (should be `string?` per domain)
- **Downstream:** Once merged, Trinity handles API DTOs, Switch handles Web UI (ViewModel + Razor views)
- **Pattern:** Every new field on Domain.Engagement must also be added to EngagementViewModel to pass AutoMapper CI test
- **Decision documented** in `.squad/decisions/inbox/morpheus-105-social-fields.md`
- **Task:** Added `BlueSkyHandle NVARCHAR(255) NULL` to `dbo.Engagements` and `dbo.Talks`
- **Files changed:** `table-create.sql`, migration `2026-03-21-add-bluesky-handle.sql`, `Domain.Models.Engagement`, `Domain.Models.Talk`, `Data.Sql.Models.Engagement`, `Data.Sql.Models.Talk`, `BroadcastingContext.cs`
- **Pattern:** Nullable nullable column is additive/backward-compatible. No AutoMapper changes needed — convention handles it via `ReverseMap()` (Engagement) and named explicit map (Talk).
- **EF config:** `HasMaxLength(255)` configured in `OnModelCreating` for both columns to match SQL definition.
- **Branch discipline:** Always confirm `git branch --show-current` before committing. Multiple concurrent branch checkouts in parallel shell sessions can put commits on the wrong branch. Use cherry-pick + reset to correct.
- **Migration location:** `scripts/database/migrations/` for one-off ALTER TABLE scripts. `scripts/database/table-create.sql` also updated to keep base schema in sync.


### 2026-04-01 — Issue Spec #574 (data layer)
- **Relevant specs:** `.squad/sessions/issue-specs-591-575-574-573.md`
- **Issue #574** — Add paged overloads to `IScheduledItemDataStore`, `IEngagementDataStore`, `IMessageTemplateDataStore` and their EF Core implementations. Introduce `PagedResult<T>` in `Domain.Models`. Do NOT remove existing parameterless overloads (Decision D2) — they are called from Functions.
- **Dependency:** This data layer work must ship before Trinity can complete the API controller work for #574.

### 2026-04-01 — Issue #574 Phase 1: Paged Data Store Overloads
- **Task:** Implement paging at the SQL layer (Phase 1 of 2-phase refactor)
- **Implementation:**
  - Created `PagedResult<T>` in Domain.Models — contains `List<T> Items` and `int TotalCount`
  - Added paged overloads to 5 domain interfaces: `IScheduledItemDataStore`, `IEngagementDataStore`, `IMessageTemplateDataStore`, `IScheduledItemManager`, `IEngagementManager`
  - Implemented paged overloads in 3 Data.Sql classes: `ScheduledItemDataStore` (5 methods), `EngagementDataStore` (2 methods), `MessageTemplateDataStore` (1 method)
- **Pattern:** IQueryable-fork approach for filtered queries:
  1. Build query with WHERE clause
  2. `CountAsync()` on query for TotalCount
  3. Apply OrderBy + Skip/Take + ToListAsync for Items
  4. Return `new PagedResult<T> { Items, TotalCount }`
- **Sort orders:** ScheduledItems by `SendOnDateTime`, Engagements by `StartDateTime`, MessageTemplates by `Platform` then `MessageType`, Talks by `Name`
- **Preserved:** All existing non-paged methods untouched — Azure Functions depend on them
- **Branch:** `issue-574-paging-data-store` pushed (Phase 1 complete)
- **Blocked:** Build fails with Manager interface errors — expected, Trinity will implement Manager + Controller paged methods in Phase 2
- **Pattern reinforced:** Two-count SQL for pagination — total count for response metadata, filtered count for current page

### 2026-04-02 — Issue #602: RBAC Phase 1 Database Schema Migration
- **Task:** Create database schema for user approval workflow and role-based access control
- **Migration:** `scripts/database/migrations/2026-04-02-rbac-user-approval.sql`
- **Branch:** `squad/rbac-phase1`
- **Tables created:**
  1. `Roles` — lookup table (Administrator, Contributor, Viewer) with UNIQUE constraint on Name
  2. `ApplicationUsers` — keyed on Entra Object ID (oid claim) for multi-tenancy support
  3. `UserRoles` — many-to-many join with composite PK on (UserId, RoleId)
  4. `UserApprovalLog` — audit trail with self-referencing FK to ApplicationUsers for admin actions
- **Key decisions:**
  - Used Entra `oid` claim (NVARCHAR(36)) as stable user identifier instead of email/UPN
  - ApprovalStatus as NVARCHAR(20) string values ('Pending', 'Approved', 'Rejected') with DEFAULT 'Pending'
  - DATETIME2 for audit timestamps (CreatedAt, UpdatedAt) per codebase convention
  - Admin user seed left as manual step with commented SQL template (OID from config, not hardcoded)
- **SQL conventions learned:**
  - Migration header: `-- Migration:`, `-- Issue:`, `-- Date:`, `-- Description:`
  - `USE JJGNet; GO` at start
  - Section headers with `-- ============================================================`
  - GO statements after each DDL block (CREATE TABLE, INSERT)
  - Idempotent seed data with `IF NOT EXISTS` guards
- **Pattern:** Self-referencing FKs are valid when nullable (AdminUserId → ApplicationUsers.Id for audit trail)
- **Coordination:** Trinity working on EF Core models in parallel; SQL-only changes avoid conflicts

### 2026-04-02 — Issue #602: Sync RBAC Tables to Base Schema Scripts
- **Task:** Update `table-create.sql` and `data-create.sql` to include the RBAC tables created by migration `2026-04-02-rbac-user-approval.sql`
- **Files changed:**
  - `scripts/database/table-create.sql` — appended 4 RBAC tables (Roles, ApplicationUsers, UserRoles, UserApprovalLog) after MessageTemplates, matching existing lowercase style with PK_/UQ_/FK_/DF_ constraint naming
  - `scripts/database/data-create.sql` — appended seed data for 3 default Roles (Administrator, Contributor, Viewer) with comment noting admin user seed is a manual step
  - `.squad/decisions/inbox/morpheus-base-scripts.md` — new decision: base scripts must always be updated alongside migrations
- **Key pattern established:** A migration is not complete until `table-create.sql` and `data-create.sql` are updated to the same schema state. Fresh environments provision from base scripts, not migrations.
- **Style note:** Base script uses lowercase SQL and inline constraint syntax (not separate ALTER TABLE). Migration uses UPPERCASE with brackets — both are valid; match the style of the file you're editing.
- **Branch:** `squad/rbac-phase1`

Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only
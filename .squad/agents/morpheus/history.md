# Morpheus — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Data Engineer
- **Joined:** 2026-03-14T16:37:57.749Z

## Prior Work Archive (Sprints 7–10)

- **PR #512 (Sprint 8):** DTO pattern merge conflict resolution — preserved both DTO layer AND pagination together. Pattern: when merging complementary refactors, keep ALL layers. Route params excluded from DTOs; `EngagementId` removed from `TalkRequest`.
- **PR #514 (Sprint 8):** Pagination validation fixes — added `page ≥ 1`, `pageSize ≥ 1`, `pageSize ≤ 100` guards to all 8 paginated endpoints across 3 controllers.
- **PR #517 (Sprint 9, Issue #324):** SQL Server 50MB size cap removed (`MAXSIZE = UNLIMITED`). Added `SaveChangesAsync` override to catch SQL error 1105 and throw `InvalidOperationException`. Migration: `2026-03-21-increase-database-size-limits.sql`.
- **PR #529 (Sprint 10):** Added `ConferenceHashtag`, `ConferenceTwitterHandle` to Engagements; `BlueSkyHandle` to Engagements and Talks. Domain models nullable `string?`, EF `HasMaxLength(255)`. Pattern: every new Domain field requires simultaneous EF entity + ViewModel + DTO update to pass AutoMapper CI test.
- **Base scripts rule established:** A migration is not complete until `table-create.sql` and `data-create.sql` are updated to the same schema state. Fresh environments provision from base scripts, not migrations.

## Learnings

### 2026-04-01 — Issue Spec #574 (data layer)
- **Issue #574** — Add paged overloads to `IScheduledItemDataStore`, `IEngagementDataStore`, `IMessageTemplateDataStore` and EF Core implementations. Introduce `PagedResult<T>` in `Domain.Models`. Do NOT remove existing parameterless overloads — Functions depends on them.

### 2026-04-01 — Issue #574 Phase 1: Paged Data Store Overloads
- Created `PagedResult<T>` in Domain.Models (`List<T> Items`, `int TotalCount`)
- Added paged overloads to 5 domain interfaces; implemented in 3 Data.Sql classes
- **IQueryable-fork pattern:** Build query → `CountAsync()` for TotalCount → OrderBy + Skip/Take + `ToListAsync` for Items
- Sort orders: ScheduledItems by `SendOnDateTime`, Engagements by `StartDateTime`, MessageTemplates by `Platform` then `MessageType`, Talks by `Name`
- Branch: `issue-574-paging-data-store` (Phase 1 complete; Phase 2 = Trinity)

### 2026-04-02 — Issue #602: RBAC Phase 1 Database Schema Migration
- Migration: `scripts/database/migrations/2026-04-02-rbac-user-approval.sql`
- Tables: `Roles`, `ApplicationUsers`, `UserRoles`, `UserApprovalLog`
- Key decisions: Entra `oid` as NVARCHAR(36) user key; ApprovalStatus as NVARCHAR(20) string; DATETIME2 for timestamps
- SQL conventions: `USE JJGNet; GO`, section headers, GO after each DDL, idempotent seed with `IF NOT EXISTS` guards
- Branch: `squad/rbac-phase1`

### 2026-04-02 — Issue #602: Sync RBAC Tables to Base Schema Scripts
- `table-create.sql` — appended 4 RBAC tables after MessageTemplates
- `data-create.sql` — appended seed data for 3 default Roles (Administrator, Contributor, Viewer)
- **Pattern:** Base scripts must always be updated alongside migrations.

### 2026-04-03 — Issue #607: RBAC Phase 2 Ownership Columns
- Added `CreatedByEntraOid NVARCHAR(36) NULL` to Engagements, Talks, ScheduledItems, MessageTemplates
- Domain models: `public string? CreatedByEntraOid { get; set; }` (nullable)
- EF entities: `public string? CreatedByEntraOid { get; set; }` (must match Domain nullability even in `#nullable disable`)
- `BroadcastingContext.cs`: `.HasMaxLength(36)` (no `.IsRequired()`)
- Branch: `squad/rbac-phase2` | Branch discipline: always confirm `git branch --show-current` before committing

### 2026-04-03 — Issue #607: RBAC Phase 2 Followup - CreatedByEntraOid Nullability Fix
- Changed `public string CreatedByEntraOid` → `public string? CreatedByEntraOid` in all 4 Data.Sql entity models
- **Pattern established:** When Domain models have nullable reference types, Data.Sql entity models must match, even in `#nullable disable` contexts.
- Commit: `ebc5ba8`

Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only

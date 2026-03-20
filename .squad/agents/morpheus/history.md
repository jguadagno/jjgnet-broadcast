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

### 2026-03-21 — PR #523 BlueSkyHandle on Engagement and Talk (Issues #167, #166)
- **Task:** Added `BlueSkyHandle NVARCHAR(255) NULL` to `dbo.Engagements` and `dbo.Talks`
- **Files changed:** `table-create.sql`, migration `2026-03-21-add-bluesky-handle.sql`, `Domain.Models.Engagement`, `Domain.Models.Talk`, `Data.Sql.Models.Engagement`, `Data.Sql.Models.Talk`, `BroadcastingContext.cs`
- **Pattern:** Nullable nullable column is additive/backward-compatible. No AutoMapper changes needed — convention handles it via `ReverseMap()` (Engagement) and named explicit map (Talk).
- **EF config:** `HasMaxLength(255)` configured in `OnModelCreating` for both columns to match SQL definition.
- **Branch discipline:** Always confirm `git branch --show-current` before committing. Multiple concurrent branch checkouts in parallel shell sessions can put commits on the wrong branch. Use cherry-pick + reset to correct.
- **Migration location:** `scripts/database/migrations/` for one-off ALTER TABLE scripts. `scripts/database/table-create.sql` also updated to keep base schema in sync.

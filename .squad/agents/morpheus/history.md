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

### 2026-04-04 — PR #662 (Issue #323): Junction Table SourceType Discriminator Pattern
- Fixed EF navigation property data bleed in `dbo.SourceTags` junction table shared between `SyndicationFeedSources` and `YouTubeSources`
- **Problem:** Both entities used IDENTITY(1,1) PKs; EF's `Include(s => s.SourceTags)` returned tags for BOTH SourceId=1 rows (wrong SourceType)
- **Solution:** Direct query pattern with SourceType filter: `broadcastingContext.SourceTags.Where(st => st.SourceId == id && st.SourceType == SourceType).ToListAsync()`
- **Transaction safety:** Wrapped entity save + junction sync in `BeginTransactionAsync`/`CommitAsync` to prevent partial failures
- **EF config:** Added warning comments to BroadcastingContext.OnModelCreating — nav properties kept for writes but NEVER use Include for reads
- Applied to: SyndicationFeedSourceDataStore and YouTubeSourceDataStore (all Get/GetAll/Save/Delete methods)
- Branch: `squad/323-tags-junction-table` | Commit: `1f59fb4`

### 2026-04-09 — PR #662 (Issue #323): Unique Index on SourceTags Junction Table
- Added unique constraint `UX_SourceTags_SourceId_SourceType_Tag` to prevent duplicate tag rows during concurrent SyncSourceTagsAsync calls
- Applied in migration script (2026-04-09-sourcetags-junction.sql) AND EF model (BroadcastingContext.cs) for consistency
- Documented STRING_SPLIT compatibility: SQL Server 2016+ compatible without ordinal arg since tag ordering is irrelevant for seeding
- **Pattern:** Junction table unique constraints protect delete+re-insert sync patterns from race conditions
- Branch: `squad/323-tags-junction-table` | Commit: `8db7dea`

### 2026-04-09 — PR #662 Merge: Resolved merge conflicts with origin/main

- **Conflicts resolved:** 5 code/SQL files + 5 .squad/ files
- **BroadcastingContext.cs:** Kept our warning comments + integrated main's `HasMany` nav property config. Added our `UX_SourceTags_SourceId_SourceType_Tag` unique index to the SourceTag entity config (main had the SourceTag config at bottom but without unique index).
- **SyndicationFeedSourceDataStore.cs / YouTubeSourceDataStore.cs:** Kept our direct-query pattern (`Where(st => st.SourceId == id && st.SourceType == SourceType)`) and `BeginTransactionAsync` throughout all methods. Main's version used `Include()` which causes SourceType bleed.
- **HashTagLists.cs:** Took main's version (intermediate `tagList` variable — functionally identical to our one-liner).
- **2026-04-09-sourcetags-junction.sql:** Kept our version (includes unique index + STRING_SPLIT compat comment). Main had the same file without the unique index (earlier commit of the same migration).
- **.squad/ files:** Used `git checkout --ours` for all 5 — Scribe handles these separately.
- **Build:** 0 errors after resolution. | Commit: `fdc8114`


### 2026-04-08 — Epic #667 Assigned: Social Media Platforms (DB Schema)
- **Task:** Design and implement DB migration for dbo.SocialMediaPlatforms lookup table + dbo.EngagementSocialMediaPlatforms junction table
- **Replacing:** Ad-hoc columns BlueSkyHandle, ConferenceHashtag, ConferenceTwitterHandle on dbo.Engagements and BlueSkyHandle on dbo.Talks
- **Also in scope (pending Joseph):** ScheduledItems.Platform (nvarchar FK→int?) and MessageTemplates.Platform (composite PK — high-impact)
- **Status:** 🔴 BLOCKED — awaiting Joseph's answers to 6 open architecture questions (see .squad/decisions.md → Epic #667 section)
- **Triage source:** Neo (issue #667)


### 2026-04-08 — Epic #667 Architecture Decisions Resolved
- **Status change:** 🟢 UNBLOCKED — Joseph answered all 6 open architecture questions
- **Key decisions affecting Morpheus (DB):**
  - dbo.SocialMediaPlatforms: Id, Name, Url, Icon, IsActive (bool soft delete)
  - dbo.EngagementSocialMediaPlatforms: EngagementId FK + SocialMediaPlatformId FK + Handle; composite PK
  - Talks inherit from parent Engagement (no separate junction table)
  - ScheduledItems.Platform: DROP nvarchar → ADD SocialMediaPlatformId int FK (breaking change)
  - MessageTemplates.Platform: migrate to SocialMediaPlatformId FK (careful — currently in composite PK)
  - Seed: Twitter/X, BlueSky, LinkedIn, Facebook, Mastodon
- **Next:** Morpheus first in pipeline — begin DB migration script

### 2026-04-08 — Epic #667 Phase 1: Database Layer Complete
- **Migration:** `scripts/database/migrations/2026-04-08-social-media-platforms.sql`
  - Created SocialMediaPlatforms table (Id, Name UNIQUE, Url, Icon, IsActive)
  - Created EngagementSocialMediaPlatforms junction table (composite PK on EngagementId + SocialMediaPlatformId)
  - Migrated ScheduledItems.Platform (nvarchar) → SocialMediaPlatformId (int FK) with best-effort string mapping
  - Migrated MessageTemplates.Platform (composite PK component) → SocialMediaPlatformId (int FK, new PK)
  - Dropped old columns: Engagements (BlueSkyHandle, ConferenceHashtag, ConferenceTwitterHandle), Talks (BlueSkyHandle)
  - Seeded 5 platforms: Twitter, BlueSky, LinkedIn, Facebook, Mastodon
- **Base scripts updated:** `table-create.sql` and `data-seed.sql` reflect post-migration schema
- **EF Core:**
  - Created entity models: `SocialMediaPlatform.cs`, `EngagementSocialMediaPlatform.cs`
  - Updated existing entities: Engagement, Talk, ScheduledItem, MessageTemplate (removed old social fields, added FK refs)
  - Updated `BroadcastingContext.cs` with new DbSets, composite PK config, FK relationships, unique indexes
- **Domain:**
  - Created domain models: `SocialMediaPlatform.cs`, `EngagementSocialMediaPlatform.cs`
  - Updated existing: Engagement, Talk, ScheduledItem, MessageTemplate (replaced string Platform with int SocialMediaPlatformId)
- **Repository:**
  - Created `ISocialMediaPlatformDataStore` interface (GetAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync)
  - Implemented `SocialMediaPlatformDataStore` with soft delete logic (IsActive flag)
  - Updated `IMessageTemplateDataStore` and `MessageTemplateDataStore` to use int SocialMediaPlatformId instead of string Platform
- **AutoMapper:** Added mappings for SocialMediaPlatform ↔ EngagementSocialMediaPlatform (bidirectional ReverseMap)
- **DI Registration:** Added `ISocialMediaPlatformDataStore` → `SocialMediaPlatformDataStore` to Api Program.cs
- **Decision doc:** `.squad/decisions/inbox/morpheus-667-db-decisions.md` (migration strategy, PK migration approach, risks)
- **Status:** ✅ Database layer complete. Breaking changes to MessageTemplate interface require updates in Functions and Web (out of scope for Morpheus — Trinity and Cypher to handle).
- **Branch:** `issue-667-social-media-platforms`


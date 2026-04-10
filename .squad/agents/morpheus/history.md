# Morpheus - History

## Core Context

**Role:** Data Engineer | Database schema, EF Core entities, migrations, data stores

**Critical rules:**
- NO EF migrations - schema via raw SQL in scripts/database/migrations/ (naming: YYYY-MM-DD-description.sql)
- Base scripts MUST be updated alongside every migration: table-create.sql + data-create.sql
- SQL conventions: USE JJGNet; GO, section headers, GO after each DDL, idempotent seed with IF NOT EXISTS
- Domain models nullable string? and EF entity models must match, even in #nullable disable
- EF config: primary constructor pattern, non-clustered PKs, .HasMaxLength(), no .HasDefaultValueSql() on value types
- Junction table shared by multiple entities: discriminated direct queries - never Include() for reads
- Concurrent sync (delete+re-insert): BeginTransactionAsync/CommitAsync + unique index on junction table

**Key patterns:**
- Paged data stores: IQueryable fork -> CountAsync() + OrderBy/Skip/Take/ToListAsync
- Sort orders: ScheduledItems by SendOnDateTime, Engagements by StartDateTime, MessageTemplates by Platform/MessageType, Talks by Name
- Soft delete via IsActive flag; AutoMapper: bidirectional ReverseMap()
- Branch discipline: always confirm git branch --show-current before committing

**Completed work:**
- Sprint 8: DTO merge conflict PR #512; pagination guards PR #514
- Sprint 9 PR #517: SQL Server 50MB cap removed; SaveChangesAsync error 1105 override
- Sprint 10 PR #529: Social columns (BlueSkyHandle, ConferenceHashtag, ConferenceTwitterHandle)
- RBAC #602: Roles, ApplicationUsers, UserRoles, UserApprovalLog tables + 3 role seeds
- RBAC #607: CreatedByEntraOid nullable columns on 4 tables + domain/EF models
- PR #662 (#323): SourceTags junction discriminator + unique index UX_SourceTags_SourceId_SourceType_Tag
- Epic #667 Phase 1: SocialMediaPlatforms table, EngagementSocialMediaPlatforms junction, string->int FK migrations, 5 seeds

**Team standing rules:** Only Joseph merges PRs; All mapping via AutoMapper; Paging at data layer only
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

### 2026-04-08 — Epic #667: PR #683 Opened (Draft)
- **PR:** https://github.com/jguadagno/jjgnet-broadcast/pull/683
- **Status:** Draft PR (build broken with 14 expected compile errors from breaking changes)
- **Breaking change:** `IMessageTemplateDataStore.GetAsync(string platform, ...)` → `GetAsync(int socialMediaPlatformId, ...)`
- **Impact:** Functions (Twitter, Facebook, LinkedIn), Api, and Web require updates in Sprint 2 (Trinity) and Sprint 3 (Switch/Sparks)
- **Closes:** #668, #669, #670, #671, #672, #673 (Sprint 1 child issues)
- **Label:** squad:morpheus
- **Pattern:** For large cross-cutting changes, use draft PRs to show DB foundation while acknowledging downstream compilation errors. Clearly document expected failures and remediation owners.

### 2026-04-08 — Epic #667: PR #683 Review Fix (bi-bluesky icon)
- **Review comment:** Reviewer flagged line 78 of migration script — `bi-cloud` should be `bi-bluesky` for BlueSky platform
- **Files fixed:**
  - `scripts/database/migrations/2026-04-08-social-media-platforms.sql` (line 78)
  - `scripts/database/data-seed.sql` (line 78)
- **Pattern:** Bootstrap icon class names for social platforms must match official icon library names. BlueSky uses `bi-bluesky`, not `bi-cloud`.
- **Process:** Fixed in both migration script AND base seed data script (consistency rule).
- **Commit:** `c864f74` — `fix(#667): Use correct Bootstrap icon bi-bluesky for BlueSky platform seed data`
- **PR reply:** Posted via `gh api repos/.../pulls/683/comments/{comment_id}/replies` to confirm fix and close review thread


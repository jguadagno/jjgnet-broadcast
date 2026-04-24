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
- Epic #667 Phase 1: SocialMediaPlatforms table, EngagementSocialMediaPlatforms junction, string→int FK migrations, 5 seeds
- Issue #715: Removed AnyAsync pre-check from AddAsync; capped EF retry to 3×/5s; fixed DisableRetry flag on AddSqlServerDbContext
- Issue #713: Exception logging audit (added LogError to 7 catch blocks)
- Issue #727: Owner-filtered data store overloads (SyndicationFeedSource, YouTubeSource, Engagement, ScheduledItem, MessageTemplate)
- PR #734: TODO comments for CreatedByEntraOid placeholders in SyndicationFeedReader, YouTubeReader
- Role restructuring: Renamed Administrator → Site Administrator; introduced new narrower Administrator role

**Key learnings:**
- Nullable-to-NOT-NULL column promotion: two-step approach (add nullable + backfill, then tighten)
- AnyAsync anti-pattern: Never guard AddAsync with pre-existence check — use PK + DbUpdateException handler
- EF retry override: Set DisableRetry = true in configureSettings to prevent Aspire's default policy from silently overriding explicit cap
- Owner-filtered overloads: Use derived interfaces (not base), apply owner filter before counting/paging, use It.IsAny<CancellationToken>() in Moq
- Custom role seeds: Two-step idempotent block for renames: UPDATE if target doesn't exist, INSERT if still missing
- Exception logging: Every catch block returning OperationResult MUST log the exception before returning

**Team standing rules:** Only Joseph merges PRs; All mapping via AutoMapper; Paging at data layer only

---

## Recent Sessions

### 2026-04-25 — Issue #778 User Collector Configs

- **Work:** Database migration + EF Core entities for per-user collector onboarding
  - Created `scripts\database\migrations\2026-04-25-user-collector-configs.sql` with idempotent IF NOT EXISTS guards
  - Added two tables: `UserCollectorFeedSources` (RSS/Atom/JSON feeds) and `UserCollectorYouTubeChannels` (YouTube channels)
  - Both tables use `CreatedByEntraOid nvarchar(36)`, unique constraint on owner+identifier, nonclustered owner index, IsActive soft-delete flag, DateTimeOffset audit fields
  - Created EF Core entity models: `UserCollectorFeedSource.cs` and `UserCollectorYouTubeChannel.cs`
  - Updated `BroadcastingContext.cs`: Added two `DbSet<>` properties and full `OnModelCreating` configurations matching UserOAuthToken pattern
  
- **EF Configuration Pattern:** Clustered PK with `.IsClustered()`, unique composite index first, then nonclustered single-column indexes, `.HasMaxLength()` on string properties, `.HasColumnType("datetimeoffset")` on DateTimeOffset fields, no `.HasDefaultValueSql()` on value types (bool)
- **Outcome:** Clean build verification; migration script ready for production; entities ready for data store implementations
- **Status:** ✅ COMPLETE

### 2026-04-20 — PR #771 Seed Bootstrap Patch

- **Work:** Fix fresh-environment database bootstrap for collector owner resolution
  - Patched scripts\database\data-seed.sql with placeholder Entra OID variable
  - Applied variable across seeded owner-aware records: Sources, Engagements, ScheduledItems, Talks, MessageTemplates
  - Reused same variable consistently to enable single TODO search-and-replace during fresh environment setup
  
- **Outcome:** Fresh-database seeding now includes CreatedByEntraOid on all owner-aware records; new fail-closed owner resolution can resolve collectors at startup
- **Commit:** 978fc73
- **Validation:** Docs lint clean; pre-existing Functions.Tests compile errors on issue-760 remain unrelated to seed changes
- **Status:** ✅ COMPLETE

---

*Detailed work logs and learnings: See decisions.md for architectural decisions and issue-specific deep dives. Earlier work archived in git history.*

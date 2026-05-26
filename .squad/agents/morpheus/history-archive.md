# Morpheus — History Archive

Archived data engineering context and completed migrations.

---
3. `SocialMediaPlatformDataStore` — Filter: `IsActive`; Sort: `name` (uses memory cache bypass)
4. `SyndicationFeedSourceDataStore` — Sort: `title`, `url`, `author` (SourceTags loaded per-page)
5. `UserCollectorFeedSourceDataStore` — Filter: `DisplayName`; Sort: `displayname`, `feedurl`
6. `UserCollectorYouTubeChannelDataStore` — Filter: `DisplayName`; Sort: `displayname`, `channelid`
7. `UserPublisherSettingDataStore` — Filter: `PlatformName`; Sort: `platformname` (MapToDomain + ProjectForResponse)
8. `YouTubeSourceDataStore` — Sort: `title`, `url`, `author` (SourceTags loaded per-page)

**Manager implementations:**
- `ScheduledItemManager`, `SocialMediaPlatformManager`, `SyndicationFeedSourceManager`, `UserCollectorFeedSourceManager`, `UserCollectorYouTubeChannelManager`, `UserPublisherSettingManager`, `YouTubeSourceManager` — all delegate to data stores

**Special handling patterns:**
- **SyndicationFeedSourceDataStore** & **YouTubeSourceDataStore**: SourceTags loaded via discriminated direct queries AFTER paged result completes (not EF Include)
- **UserPublisherSettingDataStore**: Uses `MapToDomain()` for JSON deserialization (not AutoMapper)
- **SocialMediaPlatformManager**: Paged results bypass in-memory cache (filter/sort-specific results shouldn't use cache)
- **UserPublisherSettingManager**: Applies `ProjectForResponse()` to each paged item to mask raw settings

**Build status:** ✅ Clean; 0 errors; all interfaces fully implemented

**Integration with Trinity:** Pre-staged work in working tree consumed directly by controllers; no wrapper needed

---

### 2026-05-28 — Issue #866 Sort Property Refactor

- **Work:** Replaced hard-coded sort string literals with `nameof().ToLowerInvariant()` for compile-time safety
  - Fixed 9 DataStore files: Engagement, MessageTemplate, ScheduledItem, SocialMediaPlatform, SyndicationFeedSource, YouTubeSource, UserCollectorFeedSource, UserCollectorYouTubeChannel, UserPublisherSetting
  - Converted switch expressions to if/else chains using `nameof(EntityType.PropertyName).ToLowerInvariant()`
  - Total of 18 paged `GetAllAsync` overloads updated (2 per DataStore: base + owner-filtered)
  - All hard-coded strings like `"name"`, `"startdate"`, `"platformid"`, `"author"`, `"channelid"`, etc. now use `nameof()`
  
- **Pattern used:** `var sortByLower = sortBy?.ToLowerInvariant(); if (sortByLower == nameof(Model.Property).ToLowerInvariant()) { ... }`
- **Rationale:** If property names change, the compiler will catch breaks instead of failing silently at runtime
- **Outcome:** Clean build; 0 errors; commit 1378c3b
- **Status:** ✅ COMPLETE

---

### 2026-05-27 — Issue #866 GetAll Consistency

- **Work:** Standardized all `GetAllAsync` overloads with uniform paging, sorting, and filtering pushed to data layer
  - Added sort/filter `GetAllAsync` overloads to 8 data store interfaces, 7 manager interfaces
  - Implemented in 8 data stores: MessageTemplate, ScheduledItem, SocialMediaPlatform, SyndicationFeedSource, UserCollectorFeedSource, UserCollectorYouTubeChannel, UserPublisherSetting, YouTubeSource
  - Implemented in 7 managers: ScheduledItem, SocialMediaPlatform, SyndicationFeedSource, UserCollectorFeedSource, UserCollectorYouTubeChannel, UserPublisherSetting, YouTubeSource
  - Full detail in `.squad/decisions/inbox/morpheus-datalayer-getall.md`
  
- **Key learnings:**
  - `SyndicationFeedSourceDataStore`/`YouTubeSourceDataStore`: SourceTags must be loaded per-page (not all-at-once) in paged overloads — loop over `dbItems` after paged query executes
  - `UserPublisherSettingDataStore`: Uses custom `MapToDomain()` (not AutoMapper) — call after `.Include(SocialMediaPlatform)` and `ToListAsync()`
  - `SocialMediaPlatformManager`: Paged/filtered results bypass in-memory cache since results are query-specific
  - `UserPublisherSettingManager`: Apply `ProjectForResponse()` projection to each item in the paged result before returning
  - `MessageTemplateDataStore`: No manager class — data store used directly by controller
  - CS0121 ambiguity risk: When adding optional-param overloads alongside existing optional-CancellationToken overloads, callers using named `cancellationToken:` arg may see ambiguity — Tank already updated test Moq setups to use the 7-arg explicit pattern to avoid this

- **Status:** ✅ COMPLETE

---

*Detailed work logs and learnings: See decisions.md for architectural decisions and issue-specific deep dives. Earlier work archived in git history.*

---

## Learnings

### N+1 SourceTags pattern (Issue #855)

**Pattern found:** Both `SyndicationFeedSourceDataStore` and `YouTubeSourceDataStore` had a `foreach` loop in ALL `GetAllAsync` overloads (both non-paged and paged) that issued one `broadcastingContext.SourceTags...ToListAsync()` query per row — yielding N+1 DB roundtrips.

**Fix pattern — batched SourceTags load:**
1. Collect page IDs: `var ids = dbItems.Select(x => x.Id).ToList();`
2. Single batch query: `var allTags = await broadcastingContext.SourceTags.Where(st => ids.Contains(st.SourceId) && st.SourceType == SourceType).ToListAsync(ct);`
3. Build dictionary: `var tagsBySourceId = allTags.GroupBy(t => t.SourceId).ToDictionary(g => g.Key, g => g.ToList());`
4. Assign in-memory: `item.SourceTags = tagsBySourceId.TryGetValue(item.Id, out var tags) ? tags : new List<Models.SourceTag>();`

**AsNoTracking pattern:** Add `.AsNoTracking()` at the start of the `IQueryable<T>` chain (immediately after `broadcastingContext.DbSet`) in all read-only `GetAllAsync` overloads. Do NOT add to write operations.

**SQL Server idempotency pattern for indexes:**
```sql
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_...' AND object_id = OBJECT_ID('dbo.TableName'))
    CREATE INDEX IX_... ON dbo.TableName (...);
GO
```

**Applied to:** SyndicationFeedSourceDataStore, YouTubeSourceDataStore (all GetAllAsync overloads), EngagementDataStore, ScheduledItemDataStore (paged overloads only). DB indexes added for Engagements, SyndicationFeedSources, YouTubeSources, ScheduledItems, SocialMediaPlatforms sort/filter columns.

### FeedChecks user separation (Issue #950)

**Pattern:** When a table must support both system-level (timer-triggered, no user) and user-scoped records, use empty string (`''`) as the sentinel EntraOId for system rows rather than NULL. This keeps the column `NOT NULL`, allows a clean composite unique constraint `(Name, EntraOId)`, and avoids nullable comparison edge cases in SQL Server unique indexes.

**Migration pattern:** Add column as `NULL` first → `UPDATE ... SET = ''` → `ALTER COLUMN ... NOT NULL` → `ADD CONSTRAINT DEFAULT`. This two-step approach allows backfilling existing rows safely before tightening nullability.

**Constraint swap pattern:** Use `IF EXISTS` guard before dropping the old single-column unique constraint; use `IF NOT EXISTS` guard before adding the new composite unique constraint. Both guards are idempotent so the migration is safe to re-run.

**Applied to:** `dbo.FeedChecks` — dropped `FeedChecks_Unique_Name`, added `UQ_FeedChecks_Name_EntraOId` on `(Name, EntraOId)`, added `DF_FeedChecks_EntraOId` default `('')`.

### Schema-Sync Validation — UserCollectorYouTubeChannels (2026-05-12)

**Skill created:** `.squad/skills/schema-sync-validation/SKILL.md` — reusable checklist for validating SQL DDL, EF entity, fluent config, domain model, mapper, and SaveAsync are all in sync for any table.

**Key drift patterns found on UserCollectorYouTubeChannels:**

1. **Data annotation / fluent length mismatch (non-breaking):** `CreatedByEntraOid` entity has `[MaxLength(100)]` but SQL is `nvarchar(36)` and fluent config has `.HasMaxLength(36)`. `ChannelId` entity has `[MaxLength(255)]` but SQL is `nvarchar(50)` and fluent config has `.HasMaxLength(50)`. Fluent wins at runtime, but annotations mislead reviewers.

2. **`IsRequired(false)` on NOT NULL column (breaking risk):** `PlaylistId` fluent config uses `.IsRequired(false)` but the SQL column is `NOT NULL`. EF Core will treat the property as optional, which can cause incorrect SQL generation. Should be `.IsRequired()`.

3. **Drop migration without corresponding add migration:** A `2026-05-12-youtube-channels-drop-apikeysecretname.sql` migration exists, but no add migration for `ApiKeySecretName` is present in the migrations folder. `ApiKeySecretName` was apparently added directly to a database without a formal migration. The drop migration uses `IF EXISTS` guard so it's idempotent, but the migrations chain is incomplete. `table-create.sql` baseline is already clean (no `ApiKeySecretName`).

**SaveAsync status:** Already correctly writes `PlaylistId` and `ResultSetPageSize`. Does NOT write `ApiKeySecretName` (already removed). Correctly writes `DisplayName`, `IsActive`, `LastUpdatedOn`. Correctly sets `CreatedOn` only on insert.

**Migrations baseline check:** `table-create.sql` is the cumulative result of all migrations (PlaylistId and ResultSetPageSize present, ApiKeySecretName absent). The migrations sequence itself has the gap noted above but the baseline is authoritative for fresh environments.


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


## Executive Summary

**Morpheus — Data Engineer**

- **Recent focus:** Issue #866 (GetAll consistency), sort property refactoring, pagination/paging patterns
- **Critical rules:** Raw SQL (no EF migrations), script-first approach, nullable matching between domain/EF
- **Key patterns:** Paged data stores, soft delete (IsActive), owner-filtered overloads, concurrent sync with transactions
- **Team impact:** Established data layer standards for pagination, filtering, RBAC integration
- **Key files:** Data stores (paging logic), migration scripts (SQL), EF models with nullable matching

---

## Recent Sessions


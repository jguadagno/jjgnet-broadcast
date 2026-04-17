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
- Issue #715: Removed AnyAsync pre-check from EngagementSocialMediaPlatformDataStore.AddAsync; capped EF retry to 3x/5s

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

## Learnings

### 2026-04-17 — Issue #726: CreatedByEntraOid Promoted to NOT NULL

- **Pattern:** Nullable-to-NOT NULL promotion follows a deliberate two-step approach for the project: (1) add column as NULL with a backfill migration (PR #733), confirm all rows updated, then (2) tighten to NOT NULL in a separate migration after confirmation. This avoids data-loss risk on active environments.
- **Automated collectors:** `SyndicationFeedReader` and `YouTubeReader` use `string.Empty` as a `CreatedByEntraOid` placeholder because they run with no authenticated user context. Future work could inject a system/service-principal OID here.

### 2025-01-30 — Issue #728: TODO Comments for CreatedByEntraOid Placeholders (PR #734)

- **Context:** Sprint 17 issue #728 requires replacing `CreatedByEntraOid = string.Empty` scaffolding in readers with real ownership threading from collector config.
- **Files flagged:**
  - `src/JosephGuadagno.Broadcasting.SyndicationFeedReader/SyndicationFeedReader.cs` — two object initializers (lines 71, 127)
  - `src/JosephGuadagno.Broadcasting.YouTubeReader/YouTubeReader.cs` — one object initializer (line 105)
- **Comment template:** `// TODO: #728 — Replace with ownerOid resolved from collector config. CreatedByEntraOid must never be string.Empty or null. See decisions.md.`
- **Pattern:** When adding technical debt markers to scaffolding, place the TODO comment on the line immediately before the problematic assignment. Include the issue number, a concise remediation instruction, and a reference to architectural context (decisions.md) for team visibility.
- **Branch:** `squad/725-createdbyentraoid-not-null` (PR #734)
- **Commit:** `9ab51a9` — `chore: add TODO comments to CreatedByEntraOid string.Empty placeholders`



- **Change:** Renamed existing `Administrator` role to `Site Administrator` (broader platform admin) and introduced a new, narrower `Administrator` role (personal content management).
- **Files changed:**
  - `scripts/database/data-seed.sql` — replaced single Administrator seed with idempotent rename + re-seed block; kept Contributor and Viewer unchanged.
  - `scripts/database/migrations/2026-04-17-role-restructure.sql` — standalone, safe-to-replay migration for existing production environments.
- **Pattern:** When renaming a seed row, use a two-step idempotent block: (1) UPDATE existing row if rename target does not yet exist, (2) INSERT if it still does not exist after the UPDATE guard. This covers both fresh environments and existing ones in a single replay-safe script.

### 2026-04-15 — Post-PR-718 Remaining 20 s Delay Investigation

- **Context:** After #714 (FromBody) and #715 (AnyAsync removal + EF retry cap) merged in PR #718, a ~20 s delay persisted on `AddPlatformToEngagementAsync`.
- **Root cause:** `DisableRetry = false` in `configureSettings` caused Aspire's `AddSqlServerDbContext` to install its default retry policy (6 retries, 30 s max) **before** `configureDbContextOptions` runs.  Despite the developer's subsequent `EnableRetryOnFailure(3, 5 s)` call theoretically overriding it, the observed latency (~20 s ≈ 3 retries at Aspire's default delay schedule) confirmed the 3/5 s cap was not taking effect.
- **Fix:** Changed `DisableRetry = false` → `DisableRetry = true` in `configureSettings`.  Aspire now skips its retry setup entirely.  The developer's explicit `EnableRetryOnFailure(3, 5 s)` in `configureDbContextOptions` is the single source of truth, capping max retry delay at ~9.2 s.
- **Pattern:** When customising EF Core retry via `configureDbContextOptions` in Aspire's `AddSqlServerDbContext`, **always** set `DisableRetry = true` to prevent Aspire's default retry from silently overriding or co-existing with your explicit cap.
- **Verified:** API project builds with 0 errors.  Decision doc: `.squad/decisions/inbox/morpheus-delay-root-cause.md`.

### 2025-01-27 — Issue #715: AnyAsync pre-check anti-pattern
- **Problem:** `AnyAsync()` before `SaveChangesAsync()` in `AddAsync` triggered the EF Core SQL Server retry policy on transient faults, causing ~28s delays (6 retries × exponential backoff).
- **Fix:** Removed the `AnyAsync` pre-check entirely. Duplicate detection now relies solely on the composite PK `(EngagementId, SocialMediaPlatformId)` and the existing `catch (DbUpdateException ex) when (IsDuplicateAssociationException(ex))` block.
- **EF Retry cap:** `EnrichSqlServerDbContext` (Aspire 13.x) does NOT accept `configureDbContextOptions`. Use `AddSqlServerDbContext` with both `configureSettings` and `configureDbContextOptions` in a single call to set `EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: 5s)`.
- **Pattern:** Never guard `AddAsync` with an `AnyAsync` pre-existence check — it is both a TOCTOU race and a retry-policy amplifier on transient faults. Let the DB PK constraint + `DbUpdateException` handler be the single source of truth.

### 2025-01-28 — Issue #713 Revision: Exception Logging Audit Completion
- **Context:** Neo (Lead) rejected Trinity's initial exception audit work due to missing LogError calls and cross-contamination from issue #704/#705 pagination changes.
- **Problems fixed:**
  1. Added `ILogger<EngagementDataStore>` to primary constructor and inserted `_logger.LogError(ex, ...)` into all 5 catch blocks (SaveAsync, DeleteAsync×2, SaveTalkAsync, RemoveTalkFromEngagementAsync×2).
  2. Added `ILogger<EngagementManager>` to constructor and inserted `_logger.LogError(ex, ...)` into 2 catch blocks (SaveAsync, SaveTalkAsync).
  3. Reverted unrelated pagination changes: `EngagementsController.cs`, `IEngagementService.cs`, `EngagementService.cs`, `Index.cshtml`, `_PaginationPartial.cshtml`, `EngagementsControllerTests.cs` (restored from main).
  4. Fixed broken tests: Added `Mock<ILogger<T>>` to constructors in all DataStore test classes (`EngagementDataStoreTests`, `FeedCheckDataStoreTests`, `ScheduledItemDataStoreTests`, `YouTubeSourceDataStoreTests`, `TokenRefreshDataStoreTests`, `SyndicationFeedSourceDataStoreTests`, `ScheduledItemOrphanTests`) + `EngagementManagerTests`.
- **Build:** Clean success after all fixes (0 errors).
- **Commit:** e306636 — `fix(data,managers): complete exception logging audit - add missing LogError calls (#713)` (10 files changed, -245 lines from reverted pagination tests).
- **Pattern:** When fixing reviewer-rejected work on a feature branch, FIRST identify and revert any cross-contamination from unrelated branches (use `git diff main...branch --name-only` and `git checkout main -- path/to/file`), THEN add the fixes requested (logging calls), THEN fix all test constructors to match updated DI signatures. Never commit incomplete logging instrumentation — every catch block that returns an OperationResult MUST log the exception before returning.


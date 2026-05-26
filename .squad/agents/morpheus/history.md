# History

> Learnings before 2026-04-25 archived to history-archive.md (2026-05-25)

### 2026-05-16 — Issue #972 Missing Publisher Settings Tables

- **Work:** Added four per-publisher settings tables to `scripts\database\table-create.sql`
  - Tables missing: `UserPublisherBlueskySettings`, `UserPublisherTwitterSettings`, `UserPublisherLinkedInSettings`, `UserPublisherFacebookSettings`
  - Root cause: Phase 1 migration (`2026-05-15-publisher-settings-per-publisher-tables.sql`) added the tables but `table-create.sql` was never updated
  - Aspire AppHost only runs `database-create.sql` + `table-create.sql` + `data-seed.sql` — migration files are ignored on fresh boots
  - Copied exact DDL from migration with identical IF NOT EXISTS guards; inserted after existing `UserPublisherSettings` block
- **Outcome:** Build 0 errors/0 warnings; committed to `issue-972-end-user-validation` (7b88ea23)
- **Status:** ✅ COMPLETE

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

## Recent Sessions

### 2026-05-27 — Issue #866 GetAll Consistency (FINAL SESSION)

**Status:** ✅ COMPLETE — All interfaces, managers, and data stores updated; 0 build errors

**Data layer standardization:**
- Implemented 12 new paged `GetAllAsync` overloads: 8 data store interfaces + 7 manager interfaces
- All implementations follow gold standard pattern: `IQueryable<T>` fork → filter → sort switch → `CountAsync()` + `Skip()/Take()` → `ToListAsync()`

**Data stores with sort/filter overloads:**
1. `MessageTemplateDataStore` — Filter: `MessageType`; Sort: `messagetype`, `platformid`
2. `ScheduledItemDataStore` — Filter: `Message`; Sort: `sendondate`, `message`, `messagesent`

## Learnings

### Migration scripts must ALSO land in table-create.sql (2026-05-16)

Every migration under `scripts\database\migrations\` creates tables that ONLY exist in that file until they are also added to `scripts\database\table-create.sql`. The Aspire AppHost bootstraps fresh environments by concatenating ONLY `database-create.sql` + `table-create.sql` + `data-seed.sql`. Migration files are never auto-run by the AppHost. Omitting a migration's DDL from `table-create.sql` causes `CommandError` (table not found) on any fresh Aspire-managed environment. **Rule:** every migration that creates a new table must include a corresponding IF NOT EXISTS block in `table-create.sql` in the same PR.

### EF Core 8 Bool Sentinel Pattern (2026-05-16)

EF Core 8 warns on `bool` properties with `HasDefaultValue`/`HasDefaultValueSql` because it can't distinguish CLR default (`false`) from "not explicitly set". Three resolution strategies:

1. **DB default = `true`, no model initializer** (`SocialMediaPlatform.IsActive`): keep `HasDefaultValueSql("1")` and add `.HasSentinel(true)`. Sentinel = DB default = `true` means EF uses DB default when value is `true`, and correctly inserts `false` when set to false.
2. **DB default = `false`** (`UserPublisherSetting.IsEnabled`, all `UserPublisher*Settings.IsEnabled`): remove `HasDefaultValueSql("0")` entirely. CLR default `false` == DB default, so EF always inserts the C# value — no behavioral change. DB column retains its `DEFAULT (0)` for raw SQL.
3. **DB default = `true`, model has `= true` initializer** (`UserCollectorSpeakingEngagement.IsActive`, `UserCollectorScheduledItem.IsActive`): remove `HasDefaultValue(true)`. The model initializer ensures new instances default to `true`; EF always inserts the C# value.


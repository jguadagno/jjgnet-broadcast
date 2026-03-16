# Morpheus Decisions: Orphan Detection (Issue #274)

## Date
2026-03-16

## Decisions

### 1. SQL Migration file location
Created `scripts/database/migrations/2026-03-16-scheduleditem-integrity.sql`.
The existing pattern places one-off scripts in `scripts/database/`. A `migrations/` subdirectory
was created to distinguish idempotency-sensitive one-time scripts from the base schema.

### 2. Valid ItemTableName values
Valid values enforced by the new CHECK constraint:
- `Engagements`
- `Talks`
- `SyndicationFeedSources`
- `YouTubeSources`

Bad legacy values fixed in the migration:
- `SyndicationFeed` → `SyndicationFeedSources`
- `YouTube` → `YouTubeSources`

### 3. Orphan detection SQL strategy
Used conditional NOT EXISTS per table name rather than dynamic SQL, since the set of valid
table names is fixed and small. This keeps it readable, type-safe, and fast with indexed PKs.

### 4. Return type
`GetOrphanedScheduledItemsAsync()` returns `IEnumerable<Domain.Models.ScheduledItem>` to stay
consistent with the domain layer. EF entity results are mapped via AutoMapper (same pattern as
all other methods in ScheduledItemDataStore).

### 5. Raw SQL approach
Used `FromSqlRaw` on `broadcastingContext.ScheduledItems` because the join condition is
conditional on a string column value — this cannot be expressed cleanly in LINQ without
client-side evaluation. `FromSqlRaw` is the existing EF Core pattern for this scenario.

### 6. Trinity coordination note
Trinity is adding a `ScheduledItemType` enum and renaming `ItemTableName` → `ItemType` on the
Domain model. The orphan detection method uses the EF entity (which still stores the string
`ItemTableName` in the DB) and relies on the existing AutoMapper mapping to produce
`Domain.Models.ScheduledItem`. No changes to the mapping layer are needed from our side.

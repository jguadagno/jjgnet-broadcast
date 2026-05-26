# Decision: NextRunDateUtc Implementation for RandomPosts

**Date:** 2026-05-26  
**Author:** Trinity (Backend Dev)  
**Branch:** `issue-995-per-user-publisher-routing`  
**Status:** IMPLEMENTED — all 286 tests pass

## Context

Neo identified that `RandomPosts.cs` loaded all active `UserRandomPostSettings` rows and performed CRON expression evaluation in C# on every timer tick to determine which settings were "due". This O(n × cron-parse) approach was flagged as unnecessary load as the per-user settings table grows.

## Decision

Move the "is this row due?" filtering into SQL by adding a `NextRunDateUtc` column and updating the column after each dispatch attempt.

## Implementation

### Schema changes

- **Column:** `UserRandomPostSettings.NextRunDateUtc DATETIMEOFFSET NULL` — null means "never run yet, always include".
- **Index:** Filtered index `IX_UserRandomPostSettings_IsActive_NextRunDateUtc WHERE [IsActive] = 1` to make the `GetAllDueAsync` query efficient.
- **Migration:** `scripts/database/migrations/2026-05-26-userrandomposts-add-nextrundate.sql` — idempotent with `COL_LENGTH` and `sys.indexes` guards.

### New operations

- `GetAllDueAsync(DateTimeOffset utcNow)` — `WHERE IsActive AND (NextRunDateUtc IS NULL OR NextRunDateUtc <= utcNow)`.
- `UpdateNextRunAsync(int id, DateTimeOffset? nextRunUtc)` — load-then-save, returns `false` if ID not found.

Both operations are on `IUserRandomPostSettingsDataStore`, `IUserRandomPostSettingsManager`, and their implementations.

### `RandomPosts.cs` redesign

| Before | After |
|--------|-------|
| Load all active rows, parse every cron in C# to filter due rows | `GetAllDueAsync` — SQL does the filter |
| Group by owner OID (`GroupBy`) | Flat `foreach` — no grouping needed |
| Exit loop if cron not due | SQL already excluded not-due rows |
| No `UpdateNextRunAsync` call | Advance `NextRunDateUtc` after every attempt |

**Advance-on-attempt policy:** `NextRunDateUtc` is updated even when no feed item is found, no template exists, or composition returns empty. This prevents an infinite retry storm on the same tick for recoverable misses. The only exception is an **invalid cron expression** — `UpdateNextRunAsync` is skipped because next occurrence cannot be computed.

## Rejected alternatives

- **`ExecuteUpdateAsync`** — skipped to stay consistent with the existing load-then-save pattern in the data store.
- **Cron parse for filtering in C#** — original approach; rejected because it scales with table size and does unnecessary work in application code.
- **`Task.WhenAll` across dispatch** — not considered; team rule prohibits parallel awaits over a shared scoped `DbContext`.

## Manual production steps required

A GitHub issue with label `squad:Joe` must be created for the DBA/deployment operator to run:

```sql
-- Run on the JJGNet production database
ALTER TABLE [UserRandomPostSettings]
    ADD [NextRunDateUtc] DATETIMEOFFSET NULL;

CREATE INDEX [IX_UserRandomPostSettings_IsActive_NextRunDateUtc]
    ON [UserRandomPostSettings] ([NextRunDateUtc])
    WHERE [IsActive] = 1;
```

The migration file `scripts/database/migrations/2026-05-26-userrandomposts-add-nextrundate.sql` is idempotent and can also be run directly.

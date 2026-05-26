# Decision: RandomPosts — Push Cron Evaluation into SQL via `NextRunDateUtc`

**Date:** 2026-05-26T15:39:38.587-07:00
**Author:** Neo (Lead)
**Status:** ✅ PROPOSED

---

## Problem

`RandomPosts.cs` runs on a 1-minute timer. On every tick it calls `GetAllActiveAsync()`,
which returns every active `UserRandomPostSettings` row for every user. The function then
loops through every row, parses the `CronExpression` string via Cronos, and evaluates
whether the expression fired in the last minute. If not due, the row is silently skipped.

**This is 1,440 full-table reads per day where the vast majority of rows are evaluated
and thrown away in C#.** A user with a daily schedule (e.g. `0 9 * * *`) has their row
read and discarded 1,439 times per day, and their Cronos expression is parsed and
evaluated every single one of those 1,439 times.

---

## Root Cause

The `UserRandomPostSettings` table has no persisted "due date" column. The schema stores
a `CronExpression` string and nothing else about when it should next fire. The function
has no choice but to load everything and evaluate in memory.

---

## Comparison: How ScheduledItems Solves This

`ScheduledItems.GetScheduledItemsToSendAsync()` filters in SQL:

```
WHERE MessageSent = 0 AND SendOnDateTime <= GETUTCDATE()
```

Only rows that are actually due come back. This is the correct pattern. `UserRandomPostSettings`
needs the same — but because its schedules are *recurring* (not one-shot), the write-back
updates a `NextRunDateUtc` column rather than flipping a `MessageSent` flag.

---

## Decision: Add `NextRunDateUtc` and filter in SQL

### Schema change

Add one column to `UserRandomPostSettings`:

```sql
[NextRunDateUtc] DATETIMEOFFSET NULL
```

Add a supporting index:

```sql
CREATE NONCLUSTERED INDEX IX_UserRandomPostSettings_Due
    ON [dbo].[UserRandomPostSettings] ([IsActive] ASC, [NextRunDateUtc] ASC);
```

`NULL` means "never run yet" — these rows should be included on the first query so they
get their initial `NextRunDateUtc` computed on first dispatch.

### New data store method: `GetAllDueAsync`

Replace `GetAllActiveAsync()` in the `RandomPosts` function with a new method:

```csharp
Task<List<UserRandomPostSettings>> GetAllDueAsync(
    DateTimeOffset utcNow,
    CancellationToken cancellationToken = default);
```

SQL predicate:

```
WHERE IsActive = 1
  AND (NextRunDateUtc IS NULL OR NextRunDateUtc <= @utcNow)
```

`GetAllActiveAsync()` is still useful for the Web/API management path and must not be removed.

### Write-back after dispatch

After successfully dispatching a row (queue message sent), compute and persist the next
run time:

```csharp
var nextRun = cronExpression.GetNextOccurrence(DateTimeOffset.UtcNow.UtcDateTime, TimeZoneInfo.Utc);
if (nextRun.HasValue)
{
    await userRandomPostSettingsDataStore.UpdateNextRunAsync(settings.Id, nextRun.Value);
}
```

This is one `UPDATE` per actually-fired row — cheap and only happens when a post is sent.

### New data store method: `UpdateNextRunAsync`

```csharp
Task UpdateNextRunAsync(int id, DateTimeOffset nextRunUtc, CancellationToken cancellationToken = default);
```

---

## Alternatives Considered

### In-memory cron cache with TTL
Load all active settings once, cache for N minutes, evaluate in memory. Reduces SQL round-trips
but still runs Cronos against every row on every cache miss. Adds memory pressure and
complexity to the Functions app for minimal gain. **Rejected** — data-layer filtering is always
preferable to application-layer filtering (established team pattern).

### Change feed / event-driven trigger
Use SQL Change Feed or a Service Bus trigger fired by a schedule. Requires external
infrastructure and is overkill for this use case. **Not feasible** within the current
architecture without significant platform changes.

### Compute `NextRunDateUtc` entirely in SQL
SQL Server has no CRON parser. The next occurrence must be computed in C# via Cronos. The
write-back approach keeps the computation in C# (where we already have Cronos) and persists
the result. No all-SQL approach is practical here.

---

## Quantifying the Inefficiency

| Schedule frequency | Fires per day | Rows loaded-but-skipped per day (at 10 active rows) |
|--------------------|---------------|------------------------------------------------------|
| Daily (`0 9 * * *`) | 1 | 14,390 |
| Every 4 hours | 6 | 14,340 |
| Hourly | 24 | 14,160 |
| Every 30 minutes | 48 | 13,920 |

With `NextRunDateUtc`, all 1,440 daily queries become **index seeks returning 0 rows**
on non-firing minutes. The Cronos parse-and-evaluate cost disappears entirely — cron is
only evaluated once per row when it fires, to compute the next occurrence.

---

## Implementation Scope

1. **SQL script** — add `NextRunDateUtc` column and `IX_UserRandomPostSettings_Due` index
   to `scripts/database/table-create.sql` (or a migration script if needed post-deploy).
2. **Domain model** — add `NextRunDateUtc` property to `UserRandomPostSettings`.
3. **EF data model** — add property to `Data.Sql.Models.UserRandomPostSettings` and update
   `BroadcastingContext` / mapping profile.
4. **Interface** — add `GetAllDueAsync` and `UpdateNextRunAsync` to `IUserRandomPostSettingsDataStore`.
5. **Data store** — implement both methods in `UserRandomPostSettingsDataStore`.
6. **Function** — update `RandomPosts.cs` to call `GetAllDueAsync`, and call
   `UpdateNextRunAsync` after each successful dispatch.
7. **Tests** — update data store tests, manager tests, and function tests.

---

## Notes

- `GetAllActiveAsync()` must remain — the Web/API management controllers use it.
- The `NULL` initial state is deliberate: new rows fire once on the first minute they are
  queried, then advance to the proper cadence.
- This pattern is consistent with the team preference to push filtering down to the data
  layer (decisions.md — established pattern).

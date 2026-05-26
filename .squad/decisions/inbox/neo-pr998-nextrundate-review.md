# Neo PR Review — #998 NextRunDateUtc Efficiency Fix

**Date:** 2026-05-26T16:12:32.404-07:00  
**Reviewer:** Neo  
**PR:** #998 — feat(#995): per-user publisher routing — replace Event Grid dispatch  
**Branch:** `issue-995-per-user-publisher-routing`  
**Verdict:** **BLOCKED ❌ — REQUEST CHANGES**

---

## What Was Reviewed

The `NextRunDateUtc` query efficiency fix: schema migration, EF entity, domain model,
`IUserRandomPostSettingsDataStore`, `IUserRandomPostSettingsManager`, `UserRandomPostSettingsDataStore`,
`UserRandomPostSettingsManager`, `RandomPosts.cs`, and all new tests (Functions, Managers, Data.Sql layers).

---

## Hard Gate Results

| Gate | Result |
|------|--------|
| `DATETIMEOFFSET` in SQL | ✅ |
| `DateTimeOffset` in C# | ✅ |
| Migration idempotent | ✅ |
| `table-create.sql` matches migration | ✅ |
| `GetAllDueAsync` handles NULL `NextRunDateUtc` | ✅ |
| `[IgnoreAntiforgeryToken]` on API controllers | ✅ |
| `UpdateNextRunAsync` called after dispatch failure | ✅ (code) |
| Log injection — `ownerOid` / `CronExpression` | ✅ |
| Log injection — `syndicationFeedItem.Title` in `LogInformation` | **❌ BLOCKING** |

---

## Blocking Item

**File:** `src/JosephGuadagno.Broadcasting.Functions/Publishers/RandomPosts.cs`, line 136

`syndicationFeedItem.Title` is passed directly to `logger.LogInformation()` without
`LogSanitizer.Sanitize()`. Title is a model property containing externally-sourced RSS content.
Per the `cs/log-forging` hard pre-commit gate, this is a blocking violation.

**Required fix:**
```csharp
logger.LogInformation(
    "Dispatched random post '{Title}' (Id: {Id}) to queue '{Queue}' for owner '{OwnerOid}'",
    LogSanitizer.Sanitize(syndicationFeedItem.Title),
    syndicationFeedItem.Id, queueName,
    LogSanitizer.Sanitize(ownerOid));
```

---

## Non-Blocking Observations

1. **Missing dispatch-failure test**: No test verifies that `UpdateNextRunAsync` is called when
   `SendMessageAsync` throws. Implementation is correct; test coverage for this hard gate path
   should be added before merge.

2. **Invalid cron → always-eligible row**: When `CronExpression.Parse` fails, `AdvanceNextRunAsync`
   is not called; the row stays `NextRunDateUtc = NULL` and is fetched on every run. Intentional per
   test `RunAsync_WhenInvalidCronExpression_SkipsSettingAndDoesNotUpdateNextRun`. Generates 1,440
   warnings/day per bad row — consider a metric event or deduplication.

3. **`AdvanceNextRunAsync(CronExpression)` uses `DateTimeOffset.UtcNow` at call time** rather than
   the `utcNow` captured at function entry. Negligible drift for minute-level cron scheduling.

---

## What Is Well Done

- Schema: Both migrations are idempotent; `table-create.sql` is consistent with migrations.
- Data layer: `GetAllDueAsync` LINQ correctly handles NULL (first-run) and `<= utcNow`.
- Dispatch failure: `AdvanceNextRunAsync` is outside the `try/catch` block — hard gate met.
- Tests: NULL case, due case, future case, inactive case, and mixed set all covered.
- 1,279 tests pass with 0 failures.

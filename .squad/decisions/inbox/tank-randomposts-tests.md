# Decision: `NextRunDateUtc` Test Coverage for RandomPosts Efficiency Fix

**Author:** Tank  
**Date:** 2026-05-28  
**Branch:** `issue-995-per-user-publisher-routing`  
**Status:** Ready for review

---

## Context

Trinity implemented `GetAllDueAsync(DateTimeOffset utcNow)` and `UpdateNextRunAsync(int id, DateTimeOffset? nextRunUtc)` on `UserRandomPostSettings`, and rewrote `RandomPosts.RunAsync` to use `GetAllDueAsync` (replacing `GetAllActiveAsync` + inline cron-time check) and call `UpdateNextRunAsync` after each successful dispatch.

Tank's task was to write comprehensive tests for these additions.

---

## Decisions Made

### 1. Three-layer test strategy

Tests were written at all three relevant layers:

- **Data store** (`UserRandomPostSettingsDataStoreTests.cs`) — EF in-memory; verifies `GetAllDueAsync` filter logic (`NULL` = always due, past = due, future = not due) and `UpdateNextRunAsync` (found, not-found, clear-to-null)
- **Manager** (`UserRandomPostSettingsManagerTests.cs`) — Moq; verifies delegation and `ArgumentOutOfRangeException` on `id <= 0`
- **Function** (`RandomPostsTests.cs`) — Moq; verifies `RunAsync` uses `GetAllDueAsync`, calls `UpdateNextRunAsync` after successful dispatch, skips update on no-feed-item, and skips on invalid cron

### 2. `UpdateNextRunAsync` default setup in `RandomPostsTests`

The test class constructor registers a default `UpdateNextRunAsync` setup returning `true` (`It.IsAny<int>()`, `It.IsAny<DateTimeOffset?>()`). This prevents dispatch tests from failing on the post-dispatch update call, keeping tests focused on their own behavior.

### 3. `CreateSettingsAsync` helper extended in data store tests

Added `nextRunDateUtc` optional parameter to the existing `CreateSettingsAsync` helper so individual tests can seed precise `NextRunDateUtc` values without duplicating entity construction.

---

## Traps Found

**Working-tree vs HEAD confusion:** Trinity's additions existed in the working tree but were not yet committed to HEAD. Reading files via the editor returned stale HEAD content, causing duplicate definitions when Tank added the same members. Lesson: always run `git diff HEAD` to check for uncommitted working-tree changes before adding new interface members or properties.

---

## Test Count Delta

| Assembly | Before | After | Delta |
|---|---|---|---|
| `Functions.Tests` | 14 | 20 | +6 |
| `Managers.Tests` | 205 | 211 | +6 |
| `Data.Sql.Tests` | 285 | 295 | +10 |
| **Total passed** | **1233** | **1279** | **+46** |

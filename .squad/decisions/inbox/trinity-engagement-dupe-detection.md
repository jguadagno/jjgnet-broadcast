# Decision: Engagement Duplicate Detection (feature/engagement-dupe-detection)

**Date:** 2026-07-11
**Author:** Trinity (Backend Dev)
**Branch:** `feature/engagement-dupe-detection`

## Context

`LoadNewSpeakingEngagements` is a timer-triggered Azure Function that pulls engagements from an
external reader and saves them to the database. Running it repeatedly (e.g. on redeploy or manual
trigger) would re-insert the same engagements, causing duplicate rows.

## Natural Key Chosen

| Field | Rationale |
|-------|-----------|
| `Name` | Title of the speaking engagement |
| `Url` | Canonical event URL — unique per event |
| `StartDateTime.Year` | Scopes collisions to the same calendar year |

Combined: **Name + Url + Year** — this mirrors the existing `GetByNameAndUrlAndYearAsync` already
present on `IEngagementDataStore` and `EngagementManager` from a previous sprint. No new query
method was needed.

## Detection Approach

"Check then skip" in the Function, before the save pipeline:

```csharp
var existingEngagement = await engagementManager.GetByNameAndUrlAndYearAsync(
    item.Name, item.Url, item.StartDateTime.Year);
if (existingEngagement != null)
{
    logger.LogDebug("Skipping duplicate speaking engagement '{Name}' ({Url}, {Year})", ...);
    continue;
}
```

- Duplicates are **skipped** (not upserted) — re-running the collector is now idempotent.
- Logged at **Debug** level (low-noise, appropriate for an expected skip path).
- Pattern matches `LoadNewPosts` (SyndicationFeed) and `LoadNewVideos` (YouTube) collectors.

## Files Changed

| File | Change |
|------|--------|
| `Domain/Interfaces/IEngagementManager.cs` | Added `GetByNameAndUrlAndYearAsync` to interface (was implemented but not exposed) |
| `Functions/Collectors/SpeakingEngagement/LoadNewSpeakingEngagements.cs` | Added duplicate check + skip before `SavePipeline.ExecuteAsync`; removed TODO comment |
| `Functions.Tests/Collectors/LoadNewSpeakingEngagementsTests.cs` | New — 3 tests covering duplicate-skip, new-save, and no-items paths |

## Why Not Upsert?

`EngagementManager.SaveAsync` already does an implicit "find by natural key and update" when
`entity.Id == 0`. The collector does not need to update existing engagements — if the reader
returns a known engagement, the correct behavior is to skip it so that any manual edits made via
the Web UI are preserved.

## Test Count

3 new unit tests in `JosephGuadagno.Broadcasting.Functions.Tests.Collectors.LoadNewSpeakingEngagementsTests`.

# Decisions

> Team-binding choices that survive session transitions. Updated on 2026-05-26.

---

## Decision: Replace Event Grid Dispatch with Database-Driven Per-User Queue Dispatch

**Date:** 2026-05-26T08:47:03.046-07:00  
**Author:** Neo (Lead)  
**Status:** CONFIRMED by Joseph Guadagno  
**Trigger:** Issue #995 — Random Post interface needed

### Summary

Event Grid subscriptions are statically registered infrastructure — every subscriber on a topic receives every event. Per-user publisher selection (e.g., user A wants Random Posts to LinkedIn only; user B wants Bluesky and Facebook) is incompatible with Event Grid's broadcast model. 

**Decision: Remove Event Grid from publisher dispatch. Replace with direct per-user queue dispatch.**

The publisher function (`Publishers/RandomPosts.cs`) will:
1. Query `UserRandomPostSettings` WHERE `IsEnabled = 1` AND `NextRunAt <= now`
2. For each user, apply per-user settings (frequency, cutoff date, excluded categories)
3. For each platform the user has enabled (via `UserPublisherEventTypes`), enqueue `SocialMediaPublishRequest` directly

**Why:** Storage Queues are already provisioned and already the terminal delivery mechanism. The four `ProcessNewRandomPost*` intermediate functions become obsolete.

### New Tables Required

**`UserRandomPostSettings`**
```sql
CREATE TABLE dbo.UserRandomPostSettings (
    Id                 INT IDENTITY PRIMARY KEY,
    CreatedByEntraOid  NVARCHAR(36) NOT NULL UNIQUE,
    IsEnabled          BIT NOT NULL DEFAULT (0),
    FrequencyMinutes   INT NOT NULL DEFAULT (1440),
    NextRunAt          DATETIMEOFFSET NULL,
    CutoffDate         DATETIMEOFFSET NULL,
    ExcludedCategories NVARCHAR(MAX) NULL,
    CreatedOn          DATETIMEOFFSET NOT NULL DEFAULT (GETUTCDATE()),
    LastUpdatedOn      DATETIMEOFFSET NOT NULL DEFAULT (GETUTCDATE())
)
```

**`UserPublisherEventTypes`**
```sql
CREATE TABLE dbo.UserPublisherEventTypes (
    Id                    INT IDENTITY PRIMARY KEY,
    CreatedByEntraOid     NVARCHAR(36)  NOT NULL,
    SocialMediaPlatformId INT NOT NULL REFERENCES dbo.SocialMediaPlatforms(Id),
    EventType             NVARCHAR(50)  NOT NULL,
    IsEnabled             BIT NOT NULL DEFAULT (1),
    CONSTRAINT UQ_UserPublisherEventTypes UNIQUE (CreatedByEntraOid, SocialMediaPlatformId, EventType)
)
```

### Scope — Phase 1 (#995)

- SQL tables: `UserRandomPostSettings`, `UserPublisherEventTypes`
- Domain: `IUserRandomPostSettings`, `IUserRandomPostSettingsManager`, `IUserPublisherEventTypeManager`
- Functions: Rewrite `Publishers/RandomPosts.cs`; remove four `*/ProcessNewRandomPost.cs` intermediate functions
- API: CRUD endpoints for user settings
- Web: Settings page with per-publisher toggles
- Deprecate: `IRandomPostSettings` (global) and `PublishRandomPostsEventsAsync`

### Phase 2 (Future)

- Same pattern for ScheduledItems and New Content per-user dispatch
- Evaluate whether collector events (SyndicationFeed, YouTube, Engagements) still benefit from Event Grid
- Remove Event Grid infrastructure once all publisher dispatch migrated

### Confirmed Answers (Joseph — 2026-05-26)

1. **Event type flags:** New junction table `UserPublisherEventTypes` (more extensible)
2. **Per-user scheduling:** Fixed intervals (`FrequencyMinutes`) with `NextRunAt` tracking
3. **Collector events:** Keep Event Grid for SyndicationFeed/YouTube/Engagements fan-out (deferred to Phase 2)
4. **Backward compatibility:** Auto-seed `UserRandomPostSettings` for existing users from global settings

### References

- Issue: https://github.com/jguadagno/jjgnet-broadcast/issues/995
- Key files: `src/Functions/Publishers/RandomPosts.cs`, `src/Data/EventPublisher.cs`, `src/Functions/event-grid-simulator-config.json`, `scripts/database/table-create.sql`

---

## Decision: Web downstream service logging pattern

- **Date:** 2026-05-28
- **Author:** Trinity
- **Status:** Proposed

### Summary

Web service wrappers under `src\JosephGuadagno.Broadcasting.Web\Services\` now treat downstream API nulls and delete failures as observable events instead of silent fallbacks.

### Pattern

- Inject `ILogger<TService>` alongside `IDownstreamApi` using the existing primary constructor pattern.
- `GetForUserAsync<T>()` returning `null` logs a warning with the operation name and relevant identifiers before returning the existing empty/null fallback.
- `GetOptionalForUserAsync<T>()` returning `null` is treated as a legitimate not-found case and does not warn.
- `PostForUserAsync<TRequest, TResponse>()` and `PutForUserAsync<TRequest, TResponse>()` returning `null` log a warning with the operation name and identifiers before returning `null`.
- Delete calls check for `204 NoContent`; `null` or any other status logs a warning and returns `false`.
- Any string value that could come from user input or route/query data must be sanitized with `LogSanitizer.Sanitize()` before it is written to a log.

### Why

This keeps the Web layer's current null/bool return contracts intact while giving Joe enough telemetry to diagnose downstream API failures and distinguish them from legitimate not-found responses.

---

# Decision: Issue #995 Architecture Confirmed

**Date:** 2026-05-26T08:59:04.287-07:00  
**Author:** Neo (Lead)  
**Status:** CONFIRMED  
**Trigger:** Joseph answered the five open architecture questions on
issue #995.

---

## Confirmed Decisions

1. **Storage model**
   - Joseph confirmed **Option B** from the earlier analysis.
   - In practice, that means using **dedicated normalized user-owned
     tables** for routing and scheduling instead of adding event-type flags
     onto the existing `UserPublisher*Settings` tables.
   - Existing per-platform publisher settings tables remain responsible
     for publisher-specific configuration only.

2. **Scheduling model**
   - Scheduling is **CRON-like**.
   - A user can define **multiple schedules per event type**.
   - Each schedule combines **event type + cron expression/frequency +
     target publisher(s)**.

3. **Collector event routing**
   - **Event Grid is removed for collector events too**, not just Random
     Post.
   - New speaking engagements, blog posts, videos, and other
     collector-driven events will use the same **user-selectable publisher
     routing** model.

4. **Random Post execution model**
   - `Publishers\RandomPosts.cs` should run on **one global timer every
     minute**.
   - It should poll **all users** and determine which schedules are due,
     following the same broad execution pattern as
     `Publishers\ScheduledItems.cs`.
   - Do **not** create per-user timer functions or per-user function
     instances.

5. **Migration and seeding**
   - Seed the new `UserRandomPostSettings` table with **Joseph's current
     global defaults**.
   - After the seed path exists, the old **global Random Post settings**
     can be removed.

---

## Resulting Implementation Scope

- Add new per-user scheduling and routing tables.
- Add `UserRandomPostSettings` for per-user content filtering
  (`CutoffDate`, `ExcludedCategories`).
- Replace Event Grid dispatch with direct per-user publisher routing for
  both Random Post and collector events.
- Add API and Web support so users can manage schedules, publisher
  targets, and Random Post settings.
- Remove the old global Random Post settings path after seed migration is
  in place.

---

### 2026-05-26: Issue #995 schema review

**By:** Trinity

**What:** Recommended that the new per-user scheduling/routing tables
use `CreatedByEntraOid nvarchar(36)`, `datetimeoffset` timestamps,
platform FKs, and normalized junction tables; also flagged that
`UserPublisherSchedules` likely needs `TimeZoneId` unless Joseph
declares all cron schedules UTC-only.

**Why:** This matches the existing `UserPublisher*Settings` /
`UserCollector*` ownership pattern in `Data.Sql` and avoids baking
publisher names or numeric user IDs into new routing tables. The
timezone choice changes the table shape and schedule recalculation
logic, so it needs to be settled before implementation starts.

---

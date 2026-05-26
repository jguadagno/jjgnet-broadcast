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

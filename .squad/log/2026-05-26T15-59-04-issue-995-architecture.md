# Session Log — Issue #995 Architecture — 2026-05-26T15:59:04Z

## Summary

Joseph confirmed 5 architecture decisions for #995 per-user publisher routing feature. Event Grid to be removed entirely from publisher dispatch path.

## Decision Details

**Confirmed Decisions:**

1. **Event type storage:** New junction table `UserPublisherEventTypes` (more extensible than denormalized columns on `UserPublisherSettings`)
2. **Per-user scheduling:** Fixed intervals (`FrequencyMinutes`) with `NextRunAt` tracking (not cron expressions)
3. **Collector events:** Keep Event Grid for SyndicationFeed/YouTube/Engagements fan-out (Phase 2 deferred)
4. **Backward compatibility:** Auto-seed `UserRandomPostSettings` for existing users from global settings
5. **Architecture:** Replace Event Grid dispatch entirely with direct per-user queue dispatch for publishers

## Scope — Phase 1 (#995)

**New Tables:**
- `UserRandomPostSettings` — per-user Random Post frequency, cutoff, excluded categories
- `UserPublisherEventTypes` — user × platform × event type junction (extensible for future event types)

**Code Changes:**
- Domain/Managers: `IUserRandomPostSettings`, `IUserRandomPostSettingsManager`, `IUserPublisherEventTypeManager`
- Functions: Rewrite `Publishers/RandomPosts.cs` for per-user dispatch; remove four `ProcessNewRandomPost*` intermediate functions
- API/Web: CRUD endpoints and settings UI

## Next Steps

Trinity (assigned #995) will implement this architecture using the confirmed decisions as requirements.

---

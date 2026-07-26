# Publishing Testing Guide — RandomPost and ScheduledItems

This document describes how the **RandomPost** and **ScheduledItems** publishing pipelines work after the Event Grid removal (PR #998 / Issue #995). Use it to test and validate the end-to-end flow.

---

## Overview

Both pipelines follow the same pattern:

1. A **timer-triggered Azure Function** runs on a fixed interval (every minute).
2. It queries the database for records that are **due** for the current UTC time.
3. For each due record it resolves the user's **publisher mappings** (which social platforms the user has opted in to for that action type).
4. It publishes directly to each mapped platform using the appropriate manager.
5. It records the result in the database (last run time, next run time, etc.).

There is **no Azure Event Grid** involved — the function calls the managers directly.

---

## RandomPost Pipeline

### Timer Function

**File:** `src/JosephGuadagno.Broadcasting.Functions/Publishers/RandomPosts.cs`

- Runs every minute via CRON `0 * * * * *`.
- Calls `IUserRandomPostSettingsManager.GetAllDueAsync(utcNow)` to find every user whose random-post schedule is due.
- For each `UserRandomPostSettings` record:
  - Loads the user's publisher mappings for action type `RandomPost`.
  - Calls `IRandomPostManager.GetRandomPostAsync(userId)` to select a post.
  - Publishes via each mapped platform manager.
  - Updates `LastRunDateUtc` and `NextRunDateUtc` on the settings record.

### Data Model

| Table | Key columns |
|-------|-------------|
| `UserRandomPostSettings` | `EntraOId`, `IsEnabled`, `CronExpression`, `LastRunDateUtc`, `NextRunDateUtc` |
| `UserEventPublisherMappings` | `EntraOId`, `ActionTypeId`, `SocialMediaPlatformId` |

### Schedule Evaluation

- `CronExpression` is a standard CRON string (evaluated by **Cronos**).
- Expressions are stored and evaluated in **UTC**. The UI displays converted local time.
- `NextRunDateUtc` is calculated after each run: `CronExpression.GetNextOccurrence(utcNow, inclusive: false)`.

### Testing Checklist — RandomPost

1. **Create a `UserRandomPostSettings` record** via the Web UI (Settings → Random Post Settings) or the API (`POST /api/userRandomPostSettings`).
   - Set `IsEnabled = true`.
   - Set a `CronExpression` that fires soon (e.g., `* * * * *` for every minute, or a specific UTC time).
   - Confirm `NextRunDateUtc` is populated.
2. **Create at least one publisher mapping** via the Web UI or API (`POST /api/userEventPublisherMappings`) with `ActionTypeId = RandomPost`.
3. **Wait for the function to fire** (watch Application Insights or local Functions console).
4. Verify:
   - A post was published to each mapped platform.
   - `LastRunDateUtc` and `NextRunDateUtc` were updated on the settings record.
   - No exception was thrown.
5. **Disable** (`IsEnabled = false`) and confirm the function skips the record.

---

## ScheduledItems Pipeline

### Timer Function

**File:** `src/JosephGuadagno.Broadcasting.Functions/Publishers/ScheduledItems.cs`

- Runs every minute via CRON `0 * * * * *`.
- Calls `IScheduledItemManager.GetAllDueAsync(utcNow)` to find every scheduled item whose `ScheduledDateUtc <= utcNow` and that has not yet been published.
- For each due `ScheduledItem`:
  - Loads the user's publisher mappings for action type `ScheduledItem` (keyed by `EntraOId` on the item).
  - Publishes via each mapped platform manager.
  - Marks the item `IsPublished = true` and records `PublishedDateUtc`.

### Data Model

| Table | Key columns |
|-------|-------------|
| `ScheduledItems` | `EntraOId`, `ScheduledDateUtc`, `IsPublished`, `PublishedDateUtc` |
| `UserEventPublisherMappings` | `EntraOId`, `ActionTypeId`, `SocialMediaPlatformId` |

### Testing Checklist — ScheduledItems

1. **Create a `ScheduledItem`** via the Web UI (Scheduled Items → New) or API.
   - Set `ScheduledDateUtc` to a time 1–2 minutes in the future (convert from your local time).
2. **Create at least one publisher mapping** for `ActionTypeId = ScheduledItem` for the same user.
3. **Wait for the function to fire** at or after `ScheduledDateUtc`.
4. Verify:
   - A post was published to each mapped platform.
   - `IsPublished = true` and `PublishedDateUtc` is set on the item.
5. Confirm the item does **not** publish again on the next function tick (idempotency).

---

## Publisher Mappings

### What they control

`UserEventPublisherMappings` is the routing table. Each row says:

> *"For user X, when action Y fires, publish to social platform Z."*

### Action Types (`ActionTypeId`)

| Value | Meaning |
|-------|---------|
| `RandomPost` | A random post selected by the RandomPost timer |
| `ScheduledItem` | A manually scheduled item |
| `CollectorFeedSource` | A new article from an RSS/Atom feed collector |
| `CollectorYouTubeChannel` | A new YouTube video from a collector |
| `CollectorSpeakingEngagement` | A new speaking engagement from a collector |

### Managing mappings

- **Web UI:** Settings → Publisher Mappings
- **API:** `GET/POST/DELETE /api/userEventPublisherMappings`

---

## Date/Time Conventions

| Layer | Convention |
|-------|-----------|
| Database | All `datetime` / `datetimeoffset` columns stored in **UTC** |
| C# models | `DateTimeOffset` (UTC) |
| Web UI display | Converted to **browser local time** before display |
| Web UI edit forms | User enters **local time**; controller converts to UTC before saving |
| CRON evaluation | Evaluated against **UTC** clock inside the Azure Functions runtime |

---

## Useful API Endpoints for Testing

```
# Random Post Settings
GET    /api/userRandomPostSettings
POST   /api/userRandomPostSettings
PUT    /api/userRandomPostSettings/{id}
DELETE /api/userRandomPostSettings/{id}

# Publisher Mappings
GET    /api/userEventPublisherMappings
POST   /api/userEventPublisherMappings
DELETE /api/userEventPublisherMappings/{id}

# Scheduled Items
GET    /api/scheduledItems
POST   /api/scheduledItems
PUT    /api/scheduledItems/{id}
DELETE /api/scheduledItems/{id}
```

---

## Manual Cleanup Remaining (see linked GitHub issue)

The following items require manual action by the repository owner and are **not** automated by the PR:

- Delete the Azure Event Grid topics that were used for RandomPost and ScheduledItem events.
- Remove the corresponding Event Grid subscription configurations from Azure.
- Remove any global RandomPost app settings that have been superseded by per-user `UserRandomPostSettings`.

See the linked GitHub issue for step-by-step instructions.

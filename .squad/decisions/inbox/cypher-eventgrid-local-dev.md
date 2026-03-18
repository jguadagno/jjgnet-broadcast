# Decision: Enable All 5 Event Grid Topics in Local Dev

**Date:** 2026-07-11  
**Author:** Cypher (DevOps Engineer)  
**Branch:** `feature/s4-5-eventgrid-local-dev`  
**Task:** S4-5 — Enable all Event Grid topics in local dev event-grid-simulator

---

## Problem

The `event-grid-simulator-config.json` in the Functions project only had `new-youtube-item` enabled
(`"disabled": false`). The remaining 4 topics were disabled or misconfigured, blocking local
end-to-end testing of all event-driven code paths.

### Bugs found in addition to disabled topics

| Topic | Bug |
|---|---|
| `new-speaking-engagement` | `"port": true` — invalid type, should be `60102` |
| `new-random-post` | Missing `FacebookProcessNewRandomPost` and `LinkedInProcessNewRandomPost` subscribers |
| `new-speaking-engagement` | Facebook subscriber `name` label was `FacebookProcessNewSpeakingEngagementDataFired`; corrected to `FacebookProcessSpeakingEngagementDataFired` to match the function name in the endpoint |

---

## Changes Made

### `src/JosephGuadagno.Broadcasting.Functions/event-grid-simulator-config.json`

| Topic | Before | After |
|---|---|---|
| `new-random-post` (port 60101) | `disabled: true`, 2 subscribers (Bluesky, Twitter only) | `disabled: false`, all 4 subscribers |
| `new-speaking-engagement` (port 60102) | `port: true` (bug!), `disabled: true` | `port: 60102`, `disabled: false` |
| `new-syndication-feed-item` (port 60103) | `disabled: true` | `disabled: false` |
| `new-youtube-item` (port 60104) | unchanged — already correct | unchanged |
| `scheduled-item-fired` (port 60105) | `disabled: true` | `disabled: false` |

### `local.settings.json` — No changes needed

All 5 topic endpoint entries were already present with correct ports:
- `new-random-post` → `https://localhost:60101/api/events`
- `new-speaking-engagement` → `https://localhost:60102/api/events`
- `new-syndication-feed-item` → `https://localhost:60103/api/events`
- `new-youtube-item` → `https://localhost:60104/api/events`
- `scheduled-item-fired` → `https://localhost:60105/api/events`

### `AppHost.cs` — No changes needed

The Aspire AppHost uses `WithExternalHttpEndpoints()` on the Functions project, which already
covers all event-grid-simulator HTTP webhook traffic. No per-topic wiring is required at the
AppHost level.

---

## Subscriber Topology (as wired)

| Topic | Subscribers |
|---|---|
| `new-random-post` | BlueskyProcessRandomPostFired, FacebookProcessNewRandomPost, LinkedInProcessNewRandomPost, TwitterProcessRandomPostFired |
| `new-speaking-engagement` | BlueskyProcessSpeakingEngagementDataFired, FacebookProcessSpeakingEngagementDataFired, LinkedInProcessSpeakingEngagementDataFired, TwitterProcessSpeakingEngagementDataFired |
| `new-syndication-feed-item` | BlueskyProcessNewSyndicationDataFired, FacebookProcessNewSyndicationDataFired, LinkedInProcessNewSyndicationDataFired, TwitterProcessNewSyndicationDataFired |
| `new-youtube-item` | BlueskyProcessNewYouTubeDataFired, FacebookProcessNewYouTubeDataFired, LinkedInProcessNewYouTubeDataFired, TwitterProcessNewYouTubeDataFired |
| `scheduled-item-fired` | BlueskyProcessScheduledItemFired, FacebookProcessScheduledItemFired, LinkedInProcessScheduledItemFired, TwitterProcessScheduledItemFired |

All subscribers use port `59833` (Azure Functions local host) with `disableValidation: true`.

---

## Local Dev Architecture Note

Events flow: Publisher → `https://localhost:6010X/api/events` (simulator) → simulator fans out
to `http://localhost:59833/runtime/webhooks/EventGrid?functionName=<FunctionName>`.

The `AzureWebJobs.<FunctionName>.Disabled` entries in `local.settings.json` allow individual
functions to be selectively enabled during dev/test. All are disabled by default; developers
opt-in per session.

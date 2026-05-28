# Team Decisions

Compiled record of team decisions, architecture choices, and resolutions.

> Entries before 2026-05-21 archived to decisions-archive.md (last archived: 2026-05-28)

# Decision: Phase 3 Part 1 API Endpoints (RandomPostSettings & EventPublisherMapping)

**Date:** 2026-05-26T11:17:08.070-07:00  
**Author:** Trinity  
**Issue:** #995  
**Status:** ✅ PROPOSED

---

## Summary

Phase 3 Part 1 API work adds two per-user CRUD controllers under the existing `Publishers/...` route family:

- `Publishers/RandomPostSettings`
- `Publishers/EventPublisherMappings`

Both controllers:

- use class-level `[Authorize]` and `[IgnoreAntiforgeryToken]`
- stamp `CreatedByEntraOid` from the authenticated user's claims on create
- enforce owner-based access checks on get-by-id, update, and delete
- use separate create/update DTOs so PUT can preserve omitted optional fields
- recalculate onboarding after successful create, update, and delete operations

## Why

This keeps the new Phase 1 issue #995 models aligned with the established publisher and collector API patterns already used in the API project. It also avoids accidental field resets during updates and keeps IDOR protections explicit at each item endpoint.

## Follow-up

If the Web app consumes these endpoints next, it should use the same event type values enforced by the manager layer: `NewSyndicationFeedItem`, `NewYouTubeItem`, `NewSpeakingEngagement`, `RandomPost`, and `ScheduledItem`.

---

# Decision: Collector Dispatch Routing — Phase 2 of Issue #995

**Date:** 2026-05-26  
**Author:** Trinity  
**Branch:** `issue-995-per-user-publisher-routing`  
**Commit:** `41db74f6`  
**Status:** ✅ COMPLETE

---

## Context

The collector functions (`LoadNewPosts`, `LoadNewVideos`, `LoadNewSpeakingEngagements`) previously fired Azure Event Grid events after saving items. Sixteen `ProcessNew*` subscriber functions (four per platform) then routed those events to storage queues. This architecture was incompatible with per-user publisher selection — Event Grid subscriptions are per-topic, not per-user.

## Decision

Replace Event Grid dispatch with a new `ICollectorEventPublisher` / `CollectorEventPublisher` service that:

1. Queries `UserEventPublisherMapping` to find which platforms a given user has enabled for a given event type.
2. Renders a per-platform message via `IMessageTemplateManager` + `IPostComposer`.
3. Sends directly to the platform's storage queue.

The 16 dead `ProcessNew*` functions were deleted.

## Alternatives Considered

- **Keep Event Grid, add per-user metadata to events** — Event Grid subscription filters can't route by per-user data embedded in the event payload. Would require one subscription per user per platform, which is unmanageable.
- **Fan-out in a single new function subscribed to all events** — Moved the problem rather than solving it; still a single function with no user context.

## Consequences

- `IEventPublisher` is no longer injected into the three collector functions. The existing interface may still be used elsewhere (e.g. `RandomPosts`) and should not be deleted.
- Per-item dispatch is sequential (foreach, not `Task.WhenAll`) per team policy on shared scoped `BroadcastingContext` operations.
- Each queue send creates a fresh `SocialMediaPublishRequest` (with composed `Text`) rather than mutating the shared base request, preventing cross-platform contamination.
- All three collector test files needed updating to swap `IEventPublisher` for `ICollectorEventPublisher`.

---

# Decision: UTC Storage + User Local Display (Cross-Cutting DateTime Standard)

**Date:** 2026-05-26T09:22:15-07:00  
**Author:** Joseph Guadagno  
**Status:** ✅ DECIDED & POSTED

---

## Summary

All datetime and schedule/cron fields across the entire application follow a unified pattern:
- **Storage:** Always UTC (`datetimeoffset` in SQL, `DateTimeOffset` in C#)
- **Cron/schedule evaluation:** Evaluated in UTC — no per-schedule `TimeZoneId` needed
- **Display:** Convert from UTC to user's local time before presenting in UI
- **Edit:** Present fields in user's local time; convert back to UTC before saving

This is a **cross-cutting standard** that applies to ALL datetime fields, not just schedule cron expressions.

---

## Why

Ensures consistent, predictable datetime handling with no ambiguity about storage vs. display timezone. Answers Trinity's open question on GitHub issue #995.

## Posted to GitHub

Trinity confirmed this decision via GitHub comment on issue #995.

---

## Impact

- `UserPublisherSchedules`, `UserRandomPostSettings`, and all other time-aware tables use `datetimeoffset` columns (no `TimeZoneId` needed)
- Cron expression evaluation always uses UTC
- UI layer is responsible for timezone conversion on display and edit
- No per-schedule timezone configuration required

---

# Decision: Issue #995 Architecture Confirmed

**Date:** 2026-05-26T08:59:04.287-07:00  
**Author:** Neo (Lead)  
**Status:** ✅ CONFIRMED

---

## Confirmed Decisions

1. **Storage model**
   - Joseph confirmed **Option B** from the earlier analysis.
   - In practice, that means using **dedicated normalized user-owned tables** for routing and scheduling instead of adding event-type flags onto the existing `UserPublisher*Settings` tables.
   - Existing per-platform publisher settings tables remain responsible for publisher-specific configuration only.

2. **Scheduling model**
   - Scheduling is **CRON-like**.
   - A user can define **multiple schedules per event type**.
   - Each schedule combines **event type + cron expression/frequency + target publisher(s)**.

3. **Collector event routing**
   - **Event Grid is removed for collector events too**, not just Random Post.
   - New speaking engagements, blog posts, videos, and other collector-driven events will use the same **user-selectable publisher routing** model.

4. **Random Post execution model**
   - `Publishers\RandomPosts.cs` should run on **one global timer every minute**.
   - It should poll **all users** and determine which schedules are due, following the same broad execution pattern as `Publishers\ScheduledItems.cs`.
   - Do **not** create per-user timer functions or per-user function instances.

5. **Migration and seeding**
   - Seed the new `UserRandomPostSettings` table with **Joseph's current global defaults**.
   - After the seed path exists, the old **global Random Post settings** can be removed.

---

## Resulting Implementation Scope

- Add new per-user scheduling and routing tables.
- Add `UserRandomPostSettings` for per-user content filtering (`CutoffDate`, `ExcludedCategories`).
- Replace Event Grid dispatch with direct per-user publisher routing for both Random Post and collector events.
- Add API and Web support so users can manage schedules, publisher targets, and Random Post settings.
- Remove the old global Random Post settings path after seed migration is in place.

---

# Decision: Issue #995 Schema Review (Recommendation)

**Date:** 2026-05-26  
**Author:** Trinity (Backend Dev)  
**Status:** RECOMMENDATION → ADDRESSED by UTC Storage decision

---

## Summary

Trinity recommended that the new per-user scheduling/routing tables use `CreatedByEntraOid nvarchar(36)`, `datetimeoffset` timestamps, platform FKs, and normalized junction tables; also flagged that `UserPublisherSchedules` likely needs `TimeZoneId` unless Joseph declares all cron schedules UTC-only.

## Resolution

Joseph confirmed all cron schedules are UTC-only (see "UTC Storage + User Local Display" decision above). Table design uses `datetimeoffset` with no `TimeZoneId` column. Trinity confirmed via GitHub issue #995.

---
## Action Required

**Owner of this branch:** Restore the `null ||` guards in both collector files before committing the branch's changes.


# Decision: Track engagement aggregates during save

**Date:** 2026-05-25T10:47:45.368-07:00  
**Author:** Trinity (Backend Dev)  
**Status:** ✅ COMPLETE


---
## Summary

Fixed the speaking engagement save path so EF Core updates a tracked
`Engagement` aggregate instead of remapping the request into a fresh SQL entity
and forcing only the root state.


---
## Root Cause

`SpeakingEngagementsReader` builds `Domain.Models.Engagement` objects with
populated `Talks` collections. `EngagementDataStore.SaveAsync` then mapped that
Domain object to a new `Data.Sql.Models.Engagement` and set only the root entry
state to `Added` or `Modified`.

That left child `Talk` entities detached, so
`BroadcastingContext.SaveChangesAsync()` failed with:

- `System.ArgumentOutOfRangeException: Specified argument was out of the range`
  `of valid values.`
- `Parameter 'Unexpected entry.EntityState: Detached'`


---
## Change

**Files:**

- `src/JosephGuadagno.Broadcasting.Data.Sql/EngagementDataStore.cs`
- `src/JosephGuadagno.Broadcasting.Data.Sql.Tests/EngagementDataStoreTests.cs`

### Production fix

- For new engagements: create a tracked `Models.Engagement`, copy scalar
  values, add it to the DbContext, then attach imported talks through the
  tracked aggregate.
- For existing engagements: load the tracked engagement with `Talks`, copy
  scalar values onto that tracked entity, then upsert talks onto the tracked
  collection.
- Talk matching now prefers `Talk.Id`; when imported talks have no IDs, it
  falls back to a stable field match (`Name`, URLs, start/end times) so repeat
  imports update the existing talk instead of duplicating it.
- `CreatedOn`/`LastUpdatedOn` now preserve supplied values when present and fall
  back safely when callers do not send them.

### Validation

- `dotnet build .\src\ --no-restore --configuration Release` ✅
- `dotnet test .\src\JosephGuadagno.Broadcasting.Data.Sql.Tests\`
  `JosephGuadagno.Broadcasting.Data.Sql.Tests.csproj --no-build --verbosity`
  `normal --configuration Release --filter`
  `"FullyQualifiedName~EngagementDataStoreTests"` ✅ (44/44)
- Repo-wide filtered test run still has one unrelated pre-existing Web test
  failure:
  - `JosephGuadagno.Broadcasting.Web.Tests.Controllers.LinkedInControllerTests.Index_WhenOidClaimMissing_ShouldReturnViewWithHasTokenFalse`


---
## Impact

Speaking engagement saves can now persist engagements that include imported
Talks without tripping EF Core's detached-entity path, and repeated imports
update matching talks instead of blindly inserting duplicates.


# Decision: Refactor EngagementDataStore to Use AutoMapper

**Date:** 2026-05-25  
**Author:** Neo (Lead)  
**Status:** RECOMMENDATION — Pending Joe approval

---


---
## Context

Trinity fixed a real EF Core bug ("Unexpected entry.EntityState: Detached") by replacing `mapper.Map(source, destination)` with manual property helpers `ApplyEngagementValues` and `ApplyTalkValues` in `EngagementDataStore.SaveAsync`. The root cause was `BroadcastingProfile.cs` using `.ReverseMap()` on the `Engagement` mapping without ignoring the `Talks` navigation property, causing AutoMapper to replace the EF-tracked `ICollection<Talk>` with new untracked objects.

Joe has flagged this as inconsistent with the project-wide AutoMapper directive. This document assesses the blast radius and makes a recommendation.

---


---
## Manual Mapping Completeness Audit

### `ApplyEngagementValues` vs `Domain.Models.Engagement`

| Domain property | Mapped? | Notes |
|---|---|---|
| `Name` | ✅ | Direct copy |
| `Url` | ✅ | Direct copy |
| `StartDateTime` | ✅ | Direct copy |
| `EndDateTime` | ✅ | Direct copy |
| `TimeZoneId` | ✅ | Direct copy |
| `Comments` | ✅ | Direct copy |
| `CreatedByEntraOid` | ✅ | Direct copy |
| `CreatedOn` | ✅ | Custom logic: preserve source value if non-default; else UtcNow for new entities |
| `LastUpdatedOn` | ✅ | Custom logic: preserve source value if non-default; else UtcNow always |
| `Id` | ⬛ | Intentionally omitted — EF PK, never overwritten |
| `Talks` | ⬛ | Intentionally omitted — handled by `SyncTalks` |
| `SocialMediaPlatforms` | ⬛ | Intentionally omitted — managed by `EngagementSocialMediaPlatformDataStore` |

**Verdict: `ApplyEngagementValues` is complete. No missing fields.**

### `ApplyTalkValues` vs `Domain.Models.Talk`

| Domain property | Mapped? | Notes |
|---|---|---|
| `Name` | ✅ | Direct copy |
| `UrlForConferenceTalk` | ✅ | Direct copy |
| `UrlForTalk` | ✅ | Direct copy |
| `StartDateTime` | ✅ | Direct copy |
| `EndDateTime` | ✅ | Direct copy |
| `TalkLocation` | ✅ | Direct copy |
| `Comments` | ✅ | Direct copy |
| `CreatedByEntraOid` | ✅ | With fallback to `ownerEntraOid` from parent engagement |
| `EngagementId` | ✅ | Conditional: only applied when `source.EngagementId > 0` |
| `Id` | ⬛ | Intentionally omitted — EF PK |

**Verdict: `ApplyTalkValues` is complete. No missing fields.**

---


---
## Blast Radius Analysis

### Current `BroadcastingProfile` Engagement mapping

```csharp
// Creates BOTH directions; reverse auto-generates domain→data with Talks included
CreateMap<Models.Engagement, Domain.Models.Engagement>().ReverseMap();
```

The `ReverseMap()` direction (Domain→Data) is used in exactly one additional call site:

- **`AddTalkToEngagementAsync` (line 198):** `mapper.Map<Models.Engagement>(engagement)` — creates a **new** entity from scratch when `engagement.Id == 0`. No tracked instance, so no detachment issue. The `Talks` collection is populated manually via the `talk` parameter argument, not from `engagement.Talks`, so ignoring Talks in the mapping is safe here.

No other call sites create a domain→data `Engagement` mapping. `SyncTalks` already uses `mapper.Map<Models.Talk>(talk)` correctly for new Talk objects (the explicit `Domain.Models.Talk → Models.Talk` map with `Engagement` nav prop ignored is already in the profile).

---


---
## Recommendation: REFACTOR to AutoMapper (Low Risk)

### Why

1. **The fix is surgical.** The only change to `BroadcastingProfile` is splitting one `ReverseMap()` line into explicit bidirectional maps, adding `Ignore()` for `Talks` and `SocialMediaPlatforms` on the domain→data direction. This is the same pattern already used for `Talk` (which ignores the `Engagement` nav prop) and `MessageTemplate`/`SyndicationFeedItem`.

2. **Blast radius is minimal.** Only one extra call site uses the generated reverse map (`AddTalkToEngagementAsync`), and ignoring `Talks` there is safe.

3. **The manual mapping is complete today** — but manual property lists drift. If someone adds a field to `Domain.Models.Engagement` and forgets to update `ApplyEngagementValues`, the bug is silent. AutoMapper would catch it at startup via profile validation.

4. **The timestamp logic must stay explicit.** `CreatedOn` and `LastUpdatedOn` have conditional defaults that cannot be cleanly expressed in an AutoMapper `ForMember` without introducing `DateTimeOffset.UtcNow` into the profile (non-deterministic in tests). Keep these two lines as explicit assignments after `mapper.Map(engagement, dbEngagement)`.

### What to change

#### 1. `BroadcastingProfile.cs`

```csharp
// BEFORE
CreateMap<Models.Engagement, Domain.Models.Engagement>().ReverseMap();

// AFTER
CreateMap<Models.Engagement, Domain.Models.Engagement>();
CreateMap<Domain.Models.Engagement, Models.Engagement>()
    .ForMember(d => d.Talks, opt => opt.Ignore())
    .ForMember(d => d.SocialMediaPlatforms, opt => opt.Ignore());
```

#### 2. `EngagementDataStore.SaveAsync`

Replace both `ApplyEngagementValues(engagement, dbEngagement, isNew: X)` calls with:

```csharp
mapper.Map(engagement, dbEngagement);

// Preserve CreatedOn logic (cannot go in profile — depends on isNew flag)
dbEngagement.CreatedOn = engagement.CreatedOn != default
    ? engagement.CreatedOn
    : isNew && dbEngagement.CreatedOn == default
        ? DateTimeOffset.UtcNow
        : dbEngagement.CreatedOn;

// Always stamp LastUpdatedOn
dbEngagement.LastUpdatedOn = engagement.LastUpdatedOn != default
    ? engagement.LastUpdatedOn
    : DateTimeOffset.UtcNow;
```

Then delete `ApplyEngagementValues`. Keep `SyncTalks` and `ApplyTalkValues` unchanged — they handle collection sync logic that belongs in the data store, not in a mapping profile.

### What NOT to change

- `SyncTalks` — collection diff logic; not a mapping concern.
- `ApplyTalkValues` — non-trivial `ownerEntraOid` fallback; already contained and tested.
- `Domain.Models.Talk → Models.Talk` map — already correct with `Engagement` ignored.

---


---
## Risk Assessment

| Risk | Likelihood | Mitigation |
|---|---|---|
| Detachment error re-introduced | Low | `Talks` and `SocialMediaPlatforms` explicitly ignored in domain→data map |
| `AddTalkToEngagementAsync` regression | Low | Talks not mapped from engagement in that path (added via `talk` param) |
| Timestamp logic broken | Low | Kept as explicit code, not in profile |
| New Engagement field silently not saved | Lower than status quo | AutoMapper profile validation catches unmapped members at startup |

Existing `EngagementDataStoreTests` (44/44) provide regression coverage. Run them after the change.

---


---
## Summary

**Do the refactor.** It is a 3-line profile change + timestamp extraction. The manual mapping is currently correct but is an ongoing maintenance liability. The AutoMapper approach with explicit `Ignore()` directives is the correct, established pattern in this codebase (see `Talk`, `MessageTemplate`, `SyndicationFeedItem`). The timestamp business logic should remain explicit rather than embedded in a profile.


---

# Decision: EngagementDataStore AutoMapper Refactor — Landed

**Date:** 2026-05-25  
**Author:** Trinity (Backend Dev)  
**Status:** COMPLETE

---


---
## What Changed

### `BroadcastingProfile.cs`

Replaced the unsafe `ReverseMap()` on the `Engagement` mapping with two explicit
bidirectional declarations:

```csharp
// BEFORE
CreateMap<Models.Engagement, Domain.Models.Engagement>().ReverseMap();

// AFTER
CreateMap<Models.Engagement, Domain.Models.Engagement>();
CreateMap<Domain.Models.Engagement, Models.Engagement>()
    .ForMember(dest => dest.Talks, opt => opt.Ignore())
    .ForMember(dest => dest.SocialMediaPlatforms, opt => opt.Ignore());
```

This prevents AutoMapper from replacing EF-tracked navigation collections when
mapping domain→data, which was the root cause of the original "Detached" error.

### `EngagementDataStore.cs`

In `SaveAsync`, both the insert path (`engagement.Id == 0`) and the update path
now use `mapper.Map(engagement, dbEngagement)` with explicit timestamp handling
instead of the manual `ApplyEngagementValues` helper:

```csharp
mapper.Map(engagement, dbEngagement);
if (dbEngagement.CreatedOn == default)
    dbEngagement.CreatedOn = DateTimeOffset.UtcNow;
dbEngagement.LastUpdatedOn = DateTimeOffset.UtcNow;
```

The `ApplyEngagementValues` private method was removed entirely.

`SyncTalks` and `ApplyTalkValues` were not modified — they manage tracked
collection merging and non-trivial fallback logic that belongs in the data store,
not in a mapping profile.

---


---
## Verification

- Build: **0 errors, 1 pre-existing warning (CS8600, not introduced by this change)**
- Tests: **141 passed, 0 failed** (4 Twitter integration tests skipped as expected)
- Filter applied: `FullyQualifiedName!~SyndicationFeedReader`

---


---
## References

- Neo's blast radius analysis: `.squad/decisions/inbox/neo-engagement-automapper-blast-radius.md`
- Requested by: Joe




# Decision: ScheduledItems direct per-user routing

**Date:** 2026-05-26T10:48:34.944-07:00  
**Author:** Trinity  
**Status:** ✅ IMPLEMENTED

---

## Decision

Migrate `Publishers\ScheduledItems.cs` off Event Grid and onto a dedicated `IScheduledItemEventPublisher` service that routes each due item directly to the owner's active `UserEventPublisherMapping` targets for `ScheduledItem`.

## Why

The collector rewrite already established the direct-routing pattern, and the four `ProcessScheduledItemFired` functions were only Event Grid bridge code. Keeping scheduled item routing in a dedicated service preserves the same per-user queue dispatch contract while keeping the timer trigger focused on orchestration and sent-flag updates.

## Notes

- `ScheduledItem` was already present in both `scripts\database\table-create.sql` and `scripts\database\migrations\2026-05-26-per-user-publisher-routing-tables.sql`, so no schema change was required.
- `IEventPublisher` is no longer registered in the Functions host or Functions test startup.
- The scheduled-item Event Grid simulator topic was removed because its subscribers were deleted with this migration.

---

# Decision: Phase 3 Part 2 Web UI for per-user publisher settings

**Date:** 2026-05-26T11:17:08.070-07:00  
**Author:** Trinity  
**Status:** ✅ IMPLEMENTED

## Decision

- Add Web MVC controllers under the `Publishers/...` route family for
  `UserRandomPostSettings` and `UserEventPublisherMapping`.
- Keep the Web layer on HTTP-client wrapper services
  (`IUserRandomPostSettingsService`, `IUserEventPublisherMappingService`) rather
  than injecting managers directly.
- Resolve platform names through `ISocialMediaPlatformService` and centralize
  event-type labels/icons in `Web/Constants/PublisherEventTypes.cs`.
- For editable `DateTimeOffset` values, use a visible `datetime-local` input and
  a hidden UTC field populated in the browser so the UI stays local-time
  friendly while the API contract remains UTC.

## Why

- This matches the existing Web architecture where controllers consume
  downstream API wrappers instead of domain managers.
- The shared metadata helper avoids duplicating event-type labels and collector
  icon choices across controllers and views.
- The UTC hidden-field pattern satisfies the cross-cutting date decision without
  adding server-side timezone assumptions.

## Files

- `src/JosephGuadagno.Broadcasting.Web/Controllers/UserRandomPostSettingsController.cs`
- `src/JosephGuadagno.Broadcasting.Web/Controllers/UserEventPublisherMappingController.cs`
- `src/JosephGuadagno.Broadcasting.Web/Services/UserRandomPostSettingsService.cs`
- `src/JosephGuadagno.Broadcasting.Web/Services/UserEventPublisherMappingService.cs`
- `src/JosephGuadagno.Broadcasting.Web/Constants/PublisherEventTypes.cs`

# Decision: RandomPosts log sanitization fix

**Author:** Tank  
**Date:** 2026-05-26T16:27:09.011-07:00  
**Issue:** #998  
**Status:** ✅ COMPLETE (Approved by Neo)

---

## Context

Neo blocked PR #998 because `src\JosephGuadagno.Broadcasting.Functions\Publishers\RandomPosts.cs` passed the externally controlled RSS title (`syndicationFeedItem.Title`) directly into a logger call, violating the `cs/log-forging` hard gate.

## Changes

- Confirmed `using JosephGuadagno.Broadcasting.Domain.Utilities;` was already present.
- Wrapped `syndicationFeedItem.Title` with `LogSanitizer.Sanitize(syndicationFeedItem.Title)` in the `logger.LogInformation(...)` dispatch message.
- Also sanitized the `title` value sent through `logger.LogCustomEvent(...)` so telemetry uses the same safe value.

## Verification

- `dotnet build .\src\ --no-restore --configuration Release` ✅
- `dotnet test .\src\ --no-build --configuration Release --verbosity normal --filter "FullyQualifiedName!~SyndicationFeedReader"` ✅ (1,279 tests)

## Neo Re-Review

**Date:** 2026-05-26  
**Verdict:** APPROVED ✅

All RSS-sourced content in logger calls is correctly wrapped in `LogSanitizer.Sanitize()`:
- `using JosephGuadagno.Broadcasting.Domain.Utilities;` is present ✅
- `syndicationFeedItem.Title` sanitized in `LogCustomEvent` dictionary ✅
- `syndicationFeedItem.Title` sanitized in `LogInformation` call ✅
- All other user-controlled values (ownerOid, cron expressions) are also sanitized throughout ✅

No new log injection issues introduced. The blocking violation is fully resolved.

---

> Entries before 2026-05-18 archived to decisions-archive.md



### 2026-05-27: EF Core table names aligned to singular SQL tables
**By:** Morpheus
**What:** Kept the SQL scripts as the source of truth and updated EF Core to map `UserEventPublisherMapping` to `UserEventPublisherMapping` and `UserApprovalLog` to `UserApprovalLog` with explicit `ToTable(...)` calls in `BroadcastingContext`. Also updated `BroadcastingContextTests` to assert those singular SQL table names.
**Why:** The schema in `scripts\database\table-create.sql` uses singular table names for these two entities, but EF Core was resolving the plural DbSet names by convention. That drift would make EF query `UserEventPublisherMappings` and `UserApprovalLogs` instead of the actual tables.


# Morpheus Phase 2 Complete

## Files renamed

- `src\JosephGuadagno.Broadcasting.Data.Sql\Models\UserEventPublisherMapping.cs`
  -> `src\JosephGuadagno.Broadcasting.Data.Sql\Models\UserEventDispatcherMapping.cs`
- `src\JosephGuadagno.Broadcasting.Data.Sql\UserEventPublisherMappingDataStore.cs`
  -> `src\JosephGuadagno.Broadcasting.Data.Sql\UserEventDispatcherMappingDataStore.cs`

## Files modified

- `src\JosephGuadagno.Broadcasting.Data.Sql\BroadcastingContext.cs`
- `src\JosephGuadagno.Broadcasting.Data.Sql\MappingProfiles\UserPublisherSettingsMappingProfile.cs`
- `src\JosephGuadagno.Broadcasting.Data.Sql\ServiceCollectionExtensions.cs`
- `scripts\database\table-create.sql`
- `scripts\database\data-seed.sql`

## New migration script created

- `scripts\database\migrations\2026-06-10-rename-usereventpublishermappings-to-dispatcher.sql`

## Build result

- `dotnet build .\src\JosephGuadagno.Broadcasting.Data.Sql\ --configuration Release`
  succeeded.
- Warning:
  `src\JosephGuadagno.Broadcasting.Data.Sql\EngagementDataStore.cs(37,32)`
  `CS8600` Converting null literal or possible null value to non-nullable
  type.

## Notes

- Updated `BroadcastingContext` to use `UserEventDispatcherMappings`
  and renamed the associated key, index, foreign key, and
  check-constraint mappings.
- Updated the SQL create script to use `UserEventDispatcherMappings`
  plus renamed default constraints for fresh installs.
- The migration script renames the existing table, default constraints,
  named constraints, index, and the `FeedChecks` row from
  `PublishersScheduledItems` to `DispatchersScheduledItems`.


# Dispatcher Docs Outline

**Author:** Neo  
**Date:** 2026-06-10  
**Companion:** `neo-dispatcher-rename-spec.md`

---

## 1. Glossary Entries (Three-Tier Vocabulary)

These three entries should live in `docs\glossary.md` (create if absent) and be linked from the README architecture section.

### Collector

> A Collector is an Azure Function (or background service) that pulls new content from an external source — syndication feeds, YouTube channels, speaking-engagement registrations — and persists it to the JJGNet database. Collectors do not decide what gets posted or when; they only gather. Examples: `CollectorsFeedLoadNewPosts`, `CollectorsYouTubeLoadNewVideos`.

### Dispatcher

> A Dispatcher is an Azure Function (or service) that reads persisted content and decides **when** and **where** to send it. Dispatchers consult per-user routing rules (`UserEventDispatcherMapping`), compose messages using `MessageTemplate` records, and enqueue `SocialMediaPublishRequest` messages to platform-specific Azure Storage queues. Dispatchers do not call social-media APIs directly; they delegate to platform queue processors. Examples: `DispatchersRandomPosts`, `DispatchersScheduledItems`, `CollectorEventDispatcher`, `ScheduledItemEventDispatcher`.

### Platform

> A Platform is a destination social network (Twitter/X, LinkedIn, Bluesky, Facebook). The `SocialMediaPlatform` table is the canonical registry of active platforms. Each platform has an Azure Function that dequeues `SocialMediaPublishRequest` messages and calls the platform's API. Per-user platform credentials are stored in `UserPlatform*Settings` tables (one per network). Platform functions and credential tables carry the platform name, not "Dispatcher" or "Collector" prefixes.

---

## 2. Architecture Overview Section

Add or replace the "How It Works" section in `README.md` with the following structure:

```
## Architecture

JJGNet Broadcasting uses a three-tier pipeline:

  Collectors → Dispatchers → Platforms

### Collectors
[...description + link to glossary...]

### Dispatchers
[...description + link to glossary...]

### Platforms
[...description + link to glossary...]

### Flow Example
1. `CollectorsFeedLoadNewPosts` detects a new blog post and saves a `SyndicationFeedItem`.
2. `CollectorEventDispatcher.DispatchSyndicationFeedItemAsync` queries `UserEventDispatcherMapping`
   to find which platforms the post owner has enabled for `NewSyndicationFeedItem` events.
3. For each active mapping, a composed `SocialMediaPublishRequest` is enqueued to the
   platform's Azure Storage queue.
4. The platform's queue-trigger Function (e.g., `TwitterSendTweet`) dequeues the message
   and calls the Twitter API.
```

---

## 3. Existing Docs That Reference "Publisher" and Need Updating

### High priority (contain architecture descriptions or user-facing instructions)

| File | What to change |
|---|---|
| `README.md` | Replace "Publishers" tier description with "Dispatchers"; update any function name references; add three-tier vocabulary diagram link |
| `docs\publishing-testing-guide.md` | This entire doc is named around "publishing" — consider renaming to `dispatching-testing-guide.md` or `end-to-end-dispatch-guide.md`; replace all "publisher" terminology with "dispatcher" throughout |
| `docs\per-user-credential-handling.md` | Update references to "publisher settings" → "dispatcher settings" for the routing layer; keep "platform settings" / "platform credentials" for `UserPlatform*Settings` |
| `CONTRIBUTING.md` | Update any naming guidance that mentions Publishers |
| `.github\copilot-instructions.md` | Update `Controllers\Publishers\` path reference in the architecture section; update `ConfigurationFunctionNames.Publishers*` examples; update "Publishers" route prefix mention |

### Medium priority (squad internals / skills)

| File | What to change |
|---|---|
| `.squad\skills\social-media-publisher-contract\SKILL.md` | Rename skill folder to `social-media-dispatcher-contract`; rewrite to reflect `ISocialMediaDispatcher.DispatchAsync` |
| `.squad\skills\per-user-settings-api-crud\SKILL.md` | Update any route examples that use `/Publishers/` prefix |
| `.squad\skills\per-user-settings-web-ui\SKILL.md` | Same |
| `.squad\skills\scheduled-item-direct-routing\SKILL.md` | Update `IScheduledItemEventPublisher` → `IScheduledItemEventDispatcher` references |
| `.squad\decisions.md` | Add entry recording the vocabulary decision; existing entries that reference "publisher" routing can be annotated |

### Low priority (historical / archived)

Agent history files and archive decision files reference the old names but do not need updating — they are read-only historical record. Add a note at the top of `decisions.md` clarifying that entries predating this decision use legacy "Publisher" terminology.

---

## 4. New Docs to Create

| File | Purpose |
|---|---|
| `docs\glossary.md` | Canonical definitions for Collector, Dispatcher, Platform plus cross-references to code |
| `docs\three-tier-architecture.md` | Diagram (Mermaid flowchart) of the Collector → Dispatcher → Platform pipeline with key classes at each tier |
| `docs\adding-a-new-dispatcher.md` | Step-by-step guide: create a new timer-triggered Dispatcher function following the established patterns |


# Decision: Dispatchers Replaces Publishers

**Author:** Neo  
**Date:** 2026-06-10  
**Status:** ACCEPTED  
**Supersedes:** Implicit "Publisher" naming convention established incrementally from Issue #731 through Issue #995

---

## Decision

The word **"Publisher"** is retired from contexts that refer to the scheduling and dispatch layer of JJGNet Broadcasting. It is replaced by **"Dispatcher"** throughout class names, interfaces, method names, routes, database tables, configuration keys, and documentation.

The word "Publisher" is **not** retired from per-platform credential tables and the models that back them (`UserPublisher*Settings`). That rename to `UserPlatform*Settings` is deferred to Phase 3 and will be covered by a separate decision when scoped.

The canonical vocabulary going forward is:

| Tier | Meaning | Examples |
|---|---|---|
| **Collectors** | Gather content from external sources and persist it | `CollectorsFeedLoadNewPosts`, `ICollectorEventDispatcher` |
| **Dispatchers** | Decide when and where to send content; enqueue to platform queues | `DispatchersRandomPosts`, `DispatchersScheduledItems`, `UserEventDispatcherMapping` |
| **Platforms** | Destination social networks; dequeue and call social APIs | `TwitterSendTweet`, `BlueskyPostMessage`, `SocialMediaPlatform` |

---

## Rationale

The word "Publisher" was overloaded in three incompatible ways:

1. **Scheduling/dispatch logic** — Azure Functions that fire on a timer, pick up content, and route it to storage queues (`Functions\Publishers\RandomPosts`, `Functions\Publishers\ScheduledItems`).
2. **Destination platform identity** — `SocialMediaPlatforms` table, `SocialMediaPlatformIds` constants.
3. **Per-platform user credentials** — `UserPublisherBlueskySettings`, `UserPublisherTwitterSettings`, etc.

This overloading caused confusion when onboarding new contributors and made it unclear which abstraction was being referenced in a given file. The dispatch functions neither represent the destination platform nor store credentials — they are scheduling agents. "Dispatcher" precisely describes that role: an agent that decides timing and destination, then hands off to a queue.

"Collectors" was already the established vocabulary for the gather tier (adopted in the `CollectorsFeed*`, `CollectorsYouTube*` naming). "Dispatchers" aligns with that pattern and makes the three-tier pipeline self-describing.

---

## Canonical Three-Tier Reference

```
  ┌───────────────────────────────────────────────────────────┐
  │  COLLECTORS                                               │
  │  Gather content → persist to DB                          │
  │  CollectorsFeedLoadNewPosts, CollectorsYouTubeLoadNewVideos│
  └──────────────────────────┬────────────────────────────────┘
                             │ content events
  ┌──────────────────────────▼────────────────────────────────┐
  │  DISPATCHERS                                              │
  │  Route content → enqueue SocialMediaPublishRequest        │
  │  DispatchersRandomPosts, DispatchersScheduledItems        │
  │  CollectorEventDispatcher, ScheduledItemEventDispatcher   │
  │  UserEventDispatcherMapping (per-user routing rules)      │
  └──────────────────────────┬────────────────────────────────┘
                             │ queue messages
  ┌──────────────────────────▼────────────────────────────────┐
  │  PLATFORMS                                                │
  │  Dequeue and call social API                             │
  │  TwitterSendTweet, BlueskyPostMessage, LinkedInPostLink   │
  │  UserPlatform*Settings (per-platform credentials)        │
  └───────────────────────────────────────────────────────────┘
```

---

## Impact on Future Code

**Any new feature that routes, schedules, or decides when/where to send content must use "Dispatcher" naming.**

Specific rules:

1. New timer-triggered Azure Functions that pick up content from DB and send it to queues: name the class and its `[Function(…)]` attribute under the `Dispatchers` category. Place the class in `Functions\Dispatchers\`. Register the Azure Function name constant in `ConfigurationFunctionNames` under the `// Dispatchers` comment block.

2. New services that internally dispatch content from a collector event: implement `ICollectorEventDispatcher` (or a new `ICollectorEventDispatcher<T>` variant) and name the class `*EventDispatcher`.

3. New routing tables that map user preferences to delivery destinations: name the table and model `UserEvent*DispatcherMapping` or `User*DispatcherConfig`.

4. New API routes for managing dispatch configuration: place under the `Controllers\Dispatchers\` folder and expose under `/Dispatchers/…`.

5. The `ISocialMediaDispatcher.DispatchAsync` interface is the canonical contract for "send this composed request to a platform". Platform manager classes implement this interface. Do not introduce a second `ISocialMedia*` publishing interface.

6. **Do not** use "Publisher" for any of the above. If you encounter legacy "Publisher" naming that has not yet been migrated, leave it unchanged and file a tracking note — do not introduce new "Publisher" names alongside it.

---

## Migration Phases

| Phase | Scope | Status |
|---|---|---|
| Phase 1 | `Functions\Publishers\` folder + namespace | Pending |
| Phase 2 | Domain, Data.Sql, Managers, API, Web, DB (`UserEventPublisherMapping`), Functions services | Pending |
| Phase 3 | `UserPublisher*Settings` → `UserPlatform*Settings` (all layers + DB) | Deferred |

Full change inventory: see `neo-dispatcher-rename-spec.md`.  
Documentation plan: see `neo-dispatcher-docs-outline.md`.


# Dispatcher Rename Spec

**Author:** Neo  
**Date:** 2026-06-10  
**Status:** PROPOSED — awaiting implementation assignment

---

## 1. Approved Vocabulary

The system uses three canonical tiers: **Collectors** gather content from external sources and persist it; **Dispatchers** decide when and where to send that content (timer-driven Azure Functions plus the routing services that fan out to queues); **Platforms** are the destination social networks (Twitter, LinkedIn, Bluesky, Facebook) identified by `SocialMediaPlatform` records and their per-user credential tables. The word "Publisher" is retired from dispatch/scheduling contexts because it conflated the scheduling layer with the platform-credential layer. From this decision forward, any new class, interface, method, route, or table that routes or schedules content delivery must use "Dispatcher" naming.

---

## 2. In-Scope for This Rename

All uses of "Publisher" that refer to the **dispatch or scheduling** layer:

- The `Functions\Publishers\` folder and its two function classes
- `ConfigurationFunctionNames.Publishers*` constants (and the Azure Function registration names derived from them)
- `ISocialMediaPublisher` interface (the per-platform send contract used by Functions)
- `ICollectorEventPublisher` / `CollectorEventPublisher` (fan-out service internal to Functions)
- `IScheduledItemEventPublisher` / `ScheduledItemEventPublisher` (service internal to Functions)
- `UserEventPublisherMapping` concept — the routing table that decides which platforms receive each event type (domain model, data store, manager, API controller, Web controller/views, DB table)
- The API `Publishers` controller and its route prefix (`GET /Publishers`)
- The Web `Publishers` controller and its route prefix (`/Publishers`)
- All "Publishers aggregate" types: DTOs, services, view models
- `PublisherEventTypes` / `PublisherEventTypeOption` Web constants
- `PublisherPlatformCardViewModel` Web view model
- `local.settings.json` cron config key names prefixed `publishers_`
- `data-seed.sql` FeedChecks row keyed `'PublishersScheduledItems'`

---

## 3. Out of Scope

| Concept | Reason |
|---|---|
| `SocialMediaPlatform` / `SocialMediaPlatformIds` | Already clear — names a destination, not a layer |
| `UserPublisherBlueskySettings` / `UserPublisherTwitterSettings` / `UserPublisherLinkedInSettings` / `UserPublisherFacebookSettings` | Deferred to Phase 3 → will become `UserPlatform*Settings` |
| `IUserPublisher*SettingsManager` / `IUserPublisher*SettingsDataStore` | Phase 3 |
| DB tables `UserPublisherBlueskySettings`, `UserPublisherTwitterSettings`, `UserPublisherLinkedInSettings`, `UserPublisherFacebookSettings` | Phase 3 |
| Web controllers `PublisherBlueskySettingsController`, `PublisherTwitterSettingsController`, `PublisherLinkedInSettingsController`, `PublisherFacebookSettingsController` | Phase 3 |
| Web views `PublisherBlueskySettings/`, `PublisherTwitterSettings/`, `PublisherLinkedInSettings/`, `PublisherFacebookSettings/` | Phase 3 |
| `PublisherPlatformSettingsViewModels.cs` (per-platform edit VMs) | Phase 3 |
| `UserPublisherSettingsDtos.cs` (API DTOs for per-platform settings) | Phase 3 |
| `UserPublisherSetting` / `UserPublisherSettingUpdate` domain models | Phase 3 |
| `TwitterPublisherSetting`, `LinkedInPublisherSetting`, etc. domain models | Phase 3 |
| `BlueskyPublisherSetting` domain model | Phase 3 |
| `UserPublisher*DataStore.cs` files in Data.Sql | Phase 3 |
| `UserPublisherSettingsMappingProfile.cs` in Data.Sql | Phase 3 |
| `UserPublisher*SettingsManager.cs` in Managers | Phase 3 |
| `IUserPublisher*SettingsService.cs` in Web | Phase 3 |
| `UserPublisher*SettingsService.cs` in Web | Phase 3 |

---

## 4. Phase Breakdown

### Phase 1 — Functions Dispatcher Folder

**Scope:** Rename the folder and its two classes; no interface or route changes.

| Change | From | To |
|---|---|---|
| Folder | `src\JosephGuadagno.Broadcasting.Functions\Publishers\` | `...\Functions\Dispatchers\` |
| Namespace in `RandomPosts.cs` | `JosephGuadagno.Broadcasting.Functions.Publishers` | `JosephGuadagno.Broadcasting.Functions.Dispatchers` |
| Namespace in `ScheduledItems.cs` | `JosephGuadagno.Broadcasting.Functions.Publishers` | `JosephGuadagno.Broadcasting.Functions.Dispatchers` |

**Files touched:** 2 `.cs` files + 1 folder rename.

---

### Phase 2 — Domain / Data / Manager / API / Web / DB

#### 2a. Domain — Constants

**File:** `src\JosephGuadagno.Broadcasting.Domain\Constants\ConfigurationFunctionNames.cs`

| From | To | ⚠️ |
|---|---|---|
| `PublishersRandomPosts = "PublishersRandomPosts"` | `DispatchersRandomPosts = "DispatchersRandomPosts"` | Azure Function registration name; Azure portal app-settings `AzureWebJobs.PublishersRandomPosts.Disabled` must change in lockstep |
| `PublishersScheduledItems = "PublishersScheduledItems"` | `DispatchersScheduledItems = "DispatchersScheduledItems"` | Same; also stored in `FeedChecks.Name` column — requires a data migration |
| Comment `// Publishers` | `// Dispatchers` | |

#### 2b. Domain — Interfaces

| From | To | Notes |
|---|---|---|
| `ISocialMediaPublisher.cs` | `ISocialMediaDispatcher.cs` | Rename file + interface; rename `PublishAsync` → `DispatchAsync` |
| `IUserEventPublisherMappingManager.cs` | `IUserEventDispatcherMappingManager.cs` | Rename file + interface name |
| `IUserEventPublisherMappingDataStore.cs` | `IUserEventDispatcherMappingDataStore.cs` | Rename file + interface name |

#### 2c. Domain — Models

| From | To |
|---|---|
| `UserEventPublisherMapping.cs` | `UserEventDispatcherMapping.cs` (rename file + class) |

#### 2d. Functions — Services

| From | To |
|---|---|
| `ICollectorEventPublisher.cs` | `ICollectorEventDispatcher.cs` |
| `CollectorEventPublisher.cs` | `CollectorEventDispatcher.cs` |
| `IScheduledItemEventPublisher.cs` | `IScheduledItemEventDispatcher.cs` |
| `ScheduledItemEventPublisher.cs` | `ScheduledItemEventDispatcher.cs` |

Method renames within these files:

| Old | New |
|---|---|
| `ICollectorEventPublisher.PublishSyndicationFeedItemAsync` | `ICollectorEventDispatcher.DispatchSyndicationFeedItemAsync` |
| `ICollectorEventPublisher.PublishYouTubeItemAsync` | `ICollectorEventDispatcher.DispatchYouTubeItemAsync` |
| `ICollectorEventPublisher.PublishSpeakingEngagementAsync` | `ICollectorEventDispatcher.DispatchSpeakingEngagementAsync` |
| `IScheduledItemEventPublisher.PublishAsync` | `IScheduledItemEventDispatcher.DispatchAsync` |

All call sites in Functions (callers of `scheduledItemEventPublisher.PublishAsync`) must be updated.  
All registrations in `Functions\Program.cs` (4× `ISocialMediaPublisher` → `ISocialMediaDispatcher`).  
All registrations in `Functions.Tests\Startup.cs` (4× `ISocialMediaPublisher` → `ISocialMediaDispatcher`).

#### 2e. Functions — Config

**File:** `src\JosephGuadagno.Broadcasting.Functions\local.settings.json`

| From | To |
|---|---|
| `"publishers_random_post_cron_settings"` key | `"dispatchers_random_post_cron_settings"` |
| `"publishers_scheduled_items_cron_settings"` key | `"dispatchers_scheduled_items_cron_settings"` |
| `"AzureWebJobs.PublishersRandomPosts.Disabled"` | `"AzureWebJobs.DispatchersRandomPosts.Disabled"` |
| `"AzureWebJobs.PublishersScheduledItems.Disabled"` | `"AzureWebJobs.DispatchersScheduledItems.Disabled"` |

Timer trigger binding in `RandomPosts.cs`:  
`%publishers_random_post_cron_settings%` → `%dispatchers_random_post_cron_settings%`  
Timer trigger binding in `ScheduledItems.cs`:  
`%publishers_scheduled_items_cron_settings%` → `%dispatchers_scheduled_items_cron_settings%`

#### 2f. Data.Sql — Models

| From | To |
|---|---|
| `Models\UserEventPublisherMapping.cs` | `Models\UserEventDispatcherMapping.cs` |

#### 2g. Data.Sql — Data Stores

| From | To |
|---|---|
| `UserEventPublisherMappingDataStore.cs` | `UserEventDispatcherMappingDataStore.cs` |

#### 2h. Data.Sql — Mapping Profiles

Update `UserPublisherSettingsMappingProfile.cs`: any internal references to `UserEventPublisherMapping` → `UserEventDispatcherMapping`.  
_(Profile file itself stays `UserPublisherSettingsMappingProfile` until Phase 3 renames it.)_

#### 2i. Managers

| From | To |
|---|---|
| `UserEventPublisherMappingManager.cs` | `UserEventDispatcherMappingManager.cs` |

#### 2j. API — Folder + Controllers + DTOs

| From | To | ⚠️ |
|---|---|---|
| `Controllers\Publishers\` folder | `Controllers\Dispatchers\` | |
| `PublishersController.cs` | `DispatchersController.cs` | Route `[Route("[controller]")]` → now resolves to `/Dispatchers` — **breaking API change** |
| `PublishersAggregateResponse.cs` | `DispatchersAggregateResponse.cs` | |
| All 5 controllers inside `Controllers\Publishers\` | Move to `Controllers\Dispatchers\`; update namespace from `...Controllers.Publishers` → `...Controllers.Dispatchers` | Route prefix `Publishers/...` → `Dispatchers/...` on each — **breaking API changes** |

Controllers to move (namespace only; class names unchanged except `PublishersController`):
- `BlueskySettingsController.cs` — currently `[Route("Publishers/BlueskySettings")]` → `[Route("Dispatchers/BlueskySettings")]`
- `FacebookSettingsController.cs` — `[Route("Publishers/FacebookSettings")]` → `[Route("Dispatchers/FacebookSettings")]`
- `LinkedInSettingsController.cs` — `[Route("Publishers/LinkedInSettings")]` → `[Route("Dispatchers/LinkedInSettings")]`
- `TwitterSettingsController.cs` — `[Route("Publishers/TwitterSettings")]` → `[Route("Dispatchers/TwitterSettings")]`
- `UserRandomPostSettingsController.cs` — `[Route("Publishers/RandomPostSettings")]` → `[Route("Dispatchers/RandomPostSettings")]`
- `UserEventPublisherMappingController.cs` — rename class → `UserEventDispatcherMappingController.cs`; route `Publishers/EventPublisherMappings` → `Dispatchers/EventDispatcherMappings`

#### 2k. Web — Controllers

| From | To | Notes |
|---|---|---|
| `PublishersController.cs` | `DispatchersController.cs` | Route `[Route("Publishers")]` → `[Route("Dispatchers")]` — URL change |
| `UserEventPublisherMappingController.cs` | `UserEventDispatcherMappingController.cs` | Route changes accordingly |

#### 2l. Web — Services + Interfaces

| From | To |
|---|---|
| `IPublishersAggregateService.cs` | `IDispatchersAggregateService.cs` |
| `PublishersAggregateService.cs` | `DispatchersAggregateService.cs` |
| `IUserEventPublisherMappingService.cs` | `IUserEventDispatcherMappingService.cs` |
| `UserEventPublisherMappingService.cs` | `UserEventDispatcherMappingService.cs` |

#### 2m. Web — Models / ViewModels

| From | To |
|---|---|
| `PublishersAggregateViewModel.cs` | `DispatchersAggregateViewModel.cs` |
| `UserEventPublisherMappingViewModel.cs` | `UserEventDispatcherMappingViewModel.cs` |
| `PublisherPlatformCardViewModel` (class inside `PublisherPlatformSettingsViewModels.cs`) | `DispatcherPlatformCardViewModel` — rename class; defer file rename to Phase 3 |

#### 2n. Web — Constants

| From | To |
|---|---|
| `PublisherEventTypes.cs` | `DispatcherEventTypes.cs` |
| `PublisherEventTypes` class | `DispatcherEventTypes` |
| `PublisherEventTypeOption` record | `DispatcherEventTypeOption` |

#### 2o. Web — Views

| From | To |
|---|---|
| `Views\Publishers\` folder | `Views\Dispatchers\` |
| `Views\UserEventPublisherMapping\` folder | `Views\UserEventDispatcherMapping\` |
| `Views\Shared\_LoginPartial.cshtml` | Update `asp-controller="Publishers"` → `"Dispatchers"`, label "Publisher Settings" → "Dispatcher Settings", "All Publisher Settings" → "All Dispatcher Settings", `asp-controller="UserEventPublisherMapping"` → `"UserEventDispatcherMapping"`, "Event Publisher Mappings" → "Event Dispatcher Mappings" |

#### 2p. Tests

| Project | Change |
|---|---|
| `Functions.Tests\Services\CollectorEventPublisherTests.cs` | Rename class + update interface/type references |
| `Functions.Tests\Services\ScheduledItemEventPublisherTests.cs` | Rename class + update interface/type references |
| `Api.Tests\Controllers\UserEventPublisherMappingControllerTests.cs` | Rename + update references |
| `Web.Tests\Controllers\UserEventPublisherMappingControllerTests.cs` | Rename + update references |
| `Web.Tests\Services\UserEventPublisherMappingServiceTests.cs` | Rename + update references |
| `Managers.Tests\UserEventPublisherMappingManagerTests.cs` | Rename + update references |
| `Data.Sql.Tests\UserEventPublisherMappingDataStoreTests.cs` | Rename + update references |
| `Managers.Bluesky.Tests\BlueskyManagerUnitTests.cs` | Update `ISocialMediaPublisher` → `ISocialMediaDispatcher`, `PublishAsync` → `DispatchAsync` |
| `Managers.LinkedIn.Tests\LinkedInManagerUnitTests.cs` | Same |
| `Managers.Facebook.Tests\FacebookManagerUnitTests.cs` | Same |
| `Managers.Twitter.Tests\TwitterManagerTests.cs` | Same; also rename reflection test method |

---

## 5. Database Changes

### Phase 2 DB Change — `UserEventPublisherMapping`

A migration script `scripts\database\migrations\YYYY-MM-DD-rename-usereventpublishermapping-to-dispatcher.sql` is needed:

```sql
-- Rename table
EXEC sp_rename 'dbo.UserEventPublisherMapping', 'UserEventDispatcherMapping';

-- Rename primary key constraint
EXEC sp_rename 'dbo.UserEventDispatcherMapping.PK_UserEventPublisherMapping', 'PK_UserEventDispatcherMapping', 'OBJECT';

-- Rename unique constraint
EXEC sp_rename 'dbo.UserEventDispatcherMapping.UQ_UserEventPublisherMapping_Owner_Event_Platform', 'UQ_UserEventDispatcherMapping_Owner_Event_Platform', 'OBJECT';

-- Rename FK
EXEC sp_rename 'dbo.UserEventDispatcherMapping.FK_UserEventPublisherMapping_SocialMediaPlatforms', 'FK_UserEventDispatcherMapping_SocialMediaPlatforms', 'OBJECT';

-- Rename check constraint
EXEC sp_rename 'dbo.UserEventDispatcherMapping.CK_UserEventPublisherMapping_EventType', 'CK_UserEventDispatcherMapping_EventType', 'OBJECT';

-- Rename index
EXEC sp_rename 'dbo.UserEventDispatcherMapping.IX_UserEventPublisherMapping_Active', 'IX_UserEventDispatcherMapping_Active', 'INDEX';
```

Also update `table-create.sql` to use the new names.

### Phase 2 DB Change — FeedChecks seed data

`scripts\database\data-seed.sql` line 59:  
`'PublishersScheduledItems'` → `'DispatchersScheduledItems'`

This seed row must be accompanied by a one-time data migration for any production row:

```sql
UPDATE dbo.FeedChecks SET Name = 'DispatchersScheduledItems' WHERE Name = 'PublishersScheduledItems';
```

### Phase 3 DB Changes (deferred)

Migration scripts for:
- `UserPublisherBlueskySettings` → `UserPlatformBlueskySettings`
- `UserPublisherTwitterSettings` → `UserPlatformTwitterSettings`
- `UserPublisherLinkedInSettings` → `UserPlatformLinkedInSettings`
- `UserPublisherFacebookSettings` → `UserPlatformFacebookSettings`
- `UserPublisherSettings` table (legacy, already dropped per migration `2026-05-15-drop-userpublishersettings-table.sql` — confirm it's gone in `table-create.sql`)

---

## 6. Namespace Mapping Table

| Old Namespace | New Namespace |
|---|---|
| `JosephGuadagno.Broadcasting.Functions.Publishers` | `JosephGuadagno.Broadcasting.Functions.Dispatchers` |
| `JosephGuadagno.Broadcasting.Api.Controllers.Publishers` | `JosephGuadagno.Broadcasting.Api.Controllers.Dispatchers` |

All other namespaces are unchanged — the class/file renames happen within the same namespace.

---

## 7. Class / Interface Mapping Table

### Phase 1

| Old | New |
|---|---|
| `class RandomPosts` (namespace `Functions.Publishers`) | `class RandomPosts` (namespace `Functions.Dispatchers`) |
| `class ScheduledItems` (namespace `Functions.Publishers`) | `class ScheduledItems` (namespace `Functions.Dispatchers`) |

### Phase 2

| Layer | Old | New |
|---|---|---|
| Domain/Interface | `ISocialMediaPublisher` | `ISocialMediaDispatcher` |
| Domain/Interface | `IUserEventPublisherMappingManager` | `IUserEventDispatcherMappingManager` |
| Domain/Interface | `IUserEventPublisherMappingDataStore` | `IUserEventDispatcherMappingDataStore` |
| Domain/Model | `UserEventPublisherMapping` | `UserEventDispatcherMapping` |
| Domain/Constants | `ConfigurationFunctionNames.PublishersRandomPosts` | `ConfigurationFunctionNames.DispatchersRandomPosts` |
| Domain/Constants | `ConfigurationFunctionNames.PublishersScheduledItems` | `ConfigurationFunctionNames.DispatchersScheduledItems` |
| Functions/Service | `ICollectorEventPublisher` | `ICollectorEventDispatcher` |
| Functions/Service | `CollectorEventPublisher` | `CollectorEventDispatcher` |
| Functions/Service | `IScheduledItemEventPublisher` | `IScheduledItemEventDispatcher` |
| Functions/Service | `ScheduledItemEventPublisher` | `ScheduledItemEventDispatcher` |
| Data.Sql/Model | `UserEventPublisherMapping` (EF model) | `UserEventDispatcherMapping` |
| Data.Sql/Store | `UserEventPublisherMappingDataStore` | `UserEventDispatcherMappingDataStore` |
| Managers | `UserEventPublisherMappingManager` | `UserEventDispatcherMappingManager` |
| API/Controller | `PublishersController` | `DispatchersController` |
| API/Controller | `UserEventPublisherMappingController` | `UserEventDispatcherMappingController` |
| API/DTO | `PublishersAggregateResponse` | `DispatchersAggregateResponse` |
| Web/Controller | `PublishersController` | `DispatchersController` |
| Web/Controller | `UserEventPublisherMappingController` | `UserEventDispatcherMappingController` |
| Web/Service | `IPublishersAggregateService` | `IDispatchersAggregateService` |
| Web/Service | `PublishersAggregateService` | `DispatchersAggregateService` |
| Web/Service | `IUserEventPublisherMappingService` | `IUserEventDispatcherMappingService` |
| Web/Service | `UserEventPublisherMappingService` | `UserEventDispatcherMappingService` |
| Web/ViewModel | `PublishersAggregateViewModel` | `DispatchersAggregateViewModel` |
| Web/ViewModel | `UserEventPublisherMappingViewModel` | `UserEventDispatcherMappingViewModel` |
| Web/ViewModel | `PublisherPlatformCardViewModel` | `DispatcherPlatformCardViewModel` |
| Web/Constants | `PublisherEventTypes` | `DispatcherEventTypes` |
| Web/Constants | `PublisherEventTypeOption` | `DispatcherEventTypeOption` |

### Phase 3 (deferred — for reference)

| Old | New |
|---|---|
| `UserPublisherBlueskySettings` | `UserPlatformBlueskySettings` |
| `UserPublisherTwitterSettings` | `UserPlatformTwitterSettings` |
| `UserPublisherLinkedInSettings` | `UserPlatformLinkedInSettings` |
| `UserPublisherFacebookSettings` | `UserPlatformFacebookSettings` |
| `IUserPublisherBlueskySettingsManager` | `IUserPlatformBlueskySettingsManager` |
| `IUserPublisherTwitterSettingsManager` | `IUserPlatformTwitterSettingsManager` |
| `IUserPublisherLinkedInSettingsManager` | `IUserPlatformLinkedInSettingsManager` |
| `IUserPublisherFacebookSettingsManager` | `IUserPlatformFacebookSettingsManager` |
| `PublisherBlueskySettingsController` | `PlatformBlueskySettingsController` |
| `PublisherTwitterSettingsController` | `PlatformTwitterSettingsController` |
| `PublisherLinkedInSettingsController` | `PlatformLinkedInSettingsController` |
| `PublisherFacebookSettingsController` | `PlatformFacebookSettingsController` |

---

## 8. Documentation Changes Needed

See `neo-dispatcher-docs-outline.md` for the full docs plan. At minimum:

1. `docs\publishing-testing-guide.md` — rename and update all "publisher" vocabulary
2. `docs\per-user-credential-handling.md` — update terminology
3. `README.md` — update architecture description
4. `CONTRIBUTING.md` — update any naming guidance
5. `.github\copilot-instructions.md` — update examples that reference `Publishers/` controller path and function names
6. `.squad\skills\social-media-publisher-contract\SKILL.md` — rename skill and update contract description

---

## 9. Breaking Changes Register

| # | Change | Risk | Mitigation |
|---|---|---|---|
| BC-1 | API route `/Publishers` → `/Dispatchers` | HIGH — any API client breaks | Version the API or provide a redirect shim; coordinate with consumers |
| BC-2 | API sub-routes `/Publishers/BlueskySettings`, `/Publishers/RandomPostSettings`, `/Publishers/EventPublisherMappings`, etc. | HIGH | Same as BC-1 |
| BC-3 | Web URL `/Publishers` → `/Dispatchers` | LOW — internal URLs use tag helpers which auto-update; bookmarks break | Acceptable; add 301 redirect from old path |
| BC-4 | Azure Function name `PublishersRandomPosts` → `DispatchersRandomPosts` | MEDIUM — Azure portal metric alerts keyed on old name break | Update Azure portal alert rules and app-settings in lockstep |
| BC-5 | Azure Function name `PublishersScheduledItems` → `DispatchersScheduledItems` | MEDIUM | Same; also needs `FeedChecks.Name` data migration before code deploy |
| BC-6 | Config key `publishers_random_post_cron_settings` renamed | MEDIUM — Azure Function App application settings must be updated before deploy | Deploy config change first |
| BC-7 | `ISocialMediaPublisher.PublishAsync` → `ISocialMediaDispatcher.DispatchAsync` | LOW — internal interface; affects 4 manager implementations and 2 test startup files | Contained within the repo |


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


# Decision: Scalar API controller tags

**Author:** Neo (Lead)  
**Date:** 2026-05-28T14:15:57.412-07:00  
**Status:** ACCEPTED

---

## Decision

All controllers in
`src\JosephGuadagno.Broadcasting.Api\Controllers\` must declare a single
class-level `[Tags("...")]` attribute directly below `[ApiController]`.

Use the attribute from `Microsoft.AspNetCore.Http`:

```csharp
using Microsoft.AspNetCore.Http;

[ApiController]
[Tags("Dispatchers")]
[Authorize]
public class DispatchersController : ControllerBase
```

---

## Tagging convention

### Resource controllers use resource-specific tags

- `EngagementsController` → `"Engagements"`
- `SchedulesController` → `"Schedules"`
- `SocialMediaPlatformsController` → `"Social Media Platforms"`
- `SyndicationFeedItemsController` → `"Syndication Feed Items"`
- `YouTubeItemsController` → `"YouTube Items"`
- `MessageTemplatesController` → `"Message Templates"`

### Folder-based configuration controllers share category tags

- All controllers under `Controllers\Collectors\` → `"Collectors"`
- All controllers under `Controllers\Dispatchers\` → `"Dispatchers"`

---

## Why

Scalar groups operations by OpenAPI tags. Without explicit controller
tags, the docs fall back to default grouping and appear unordered or
fragmented.

Using one class-level tag per controller keeps the API reference
predictable, makes related settings endpoints appear together, and avoids
per-action tag duplication.

---

## Rule for future controllers

When adding a new API controller:

1. Put `[Tags("...")]` directly under `[ApiController]`.
2. Reuse the folder category tag for settings/aggregate controllers
   under `Collectors` and `Dispatchers`.
3. Use a resource-specific plural tag for top-level domain controllers.
4. Do not rely on default OpenAPI grouping behavior.


# Phase 2 Web Rename Complete

## Renamed files

- Controllers
  - `src\JosephGuadagno.Broadcasting.Web\Controllers\PublishersController.cs`
    → `src\JosephGuadagno.Broadcasting.Web\Controllers\DispatchersController.cs`
  - `src\JosephGuadagno.Broadcasting.Web\Controllers\UserEventPublisherMappingController.cs`
    → `src\JosephGuadagno.Broadcasting.Web\Controllers\UserEventDispatcherMappingController.cs`
- Interfaces
  - `src\JosephGuadagno.Broadcasting.Web\Interfaces\IPublishersAggregateService.cs`
    → `src\JosephGuadagno.Broadcasting.Web\Interfaces\IDispatchersAggregateService.cs`
  - `src\JosephGuadagno.Broadcasting.Web\Interfaces\IUserEventPublisherMappingService.cs`
    → `src\JosephGuadagno.Broadcasting.Web\Interfaces\IUserEventDispatcherMappingService.cs`
- Services
  - `src\JosephGuadagno.Broadcasting.Web\Services\PublishersAggregateService.cs`
    → `src\JosephGuadagno.Broadcasting.Web\Services\DispatchersAggregateService.cs`
  - `src\JosephGuadagno.Broadcasting.Web\Services\UserEventPublisherMappingService.cs`
    → `src\JosephGuadagno.Broadcasting.Web\Services\UserEventDispatcherMappingService.cs`
- Models
  - `src\JosephGuadagno.Broadcasting.Web\Models\PublishersAggregateViewModel.cs`
    → `src\JosephGuadagno.Broadcasting.Web\Models\DispatchersAggregateViewModel.cs`
  - `src\JosephGuadagno.Broadcasting.Web\Models\UserEventPublisherMappingViewModel.cs`
    → `src\JosephGuadagno.Broadcasting.Web\Models\UserEventDispatcherMappingViewModel.cs`
- Constants
  - `src\JosephGuadagno.Broadcasting.Web\Constants\PublisherEventTypes.cs`
    → `src\JosephGuadagno.Broadcasting.Web\Constants\DispatcherEventTypes.cs`
- Views
  - `src\JosephGuadagno.Broadcasting.Web\Views\Publishers\Index.cshtml`
    → `src\JosephGuadagno.Broadcasting.Web\Views\Dispatchers\Index.cshtml`
  - `src\JosephGuadagno.Broadcasting.Web\Views\UserEventPublisherMapping\Create.cshtml`
    → `src\JosephGuadagno.Broadcasting.Web\Views\UserEventDispatcherMapping\Create.cshtml`
  - `src\JosephGuadagno.Broadcasting.Web\Views\UserEventPublisherMapping\Edit.cshtml`
    → `src\JosephGuadagno.Broadcasting.Web\Views\UserEventDispatcherMapping\Edit.cshtml`
  - `src\JosephGuadagno.Broadcasting.Web\Views\UserEventPublisherMapping\Delete.cshtml`
    → `src\JosephGuadagno.Broadcasting.Web\Views\UserEventDispatcherMapping\Delete.cshtml`
  - `src\JosephGuadagno.Broadcasting.Web\Views\UserEventPublisherMapping\Index.cshtml`
    → `src\JosephGuadagno.Broadcasting.Web\Views\UserEventDispatcherMapping\Index.cshtml`

## Modified files

- `src\JosephGuadagno.Broadcasting.Web\Program.cs`
- `src\JosephGuadagno.Broadcasting.Web\Views\Shared\_LoginPartial.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\SiteAdmin\Users.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\Setup\Index.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\PublisherBlueskySettings\Index.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\PublisherFacebookSettings\Index.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\PublisherLinkedInSettings\Index.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\PublisherTwitterSettings\Index.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\Help\SocialMediaPlatforms\Bluesky.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\Help\SocialMediaPlatforms\Facebook.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\Help\SocialMediaPlatforms\LinkedIn.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\Help\SocialMediaPlatforms\Mastodon.cshtml`
- `src\JosephGuadagno.Broadcasting.Web\Views\Help\SocialMediaPlatforms\Twitter.cshtml`

## Build errors

- `dotnet build .\src\JosephGuadagno.Broadcasting.Web\ --configuration Release --nologo`
  - Fails in
    `src\JosephGuadagno.Broadcasting.Data.Sql\UserEventPublisherMappingDataStore.cs`.
  - Missing renamed backend types:
    `IUserEventPublisherMappingDataStore` and `UserEventPublisherMapping`.
  - Waiting on Morpheus/Data.Sql rename work.

## Notes

- Isolated validation succeeded after building managers without dependencies,
  then building the Web project without dependencies.
- The remaining blocking error is upstream in Data.Sql, not in the Web changes.

- `PublisherPlatformCardViewModel` did not exist in
  `PublisherPlatformSettingsViewModels.cs` on this branch.
- The card type was renamed where it actually lived:
  `DispatchersAggregateViewModel.cs`.


# Phase 2 Dispatcher Rename Complete

## Renamed files

- `src\JosephGuadagno.Broadcasting.Functions.Tests\Services\`
  `CollectorEventPublisherTests.cs` ->
  `src\JosephGuadagno.Broadcasting.Functions.Tests\Services\`
  `CollectorEventDispatcherTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\Services\`
  `ScheduledItemEventPublisherTests.cs` ->
  `src\JosephGuadagno.Broadcasting.Functions.Tests\Services\`
  `ScheduledItemEventDispatcherTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\Publishers\`
  `RandomPostsTests.cs` ->
  `src\JosephGuadagno.Broadcasting.Functions.Tests\Dispatchers\`
  `RandomPostsTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\Publishers\`
  `ScheduledItemsTests.cs` ->
  `src\JosephGuadagno.Broadcasting.Functions.Tests\Dispatchers\`
  `ScheduledItemsTests.cs`
- `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\`
  `UserEventPublisherMappingControllerTests.cs` ->
  `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\`
  `UserEventDispatcherMappingControllerTests.cs`
- `src\JosephGuadagno.Broadcasting.Web.Tests\Controllers\`
  `UserEventPublisherMappingControllerTests.cs` ->
  `src\JosephGuadagno.Broadcasting.Web.Tests\Controllers\`
  `UserEventDispatcherMappingControllerTests.cs`
- `src\JosephGuadagno.Broadcasting.Web.Tests\Services\`
  `UserEventPublisherMappingServiceTests.cs` ->
  `src\JosephGuadagno.Broadcasting.Web.Tests\Services\`
  `UserEventDispatcherMappingServiceTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Tests\`
  `UserEventPublisherMappingManagerTests.cs` ->
  `src\JosephGuadagno.Broadcasting.Managers.Tests\`
  `UserEventDispatcherMappingManagerTests.cs`
- `src\JosephGuadagno.Broadcasting.Data.Sql.Tests\`
  `UserEventPublisherMappingDataStoreTests.cs` ->
  `src\JosephGuadagno.Broadcasting.Data.Sql.Tests\`
  `UserEventDispatcherMappingDataStoreTests.cs`

## Modified files

- `src\JosephGuadagno.Broadcasting.Functions.Tests\Collectors\`
  `LoadNewPostsTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\Collectors\`
  `LoadNewVideosTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\Collectors\`
  `LoadNewSpeakingEngagementsTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\Bluesky\`
  `SendPostTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\LinkedIn\`
  `PostLinkTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\Facebook\`
  `PostPageStatusTests.cs`
- `src\JosephGuadagno.Broadcasting.Functions.Tests\Startup.cs`
- `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\`
  `ControllerAuthorizationPolicyTests.cs`
- `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\`
  `UserRandomPostSettingsControllerTests.cs`
- `src\JosephGuadagno.Broadcasting.Data.Sql.Tests\`
  `BroadcastingContextTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Bluesky.Tests\`
  `BlueskyManagerUnitTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.LinkedIn.Tests\`
  `LinkedInManagerUnitTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Facebook.Tests\`
  `FacebookManagerUnitTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Twitter.Tests\`
  `TwitterManagerTests.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Twitter.IntegrationTests\`
  `TwitterManagerTests.cs`

## Validation

- Broad grep for
  `ISocialMediaPublisher|CollectorEventPublisher|ScheduledItemEventPublisher|`
  `UserEventPublisherMapping` in `**/*Tests*.cs` returned no matches.
- `dotnet build .\src\ --no-restore --configuration Release` succeeded.
- `dotnet build .\src\JosephGuadagno.Broadcasting.Functions.Tests\`
  `--configuration Release` succeeded.
- `dotnet build .\src\JosephGuadagno.Broadcasting.Api.Tests\`
  `--configuration Release` succeeded.

## Remaining compilation errors

- None.


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


# Trinity Phase 1 Complete

**Date:** 2026-05-27
**Author:** Trinity
**Status:** COMPLETE

## What changed

Completed Phase 1 of the Publishers → Dispatchers rename for the timer-driven Functions dispatcher layer.

- Renamed `src\JosephGuadagno.Broadcasting.Functions\Publishers\` to `src\JosephGuadagno.Broadcasting.Functions\Dispatchers\`
- Updated the `RandomPosts` and `ScheduledItems` namespaces to `JosephGuadagno.Broadcasting.Functions.Dispatchers`
- Updated the Azure Function registration names from `PublishersRandomPosts` / `PublishersScheduledItems` to `DispatchersRandomPosts` / `DispatchersScheduledItems`
- Updated in-file dispatcher wording in `ScheduledItems.cs` log messages and variable naming
- Updated external test-file `using` statements and function-name constant references that depended on the old dispatcher namespace/function names

## New folder and namespace

- Folder: `src\JosephGuadagno.Broadcasting.Functions\Dispatchers\`
- Namespace: `JosephGuadagno.Broadcasting.Functions.Dispatchers`

## Azure cron config key names found

- `publishers_random_post_cron_settings`
- `publishers_scheduled_items_cron_settings`

## squad:Joe issue

- GitHub issue: `#999`


# Decision: Phase 2 Dispatchers Rename — Commit

**Date:** 2026-05-27T11:50:20.310-07:00
**Author:** Trinity
**Status:** ✅ COMPLETE

## Summary

Committed all Phase 2 changes for the Publishers → Dispatchers rename across
Domain, Data, Managers, API, Web, Functions, and DB layers.

## CancellationToken Fix

Before committing, fixed Neo's non-blocking review warning:

- **File:** `src/JosephGuadagno.Broadcasting.Functions/Services/CollectorEventDispatcher.cs`
- **Lines 154–156 (pre-fix):** `CreateIfNotExistsAsync()` and `SendMessageAsync()` were
  not forwarding `cancellationToken`.
- **Fix:** Added `cancellationToken: cancellationToken` to `CreateIfNotExistsAsync` and
  passed `cancellationToken` as positional argument to `SendMessageAsync`, matching
  the pattern already used in `ScheduledItemEventDispatcher.cs`.

## Build Result

`dotnet build .\src\ --configuration Release` — **0 errors, 0 warnings**.

## Commit

All Phase 2 changes staged with `git add -A` and committed with the conventional
commit message:

```
refactor(dispatchers): rename Publishers -> Dispatchers across Domain/Data/Managers/API/Web/DB (Phase 2)
```


# Trinity Phase 2 Dispatcher Rename Completion

## Renamed files

- `src\JosephGuadagno.Broadcasting.Domain\Interfaces\ISocialMediaPublisher.cs` -> `src\JosephGuadagno.Broadcasting.Domain\Interfaces\ISocialMediaDispatcher.cs`
- `src\JosephGuadagno.Broadcasting.Domain\Interfaces\IUserEventPublisherMappingManager.cs` -> `src\JosephGuadagno.Broadcasting.Domain\Interfaces\IUserEventDispatcherMappingManager.cs`
- `src\JosephGuadagno.Broadcasting.Domain\Interfaces\IUserEventPublisherMappingDataStore.cs` -> `src\JosephGuadagno.Broadcasting.Domain\Interfaces\IUserEventDispatcherMappingDataStore.cs`
- `src\JosephGuadagno.Broadcasting.Domain\Models\UserEventPublisherMapping.cs` -> `src\JosephGuadagno.Broadcasting.Domain\Models\UserEventDispatcherMapping.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Services\ICollectorEventPublisher.cs` -> `src\JosephGuadagno.Broadcasting.Functions\Services\ICollectorEventDispatcher.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Services\CollectorEventPublisher.cs` -> `src\JosephGuadagno.Broadcasting.Functions\Services\CollectorEventDispatcher.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Services\IScheduledItemEventPublisher.cs` -> `src\JosephGuadagno.Broadcasting.Functions\Services\IScheduledItemEventDispatcher.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Services\ScheduledItemEventPublisher.cs` -> `src\JosephGuadagno.Broadcasting.Functions\Services\ScheduledItemEventDispatcher.cs`
- `src\JosephGuadagno.Broadcasting.Managers\UserEventPublisherMappingManager.cs` -> `src\JosephGuadagno.Broadcasting.Managers\UserEventDispatcherMappingManager.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\PublishersController.cs` -> `src\JosephGuadagno.Broadcasting.Api\Controllers\Dispatchers\DispatchersController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\UserEventPublisherMappingController.cs` -> `src\JosephGuadagno.Broadcasting.Api\Controllers\Dispatchers\UserEventDispatcherMappingController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\BlueskySettingsController.cs` -> `src\JosephGuadagno.Broadcasting.Api\Controllers\Dispatchers\BlueskySettingsController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\FacebookSettingsController.cs` -> `src\JosephGuadagno.Broadcasting.Api\Controllers\Dispatchers\FacebookSettingsController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\LinkedInSettingsController.cs` -> `src\JosephGuadagno.Broadcasting.Api\Controllers\Dispatchers\LinkedInSettingsController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\TwitterSettingsController.cs` -> `src\JosephGuadagno.Broadcasting.Api\Controllers\Dispatchers\TwitterSettingsController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\UserRandomPostSettingsController.cs` -> `src\JosephGuadagno.Broadcasting.Api\Controllers\Dispatchers\UserRandomPostSettingsController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Dtos\PublishersAggregateResponse.cs` -> `src\JosephGuadagno.Broadcasting.Api\Dtos\DispatchersAggregateResponse.cs`

## Modified files

- `src\JosephGuadagno.Broadcasting.Domain\Interfaces\ITwitterManager.cs`
- `src\JosephGuadagno.Broadcasting.Domain\Models\SocialMediaPublishRequest.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Program.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Dispatchers\RandomPosts.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Dispatchers\ScheduledItems.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Collectors\SyndicationFeed\LoadNewPosts.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Collectors\YouTube\LoadNewVideos.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Collectors\SpeakingEngagement\LoadNewSpeakingEngagements.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Bluesky\SendPost.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Facebook\PostPageStatus.cs`
- `src\JosephGuadagno.Broadcasting.Functions\LinkedIn\PostLink.cs`
- `src\JosephGuadagno.Broadcasting.Functions\Twitter\SendTweet.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Bluesky\Interfaces\IBlueskyManager.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Bluesky\BlueskyManager.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Facebook\Interfaces\IFacebookManager.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Facebook\FacebookManager.cs`
- `src\JosephGuadagno.Broadcasting.Managers.LinkedIn\Models\ILinkedInManager.cs`
- `src\JosephGuadagno.Broadcasting.Managers.LinkedIn\LinkedInManager.cs`
- `src\JosephGuadagno.Broadcasting.Managers.Twitter\TwitterManager.cs`
- `src\JosephGuadagno.Broadcasting.Api\Program.cs`
- `src\JosephGuadagno.Broadcasting.Api\Dtos\UserPublisherSettingsDtos.cs`
- `src\JosephGuadagno.Broadcasting.Api\MappingProfiles\ApiBroadcastingProfile.cs`

## Build results

### Succeeded

- `dotnet build .\src\JosephGuadagno.Broadcasting.Domain\JosephGuadagno.Broadcasting.Domain.csproj --configuration Release`
- `dotnet build .\src\JosephGuadagno.Broadcasting.Managers.Bluesky\JosephGuadagno.Broadcasting.Managers.Bluesky.csproj --configuration Release`
- `dotnet build .\src\JosephGuadagno.Broadcasting.Managers.Facebook\JosephGuadagno.Broadcasting.Managers.Facebook.csproj --configuration Release`
- `dotnet build .\src\JosephGuadagno.Broadcasting.Managers.LinkedIn\JosephGuadagno.Broadcasting.Managers.LinkedIn.csproj --configuration Release`
- `dotnet build .\src\JosephGuadagno.Broadcasting.Managers.Twitter\JosephGuadagno.Broadcasting.Managers.Twitter.csproj --configuration Release`

### Blocked / failed

- `dotnet build .\src\JosephGuadagno.Broadcasting.Managers\JosephGuadagno.Broadcasting.Managers.csproj --configuration Release`
- `dotnet build .\src\JosephGuadagno.Broadcasting.Functions\JosephGuadagno.Broadcasting.Functions.csproj --configuration Release`
- `dotnet build .\src\JosephGuadagno.Broadcasting.Api\JosephGuadagno.Broadcasting.Api.csproj --configuration Release`

All three failures are waiting on **Morpheus**. `src\JosephGuadagno.Broadcasting.Data.Sql\UserEventPublisherMappingDataStore.cs` still references the old `IUserEventPublisherMappingDataStore` interface and `UserEventPublisherMapping` model, so downstream projects fail until the Data.Sql rename lands.

No build blocker surfaced from **Switch** during this phase.


# Trinity — PR #998 opened for #995

- Date: 2026-05-26T11:56:31.095-07:00
- PR: #998 — feat(#995): per-user publisher routing — replace Event Grid dispatch
- Manual steps issue: #997
- Cleanup commit: `83e4a8a5`
- Notes: Cleanup removed dead global RandomPost/Event Grid plumbing after the per-user routing migration. Full build and CI-aligned tests passed before the branch was pushed and the PR was opened.


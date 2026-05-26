# Team Decisions

Compiled record of team decisions, architecture choices, and resolutions.

> Entries before 2026-05-22 archived to decisions-archive.md (last archived: 2026-05-25)

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


# Decision: Fix LinkedInControllerTests Signature Mismatch

**Date:** 2026-05-21  
**Author:** Trinity (Backend Dev)  
**Status:** ✅ COMPLETE


---
## Summary

Fixed a pre-existing compiler error in `LinkedInControllerTests.cs` where the test called `await controller.RefreshToken()`, but the production `LinkedInController.RefreshToken()` method returns synchronous `IActionResult`.


---
## Change

**File:** `src/JosephGuadagno.Broadcasting.Web.Tests/Controllers/LinkedInControllerTests.cs`

- Test method `RefreshToken_WhenCallbackUrlIsValid_ShouldRedirectToLinkedInAuthUrl` changed from `async Task` → `void`
- Removed `await` keyword from `controller.RefreshToken()` call (line 214)

**No production code was modified.**


---
## Root Cause

A prior change to `LinkedInController.RefreshToken()` simplified its return type from `async Task<IActionResult>` to synchronous `IActionResult` (the method only builds a URL string and calls `Redirect()` — no async work needed). The test was not updated at that time, leaving a CS1061 compiler error.


---
## Impact

- Build: 0 errors (was 1 error)
- `LinkedInControllerTests`: 12/12 pass
- No other test regressions introduced


---
## Pre-existing Failures (out of scope)

Two Functions tests remain failing and are unrelated:
- `LoadAllSpeakingEngagementsTests.RunAsync_HandlesNullEngagementsList_Gracefully`
- `LoadNewPostsTests.RunAsync_HandlesNullFeedList_Gracefully`

These fail with `Assert.IsType() Failure` and predate this fix.


---

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




> Entries before 2026-05-18 archived to decisions-archive.md


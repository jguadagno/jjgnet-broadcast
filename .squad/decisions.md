# Team Decisions

Compiled record of team decisions, architecture choices, and resolutions.

---

## Decision: Azure Functions Stable Port via .WithHttpEndpoint() in AppHost

**Date:** 2026-05-16  
**Author:** Cypher (DevOps Engineer)  
**Requested by:** Joseph Guadagno  
**Status:** Implemented

### Context

The Azure Functions project (JosephGuadagno.Broadcasting.Functions) had unstable port assignment
in local Aspire environments. Aspire assigned a random proxy port to the Functions resource on every
run, making it impossible to predict the local endpoint without inspecting the Aspire dashboard.

**Initial Approach Attempted:** Add Properties/launchSettings.json with a fixed pplicationUrl.
**Outcome:** Failed. launchSettings.json does NOT work for Azure Functions isolated worker model
in Aspire. The launch settings are ignored by the Functions host.

### Decision (Final)

**Delete** src/JosephGuadagno.Broadcasting.Functions/Properties/launchSettings.json
and any Properties/ directory if empty.

**Add** .WithHttpEndpoint(port: 7071, isProxied: false) to the Functions resource in AppHost.cs.

Port 7071 is the Azure Functions conventional default and does not conflict with any other
project in the solution:

| Project    | HTTP port | HTTPS port |
|------------|-----------|------------|
| API        | 5272      | 7272       |
| Web        | 5224      | 7224       |
| AppHost    | 15061     | 17282      |
| **Functions** | **7071** | *(none)*  |

### Rationale

launchSettings.json is a Visual Studio-specific launch configuration and is ignored by the
Azure Functions isolated worker model when running under Aspire orchestration. The correct
pattern for Aspire is to use the resource builder API in AppHost.cs.

The .WithHttpEndpoint(port: 7071, isProxied: false) configuration:
- Sets the Functions resource to bind directly to port 7071 (not through Aspire's reverse proxy)
- isProxied: false is the correct pattern for Azure Functions — the Functions host manages
  its own HTTP binding, bypassing Aspire's proxy layer
- Ensures stable port across all local runs

### Files Changed

- **Deleted:** src/JosephGuadagno.Broadcasting.Functions/Properties/launchSettings.json
- **Modified:** src/JosephGuadagno.Broadcasting.AppHost/AppHost.cs
  - Added .WithHttpEndpoint(port: 7071, isProxied: false) to Functions resource registration

### Verification

dotnet build .\src\ --no-restore --configuration Release — Build succeeded, 0 errors.
Functions resource now consistently binds to http://localhost:7071 across all local Aspire runs.

---

## Decision: Phase 1 Complete — IPostComposer/PostComposer + Dead Code Removal

**Date:** 2026-05-18  
**Author:** Neo  
**Branch:** `issue-980-publisher-architecture-refactor`  
**Commit:** `dbfa2589`  
**Status:** COMPLETE ✅

### Summary

Phase 1 of the publisher architecture refactor (Issue #980) is complete. This phase was additive and a prerequisite for Phases 3–5.

### What Was Done

#### Part A — Dead Code Deletion

Deleted 4 plural `*PublisherSettings` domain classes with zero references:

- `Domain/Models/BlueskyPublisherSettings.cs`
- `Domain/Models/FacebookPublisherSettings.cs`
- `Domain/Models/LinkedInPublisherSettings.cs`
- `Domain/Models/TwitterPublisherSettings.cs`

Grep confirmed zero references before deletion. The singular counterparts (`BlueskyPublisherSetting`, etc.) were **not** touched — they are legitimate domain value objects used in `UserPublisherSetting` aggregates.

#### Part B — IPostComposer Interface

Created `src/JosephGuadagno.Broadcasting.Domain/Interfaces/IPostComposer.cs` with a single `ComposeAsync(SocialMediaPublishRequest, string, CancellationToken)` method.

#### Part C — PostComposer Implementation

Created `src/JosephGuadagno.Broadcasting.Managers/PostComposer.cs`:

- Uses Scriban 7.2.0 (newly added to Managers.csproj)
- Template variables exposed: `{{ title }}`, `{{ url }}`, `{{ description }}`, `{{ tags }}`, `{{ image_url }}`
- `url` resolves as `ShortenedUrl ?? LinkUrl ?? ""` (prefers short URL)
- `tags` is space-joined `#`-prefixed hashtags from `Hashtags: IReadOnlyCollection<string>?`
- Logs a warning on Scriban parse/render exceptions and returns `null` — never throws to caller

#### Part D — DI Registration

Registered `services.TryAddScoped<IPostComposer, PostComposer>()` in all three consumers:

- `src/JosephGuadagno.Broadcasting.Api/Program.cs`
- `src/JosephGuadagno.Broadcasting.Functions/Program.cs`
- `src/JosephGuadagno.Broadcasting.Web/Program.cs`

Pattern matches existing `TryAddScoped` style used throughout the solution.

### Deviations from Proposal

**None.** Implementation follows the proposal exactly.

One observation: the proposal mentioned `ConsumerKey`/`ConsumerSecret`/`AccessTokenSecret` on `SocialMediaPublishRequest` for Twitter's per-user credentials (Phase 5 work). These properties do **not** currently exist on that model — they will be added as part of Phase 5, not Phase 1.

### Build/Test

- `dotnet build .\src\ --no-restore --configuration Release` → **0 errors, 0 warnings**
- `dotnet test .\src\ --no-build --configuration Release --filter "FullyQualifiedName!~SyndicationFeedReader"` → **422 passed, 0 failed**

### Next Phase

**Phase 2:** Extract `IMessageTemplateLookup` — user-scoped template resolution with `GetAsync(platformName, messageType, ownerEntraOid)`.

---

## Decision: Publisher Architecture — Finalized Decisions

**Author:** Neo (Lead)  
**Date:** 2026-05-18  
**By:** Joseph Guadagno (via Copilot)  
**Type:** Decision Record  
**Related proposal:** `.squad/decisions/inbox/neo-publisher-architecture-proposal.md`

### 2026-05-18: Publisher Architecture — Finalized Decisions

**What:** Finalized the social media publisher architecture refactor with the following confirmed decisions:

1. **Always per-user credentials** — No shared/system accounts on any platform. `TwitterManager.PublishAsync()` always builds `TwitterContext` from per-user credentials in `SocialMediaPublishRequest`. The global `TwitterContext` DI registration is removed. There is no shared-account fallback on any platform.

2. **Queue DTO unification is in-scope** — Queues are currently empty. Replace `BlueskyPostMessage`, `TwitterTweetMessage`, `FacebookPostStatus`, `LinkedInPostLink` with `SocialMediaPublishRequest` as part of this refactor. No deployment window or queue drain needed. This also fixes the `LinkedInPostLink` layering violation (Functions referencing `Managers.LinkedIn.Models`).

3. **All event types require user-scoped templates** — Including `RandomPost`, `NewSyndicationData`, `NewSpeakingEngagement`, `NewYouTubeData`. No hardcoded strings anywhere in `Process*` functions. Templates are managed via:
   - Issue #978 — creation of all required templates for a publisher; a user cannot publish to a provider unless all required templates are present
   - Issue #979 — seeds default templates for a user during initial setup

4. **Templates are REQUIRED — no global fallback** — `IMessageTemplateLookup` is user-scoped only. `GetAsync()` returns `null` if the user has no template for the given platform/message type. `Process*` functions must bail (return `null`, skip enqueue) when the template is null. Users cannot publish until templates are present (enforced via #978 and #979).

5. **Hashtags rendered inline by template** — `PostComposer` provides `{{ tags }}` as a formatted string (space-separated, `#`-prefixed, e.g., `#HappyHour #ItsFiveOclock`). The template author decides placement — hashtags appear as inline text in the composed message. Bluesky's `PublishAsync()` will parse `HashTag` facets from `request.Text` using AT Protocol text scanning rather than reading from a separate `Hashtags` list. This simplifies the contract: `request.Text` is the canonical composed output.

6. **RandomPost requires its own template type per user** — `ProcessNewRandomPost` across all four platforms will require a `RandomPost` message template type per user. This is specific to Joe's workflow preferences and must be included in the user setup flow. Tracked under issues #978 (required templates) and #979 (default templates on setup).

**Why:** Architecture decision for the publisher refactor — required for consistent multi-user support, clean SRP across publisher managers, and elimination of hardcoded composition logic scattered across 20+ Azure Functions.

**Dependencies before implementation:**
- Issue #978 must define and enforce required templates per publisher
- Issue #979 must seed default templates on user setup (including RandomPost type)

---

## Decision: Publisher Architecture — Model Placement and Settings Cleanup

**By:** Joseph Guadagno (via Copilot)  
**Date:** 2026-05-18  
**Related:** #980

### What

Two additions to the #980 scope identified via follow-up analysis:

#### 1. Dead credential-holder models in Domain (delete, not move)

Four **`*PublisherSettings` (plural)** classes live in `JosephGuadagno.Broadcasting.Domain.Models`
and are **completely unreferenced** (dead code). They are platform-specific credential holders
that duplicate models already in the manager projects:

| Class (Domain) | Manager-side equivalent | References |
|---|---|---|
| `BlueskyPublisherSettings` | `Managers.Bluesky.Models.BlueskySettings` | 0 (dead code) |
| `FacebookPublisherSettings` | `Managers.Facebook.Models.FacebookApplicationSettings` | 0 (dead code) |
| `LinkedInPublisherSettings` | `Managers.LinkedIn.Models.LinkedInApplicationSettings` | 0 (dead code) |
| `TwitterPublisherSettings` | *(no manager equivalent)* | 0 (dead code) |

**Decision:** Delete all four. No migration required — they have zero references in the solution.

These must not be confused with the `*PublisherSetting` **(singular)** classes, which are
**legitimate domain models** that belong in Domain:

| Class (Domain) | Purpose | Used by |
|---|---|---|
| `BlueskyPublisherSetting` | `Has*` boolean display model — no credentials | `UserPublisherSetting` aggregate |
| `FacebookPublisherSetting` | Same | `UserPublisherSetting` aggregate |
| `LinkedInPublisherSetting` | Same | `UserPublisherSetting` aggregate |
| `TwitterPublisherSetting` | Same | `UserPublisherSetting` aggregate |

The singular classes expose only `Has*` booleans (never raw credentials) and are value objects
of the generic `UserPublisherSetting` API response aggregate. They are correctly placed in Domain.

#### 2. Queue DTOs in Domain.Models.Messages

The following queue message DTOs currently live in
`JosephGuadagno.Broadcasting.Domain.Models.Messages`:

| Class | Status |
|---|---|
| `TwitterTweetMessage` | Platform-specific; should be in `Managers.Twitter.Models` |
| `FacebookPostStatus` | Platform-specific; should be in `Managers.Facebook.Models` |
| `LinkedInPostLink` | Platform-specific; should be in `Managers.LinkedIn.Models` |
| `LinkedInPostText` | Platform-specific; should be in `Managers.LinkedIn.Models` |
| `LinkedInPostImage` | Platform-specific; should be in `Managers.LinkedIn.Models` |

**Contrast:** `BlueskyPostMessage` already lives correctly in `Managers.Bluesky.Models`.

**Decision:** These DTOs are already slated for **deletion** in Phase 6 of #980
(unification into `SocialMediaPublishRequest`). Do NOT move them first — that is wasted churn.
They stay in Domain until Phase 6 deletes them. The Phase 6 PR must also clean up the
`Domain.Models.Messages` folder entirely (except `Email.cs` which is not platform-specific).

If for any reason Phase 6 is descoped, these five DTOs should be moved to their respective
manager project `Models` folders. Files that would need updating:
- All files under `JosephGuadagno.Broadcasting.Functions` that reference these types
  (via `using JosephGuadagno.Broadcasting.Domain.Models.Messages`)
- Manager projects would need no reference changes (they already depend on Domain or would
  gain local models)

### Why

The Domain project should contain only:
- Shared/generic domain models and value objects
- Interfaces consumed across multiple projects
- Cross-cutting aggregates (e.g., `UserPublisherSetting`)

Platform-specific credential holders and queue message shapes are implementation details that
belong in their platform's manager project (or, better, unified into the generic
`SocialMediaPublishRequest` per Phase 6).

Leaving dead code in Domain creates confusion about which class to use (singular vs. plural) and
pollutes IntelliSense with stale types.

### Files to Delete (as part of #980)

```
src/JosephGuadagno.Broadcasting.Domain/Models/BlueskyPublisherSettings.cs   ← DELETE
src/JosephGuadagno.Broadcasting.Domain/Models/FacebookPublisherSettings.cs  ← DELETE
src/JosephGuadagno.Broadcasting.Domain/Models/LinkedInPublisherSettings.cs  ← DELETE
src/JosephGuadagno.Broadcasting.Domain/Models/TwitterPublisherSettings.cs   ← DELETE
```

Phase 6 also deletes (as part of queue DTO unification):
```
src/JosephGuadagno.Broadcasting.Domain/Models/Messages/TwitterTweetMessage.cs   ← Phase 6
src/JosephGuadagno.Broadcasting.Domain/Models/Messages/FacebookPostStatus.cs    ← Phase 6
src/JosephGuadagno.Broadcasting.Domain/Models/Messages/LinkedInPostLink.cs      ← Phase 6
src/JosephGuadagno.Broadcasting.Domain/Models/Messages/LinkedInPostText.cs      ← Phase 6
src/JosephGuadagno.Broadcasting.Domain/Models/Messages/LinkedInPostImage.cs     ← Phase 6
```

---

## Decision: Phase 2 Complete — IMessageTemplateLookup + SocialMediaPublishRequest Extended

**Date:** 2026-05-18  
**Author:** Neo  
**Branch:** `issue-980-publisher-architecture-refactor`  
**Commit:** `e63c4012`  
**Status:** COMPLETE ✅

### Summary

Phase 2 of the publisher architecture refactor (#980) is complete. All work is additive — no existing callers were modified.

### What Was Done

#### New Files

- `src/JosephGuadagno.Broadcasting.Domain/Interfaces/IMessageTemplateLookup.cs`  
  Interface with `GetAsync(platformName, messageType, ownerEntraOid, cancellationToken)` — required return (null means bail, no global fallback).

- `src/JosephGuadagno.Broadcasting.Managers/MessageTemplateLookup.cs`  
  Implementation: calls `ISocialMediaPlatformManager.GetByNameAsync(platformName)` to resolve the platform ID, then calls `IMessageTemplateDataStore.GetAsync(platformId, messageType)`. Logs a warning if either lookup returns null. Returns null in both failure cases so callers can bail.

#### Modified Files

- `Api/Program.cs`, `Functions/Program.cs`, `Web/Program.cs` — registered `IMessageTemplateLookup`/`MessageTemplateLookup` with `TryAddScoped`.
- `Domain/Models/SocialMediaPublishRequest.cs` — added four missing properties (see below).

### Key Finding: IMessageTemplateDataStore Has No User-Scoped Single-Item Overload

`IMessageTemplateDataStore.GetAsync` only exists in one single-item form:

```csharp
Task<MessageTemplate?> GetAsync(int socialMediaPlatformId, string messageType, CancellationToken cancellationToken = default);
```

There is no `GetAsync(int platformId, string messageType, string ownerEntraOid)` overload.

**Decision:** `MessageTemplateLookup` calls the non-scoped version for now, with a `// TODO(#980 Phase 3)` comment. Full user-scoping activates in Phase 3 when `Process*` Functions migrate to use `IMessageTemplateLookup`. At that point `IMessageTemplateDataStore` should be extended with the user-scoped single-item overload.

### SocialMediaPublishRequest Properties Added

All four were absent and were added:

| Property | Type | Purpose |
|---|---|---|
| `OwnerEntraOid` | `string?` | Entra OID of content owner; used by Send functions for per-user credential lookup |
| `ConsumerKey` | `string?` | Twitter OAuth consumer key |
| `ConsumerSecret` | `string?` | Twitter OAuth consumer secret |
| `AccessTokenSecret` | `string?` | Twitter OAuth access token secret |

Note: `AccessToken` was already present on the model (added in Phase 1 research).

### TODOs for Phase 3

1. Add `GetAsync(int platformId, string messageType, string ownerEntraOid, CancellationToken)` overload to `IMessageTemplateDataStore` and its SQL implementation.
2. Update `MessageTemplateLookup.GetAsync` to call the new user-scoped overload (remove the TODO comment).
3. Migrate all `Process*` Functions to use `IMessageTemplateLookup` instead of inline two-step lookup.

---

## Decision: Phase 4 Complete — Composition Stripped from Publisher Managers

**Date:** 2026-05-18  
**Author:** Neo  
**Branch:** `issue-980-publisher-architecture-refactor`  
**Commit:** `7176c08d`  
**Status:** COMPLETE ✅

### Summary

Phase 4 of issue #980 is complete. All post-composition logic has been stripped from the four publisher managers. Each manager now has a single `PublishAsync(SocialMediaPublishRequest)` entry point that posts directly to the platform API.

### Changes Made

#### Interfaces

- `IBlueskyManager`: removed `ComposeMessageAsync`
- `ITwitterManager`: removed `SendTweetAsync` and `ComposeMessageAsync` (now a pure marker interface extending `ISocialMediaPublisher`)
- `IFacebookManager`: removed `ComposeMessageAsync`
- `ILinkedInManager`: removed `ComposeMessageAsync`

#### Manager Implementations

- **BlueskyManager**: removed 5 composition constructor params, removed `ComposeMessageAsync`/`GetMessageType`/`TryRenderTemplateAsync`, removed hashtag-appending loop in `PublishAsync` (now inline via `{{ tags }}`)
- **TwitterManager**: complete rewrite — removed global `TwitterContext` constructor dep, new `TweetAsync` takes per-user credentials from `SocialMediaPublishRequest`, removed `SendTweetAsync`/`ComposeMessageAsync`/`GetMessageType`/`TryRenderTemplateAsync`
- **FacebookManager**: removed 5 composition constructor params and 3 methods
- **LinkedInManager**: removed 5 composition constructor params, 3 methods, and `ILogger` (no longer has any log calls)

#### csproj Files

Removed `Scriban` package reference from all 4 manager csproj files.

#### Tests

- All 4 unit test projects updated to match new constructors
- Twitter unit tests rewritten with `TestableTwitterManager` overriding new `TweetAsync(consumerKey, consumerSecret, accessToken, accessTokenSecret, text)` signature
- Twitter integration tests updated: `SendTweetAsync` → `PublishAsync` (all still `Skip = "Manually run only"`)

#### Functions/Program.cs

- `ConfigureTwitter` parameter `IConfiguration config` removed (no longer needed)
- Removed: `InMemoryCredentialStore`, `IAuthorizer`, `TwitterContext` singleton registrations
- Removed `TwitterHealthCheck` from health check registrations

#### Deleted

- `TwitterHealthCheck.cs` — validated global OAuth credentials that no longer exist

### Key Decisions Recorded

- Global `TwitterContext` DI is removed; credentials are always per-user
- `request.Text` is the canonical composed output entering each manager
- Hashtags are rendered inline by the template composer via `{{ tags }}`
- `ILogger` removed from `LinkedInManager` (zero log call sites remain after the Scriban methods were deleted)

### Test Results

All tests pass. 0 failures. (SyndicationFeedReader network tests excluded per CI policy.)

---

## Decision: Phase 5 Complete — Send* Functions Dequeue SocialMediaPublishRequest

**Date**: 2026-05-18  
**Branch**: `issue-980-publisher-architecture-refactor`  
**Commit**: `bcfbf962`  
**Status:** COMPLETE ✅

### Summary

Phase 5 of the publisher architecture refactor (#980) is complete. All Send* Functions
now dequeue `SocialMediaPublishRequest`, look up per-user credentials, inject them into
the request, and call `manager.PublishAsync(request)`. All 20 Process* Functions now
enqueue `SocialMediaPublishRequest` instead of platform-specific DTOs.

### Files Changed (33)

#### Process* Functions (20 files — return type change only)

| Platform  | Files Changed |
|-----------|--------------|
| Twitter   | ProcessNewSyndicationDataFired, ProcessScheduledItemFired, ProcessNewYouTubeDataFired, ProcessNewSpeakingEngagementFired, ProcessNewRandomPost |
| Bluesky   | ProcessNewSyndicationDataFired, ProcessScheduledItemFired, ProcessNewYouTubeDataFired, ProcessNewSpeakingEngagementFired, ProcessNewRandomPost |
| Facebook  | ProcessNewSyndicationDataFired, ProcessScheduledItemFired, ProcessNewYouTubeDataFired, ProcessNewSpeakingEngagementFired, ProcessNewRandomPost |
| LinkedIn  | ProcessNewSyndicationDataFired, ProcessScheduledItemFired, ProcessNewYouTubeDataFired, ProcessNewSpeakingEngagementFired, ProcessNewRandomPost |

LinkedIn Process* functions also had `IUserOAuthTokenManager` removed from constructors
(token lookup moved to `PostLink.cs`).

#### Send* Functions (4 files)

- **`Twitter/SendTweet.cs`**: Added `ITwitterManager`; changed trigger type to
  `SocialMediaPublishRequest`; looks up 4 Twitter credentials, injects them, calls
  `twitterManager.PublishAsync(request)`; removed `LinqToTwitter` direct usage.
- **`Bluesky/SendPost.cs`**: Changed trigger type to `SocialMediaPublishRequest`;
  looks up Bluesky username + app password, injects as `request.AuthorId` and
  `request.AccessToken`; calls `blueskyManager.PublishAsync(request)`; removed manual
  `PostBuilder`/`BlueskyAgent` construction.
- **`Facebook/PostPageStatus.cs`**: Changed trigger type to `SocialMediaPublishRequest`;
  looks up page credentials, injects as `request.AccessToken` and `request.AuthorId`;
  calls `facebookManager.PublishAsync(request)`.
- **`LinkedIn/PostLink.cs`**: Changed trigger type to `SocialMediaPublishRequest`;
  added `IUserOAuthTokenManager` and `IUserPublisherLinkedInSettingsManager`; looks up
  OAuth token + `AuthorId`; calls `linkedInManager.PublishAsync(request)`; removed
  `HttpClient` (manager handles image download); **fixes pre-existing `AuthorId` null bug**.

#### Managers (2 files)

- **`BlueskyManager.cs`**: `PublishAsync` now creates a per-user `BlueskyAgent` using
  `request.AuthorId` (handle) and `request.AccessToken` (app password); builds
  `PostBuilder` with link facets and hashtag facets from `request.Hashtags`; throws
  `BlueskyPostException` on login or post failure. Private `GetEmbeddedExternalRecordAsync`
  and `GetEmbeddedExternalRecordWithThumbnailAsync` overloads added that accept a
  `BlueskyAgent` parameter (used by the per-user flow). Public interface methods unchanged.
- **`FacebookManager.cs`**: `PublishAsync` now uses `request.AccessToken` (page access
  token) and `request.AuthorId` (page ID) via the existing 4/5-arg internal overloads;
  throws `ArgumentException` if either credential is missing.

#### Tests (6 files updated)

- `SendPostTests.cs`, `PostPageStatusTests.cs`, `PostLinkTests.cs` — updated to use
  `SocialMediaPublishRequest`, mock `PublishAsync`, updated constructor signatures.
- `ProcessScheduledItemFiredTests.cs` (Bluesky, Facebook, LinkedIn) — updated for
  constructor changes (LinkedIn: removed `IUserOAuthTokenManager` mock).

### Decisions Made

1. **Bluesky hashtag strategy**: Phase 5 appends hashtags from `request.Hashtags` list
   (same as old `SendPost.cs`). The decisions.md note about "AT Protocol text scanning"
   is deferred — hashtags are still rendered inline via template `{{ tags }}` and appended
   as facets from the list.

2. **No service-level fallback**: Both `BlueskyManager.PublishAsync` and
   `FacebookManager.PublishAsync` throw if per-user credentials are missing. There is no
   fallback to service-level settings, in line with the architecture decision
   ("no shared/system accounts on any platform").

3. **LinkedIn `AuthorId` bug fixed**: The old LinkedIn Process* functions never set
   `AuthorId` in the `LinkedInPostLink` DTO. The new `PostLink.cs` correctly looks up
   `settings.AuthorId` from `IUserPublisherLinkedInSettingsManager`.

### Phase 6 Remaining Work

- Delete old DTO files from `Domain/Models/Messages/`:
  - `BlueskyPostMessage.cs`
  - `TwitterTweetMessage.cs`
  - `FacebookPostStatus.cs`
  - `LinkedInPostLink.cs`
- These DTOs are no longer enqueued or dequeued by any function but may still be
  referenced in test helpers or legacy code — verify before deleting.


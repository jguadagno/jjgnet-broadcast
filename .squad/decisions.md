# Team Decisions

Compiled record of team decisions, architecture choices, and resolutions.
---
## Facebook Token Expiry Notifications — Not Required

**Date:** 2026-05-21  
**Author:** Joseph Guadagno (via Copilot)  
**Status:** DECISION  

### Decision
Facebook token expiry notifications (section 3.4 of neo-oauth-token-architecture recommendation) are **NOT needed**. The `RefreshTokens` Azure Function handles Facebook token renewal automatically — there is no need for user-facing expiry notifications for Facebook tokens.

### Rationale
- User request — captured for team memory
- LinkedIn requires user-intervention notifications because LinkedIn tokens cannot be refreshed automatically
- Facebook tokens CAN be refreshed automatically, so no notification is needed
- This decision removes scope from the neo-oauth-token-architecture work

---
## OAuth Token Architecture — LinkedIn & Facebook

**Date:** 2026-05-21  
**Author:** Neo  
**Status:** PROPOSAL — awaiting Joseph approval  
**Requested by:** Joseph Guadagno

---

### 1. Current State Summary

#### 1.1 LinkedIn

| Layer | What exists |
|-------|-------------|
| SQL | `UserOAuthTokens` table (`CreatedByEntraOid`, `SocialMediaPlatformId = 3`, `AccessToken`, `RefreshToken`, expiry timestamps, `LastNotifiedAt`) |
| SQL | `UserPublisherLinkedInSettings` table: `IsEnabled`, `AuthorId`, `ClientId`, `HasClientSecret`, **`HasAccessToken`** (boolean flag) |
| KV | `publisher-{ownerOid}-linkedin-access-token` — written by `UserPublisherLinkedInSettingsManager.StoreAccessTokenAsync()` |
| KV | `publisher-{ownerOid}-linkedin-client-secret` |
| Manager | `IUserOAuthTokenManager` / `UserOAuthTokenManager` — full CRUD + expiry window queries |
| Manager | `IUserPublisherLinkedInSettingsManager` — settings + `GetAccessTokenAsync()` / `StoreAccessTokenAsync()` (KV-backed) |
| Function | `PostLink.cs` — uses `IUserOAuthTokenManager.GetByUserAndPlatformAsync()` for the access token |
| Function | `NotifyExpiringTokens.cs` — queries `UserOAuthTokens` expiry window; sends 7-day and 1-day emails |

**Key observation**: `UserPublisherLinkedInSettings.HasAccessToken` and the KV path in `IUserPublisherLinkedInSettingsManager` are **dead code** — `PostLink.cs` fetches the token from `UserOAuthTokens`, not from KV. These two mechanisms were likely built at different times (KV approach first, SQL table added later by issue #777) and were never reconciled.

#### 1.2 Facebook

| Layer | What exists |
|-------|-------------|
| SQL | `UserPublisherFacebookSettings` table: `IsEnabled`, `PageId`, `AppId`, plus boolean flags `HasPageAccessToken`, `HasAppSecret`, `HasClientToken`, `HasShortLivedAccessToken`, `HasLongLivedAccessToken` |
| KV (per-user) | `publisher-{ownerOid}-facebook-page-access-token`, `-long-lived-access-token`, `-short-lived-access-token` |
| KV (per-user) | `publisher-{ownerOid}-facebook-app-secret`, `-client-token` |
| KV (global) | `jjg-net-facebook-{long-lived|page}-access-token` — written by `Facebook/RefreshTokens.cs` |
| Manager | `IUserPublisherFacebookSettingsManager` — settings + all token get/store methods (KV-backed, per-user) |
| Function | `PostPageStatus.cs` — correctly uses `IUserPublisherFacebookSettingsManager.GetPageAccessTokenAsync()` (per-user KV) |
| Function | `RefreshTokens.cs` — uses `IFacebookApplicationSettings` (global app config), refreshes global tokens, saves to global KV key via `IKeyVault` directly |
| DB | `TokenRefreshes` table — used only by `Facebook/RefreshTokens.cs` to track last-refresh timestamps |

**Key observation**: `PostPageStatus` and `RefreshTokens` are **working against different token stores**. `PostPageStatus` reads per-user KV secrets. `RefreshTokens` refreshes global KV secrets. The tokens that `RefreshTokens` refreshes are **never read by PostPageStatus**. The Facebook token refresh is functionally disconnected from the publisher pipeline.

#### 1.3 Collector/Publisher Settings Pattern (the reference model)

- Each user's platform settings live in a dedicated SQL table (`UserPublisher{Platform}Settings`) keyed by `CreatedByEntraOid`.
- The SQL row holds metadata and boolean `Has*` flags. Actual secrets live in KV under `{ownerType}-{ownerOid}-{platform}-{settingName}`.
- A manager class (`UserPublisher{Platform}SettingsManager`) wraps both SQL datastore and KV. Functions resolve credentials at dequeue time: `OwnerEntraOid` → manager → credentials.
- This is working correctly for Twitter, Bluesky, and Facebook (at the PostPageStatus level).

---

### 2. Gap Analysis

| Problem | Root Cause | Impact |
|---------|-----------|--------|
| LinkedIn has two parallel token stores (SQL `UserOAuthTokens` + KV via `HasAccessToken`) | Both were built without reconciliation; the KV path was never wired into the publisher pipeline | Dead code in `IUserPublisherLinkedInSettingsManager`; false signal (`HasAccessToken = true` in DB but ignored at publish time) |
| Facebook `RefreshTokens` uses global app-level config, not per-user settings | Was built before per-user pattern existed; never updated when `UserPublisherFacebookSettings` was introduced | Tokens refreshed by the Function are NEVER used by `PostPageStatus`; the actual per-user tokens in KV never get refreshed |
| Facebook has no expiry tracking with user notification | `UserOAuthTokens` only populated for LinkedIn; no `NotifyExpiringTokens` for Facebook | If Facebook token expires and auto-refresh fails, the user has no warning |
| `TokenRefreshes` table is a global log with no user scoping | Built for the old global refresh approach | Redundant once Facebook migrates to per-user `UserOAuthTokens` |

---

### 3. Recommendation

#### 3.1 Core Principle

**`UserOAuthTokens` is the authoritative store for per-user OAuth access tokens with expiry.** App-level credentials (AppSecret, ClientSecret, AppId, PageId) belong in KV via the Settings Manager pattern.

The two patterns serve distinct roles:
- `UserOAuthTokens` — tokens obtained via an OAuth flow or refreshed automatically; all have expiry dates that the system must track.
- Settings Manager (KV + SQL flags) — app credentials that don't expire like OAuth tokens (secrets, IDs, keys).

#### 3.2 LinkedIn — Clean Up the Dual Track

**Keep** `UserOAuthTokens` as the canonical OAuth token store. It is correct and already wired into `PostLink.cs` and `NotifyExpiringTokens.cs`.

**Remove** the dead KV-backed token path from the LinkedIn settings manager:

| Change | File(s) |
|--------|---------|
| Remove `HasAccessToken`, `GetAccessTokenAsync`, `StoreAccessTokenAsync` | `IUserPublisherLinkedInSettingsManager.cs` |
| Remove corresponding implementation | `UserPublisherLinkedInSettingsManager.cs` |
| Remove `HasAccessToken` property | `Domain/Models/UserPublisherLinkedInSettings.cs` |
| Remove `HasAccessToken` property + EF mapping | `Data.Sql/Models/UserPublisherLinkedInSettings.cs` + `MappingProfiles/` |
| SQL migration: drop `HasAccessToken` column | `scripts/database/migrations/` (new script) |
| Update tests | `Managers.Tests/UserPublisherLinkedInSettingsManagerTests.cs`, `Data.Sql.Tests/UserPublisherLinkedInSettingsDataStoreTests.cs` |

`PostLink.cs`, `NotifyExpiringTokens.cs`, `UserOAuthTokenManager.cs`, and all `UserOAuthToken` datastore and domain classes remain unchanged.

#### 3.3 Facebook — Migrate to Per-User UserOAuthTokens

**Phase A — Wire `PostPageStatus` to `UserOAuthTokens`**

`PostPageStatus.cs` currently calls `facebookSettingsManager.GetPageAccessTokenAsync()`. Change it to call `IUserOAuthTokenManager.GetByUserAndPlatformAsync(ownerOid, SocialMediaPlatformIds.Facebook)` — exactly matching how `PostLink.cs` resolves LinkedIn tokens. The `AuthorId` (`PageId`) remains in the settings manager.

**Phase B — Rewrite `RefreshTokens.cs` to per-user**

The function must stop using `IFacebookApplicationSettings` for token values and instead:

1. Query `IUserOAuthTokenManager` (or the underlying data store) for all Facebook rows where the access token will expire within N days (or query all enabled Facebook users via their settings).
2. For each user, retrieve their app credentials (`AppId`, `AppSecret`) via `IUserPublisherFacebookSettingsManager.GetAppSecretAsync()`.
3. Call `facebookManager.RefreshToken(currentToken)` with the per-user token value from `UserOAuthTokens`.
4. Write the new token back via `IUserOAuthTokenManager.StoreOAuthCallbackTokenAsync()`.

**Phase C — Clean up Facebook settings model**

Remove the token-specific boolean flags from `UserPublisherFacebookSettings` — those tokens now live in `UserOAuthTokens`:

| Remove | Keep |
|--------|------|
| `HasPageAccessToken`, `HasShortLivedAccessToken`, `HasLongLivedAccessToken` | `HasAppSecret`, `HasClientToken` |
| `GetPageAccessTokenAsync`, `StorePageAccessTokenAsync` | `GetAppSecretAsync`, `StoreAppSecretAsync` |
| `GetShortLivedAccessTokenAsync`, `StoreShortLivedAccessTokenAsync` | `GetClientTokenAsync`, `StoreClientTokenAsync` |
| `GetLongLivedAccessTokenAsync`, `StoreLongLivedAccessTokenAsync` | (keep all metadata: `PageId`, `AppId`, `IsEnabled`) |

Add `IUserOAuthTokenManager` parameter to the `PostPageStatus` constructor; remove `IFacebookApplicationSettings` from `RefreshTokens` constructor.

**Phase D — Add `IUserOAuthTokenDataStore.GetExpiringByPlatformAsync()`**

The `RefreshTokens` function needs to enumerate all users for a given platform. Extend `IUserOAuthTokenDataStore` with:

```csharp
Task<List<UserOAuthToken>> GetExpiringByPlatformAsync(
    int platformId, DateTimeOffset threshold, CancellationToken cancellationToken = default);
```

This method mirrors `GetExpiringAsync` but scopes by `SocialMediaPlatformId`.

**Phase E — Retire `TokenRefreshes` table (deferred)**

The `TokenRefreshes` table and `ITokenRefreshManager` serve only the old global-refresh pattern. Once Facebook is migrated, no function writes to `TokenRefreshes`. Mark it for deletion in a future cleanup pass — do not delete it in the same PR as the migration to avoid risk.

#### 3.4 Optional Enhancement — Facebook Expiry Notifications

Since `UserOAuthTokens` now tracks Facebook token expiry, a `NotifyExpiringTokens` function for Facebook (modelled exactly on the LinkedIn one) can be added later. This is a future enhancement, not part of the immediate fix.

---

### 4. Migration Considerations

- **LinkedIn SQL migration**: The `HasAccessToken` column should be dropped via a new idempotent migration script in `scripts/database/migrations/`. Add `IF EXISTS (SELECT 1 FROM sys.columns WHERE ...)` guard.
- **Facebook SQL migration**: Same approach for `HasPageAccessToken`, `HasShortLivedAccessToken`, `HasLongLivedAccessToken` columns.
- **Data migration**: Existing Facebook per-user tokens currently in KV (`publisher-{ownerOid}-facebook-page-access-token`, etc.) must be migrated into `UserOAuthTokens` rows. This requires a one-time script or admin utility to: read each enabled Facebook user's `PageAccessToken` from KV, parse the expiry, and insert into `UserOAuthTokens`. **This is a manual production step — a GitHub issue with `squad:Joe` label must accompany the PR.**
- **LinkedIn KV secret cleanup**: Per-user LinkedIn access tokens stored in KV under `publisher-{ownerOid}-linkedin-access-token` were never used by the pipeline. They can be deleted from KV as part of cleanup (also manual, tracked via issue).
- **Global KV secret `jjg-net-facebook-*-access-token`**: Once per-user tokens are in `UserOAuthTokens`, the global KV secret is no longer needed. Remove via manual cleanup (tracked via issue).

---

### 5. Implementation Order

1. **LinkedIn cleanup** (lower risk, no functional change) — PR 1
2. **Facebook Phase A** — wire `PostPageStatus` to `UserOAuthTokens` — PR 2
3. **Facebook Phase B+C** — rewrite `RefreshTokens`, clean settings manager — PR 3 (depends on data migration issue)
4. **Facebook Phase D** — extend `IUserOAuthTokenDataStore.GetExpiringByPlatformAsync()` — included in PR 3
5. **`TokenRefreshes` retirement** — future cleanup PR

---

### 6. Files of Interest

| File | Role |
|------|------|
| `scripts/database/table-create.sql` (lines 404–460) | LinkedIn and Facebook settings tables |
| `scripts/database/table-create.sql` (lines 480–509) | `UserOAuthTokens` table |
| `src/Domain/Models/UserPublisherLinkedInSettings.cs` | `HasAccessToken` to remove |
| `src/Domain/Models/UserPublisherFacebookSettings.cs` | Token flags to remove |
| `src/Domain/Interfaces/IUserPublisherLinkedInSettingsManager.cs` | Methods to remove |
| `src/Domain/Interfaces/IUserPublisherFacebookSettingsManager.cs` | Methods to remove |
| `src/Domain/Interfaces/IUserOAuthTokenDataStore.cs` | Add `GetExpiringByPlatformAsync` |
| `src/Managers/UserPublisherLinkedInSettingsManager.cs` | Implementation to prune |
| `src/Managers/UserPublisherFacebookSettingsManager.cs` | Implementation to prune |
| `src/Managers/UserOAuthTokenManager.cs` | Stays as-is |
| `src/Functions/Facebook/PostPageStatus.cs` | Switch token source to `IUserOAuthTokenManager` |
| `src/Functions/Facebook/RefreshTokens.cs` | Full rewrite to per-user |
| `src/Functions/LinkedIn/PostLink.cs` | No change |
| `src/Functions/LinkedIn/NotifyExpiringTokens.cs` | No change |

---
## # Phase 6 Complete — #980 Publisher Architecture Refactor

**Date:** 2026-05-15  
**Author:** Neo  
**Branch:** `issue-980-publisher-architecture-refactor`  
**Status:** COMPLETE
---
## Phase 6: Delete Obsolete Platform Queue DTOs

### Files Deleted

**Domain model DTOs (zero type references in production code):**

| File | Reason |
|------|--------|
| `Domain/Models/Messages/FacebookPostStatus.cs` | Replaced by `SocialMediaPublishRequest`; no callers |
| `Domain/Models/Messages/LinkedInPostLink.cs` | Replaced by `SocialMediaPublishRequest`; no callers |
| `Domain/Models/Messages/TwitterTweetMessage.cs` | Replaced by `SocialMediaPublishRequest`; no callers |
| `Domain/Models/Messages/LinkedInPostImage.cs` | Replaced by `SocialMediaPublishRequest`; no callers |
| `Domain/Models/Messages/LinkedInPostText.cs` | Replaced by `SocialMediaPublishRequest`; no callers |

**Orphaned LinkedIn functions (queues no longer fed by any Process* function):**

| File | Reason |
|------|--------|
| `Functions/LinkedIn/PostImage.cs` | Listened on `linkedin-post-image`; no Process* function enqueued to it; functionality fully covered by `PostLink.cs` + `LinkedInManager.PublishAsync()` which handles `ImageUrl` |
| `Functions/LinkedIn/PostText.cs` | Listened on `linkedin-post-text`; no Process* function enqueued to it; functionality covered by `PostLink.cs` |
| `Functions.Tests/LinkedIn/PostImageTests.cs` | Tests for deleted function |
| `Functions.Tests/LinkedIn/PostTextTests.cs` | Tests for deleted function |

**Domain/Models/Messages/ now contains only `Email.cs` (as intended).**

### Constants Cleaned Up

Removed from `Domain/Constants/Queues.cs`:
- `LinkedInPostText = "linkedin-post-text"`
- `LinkedInPostImage = "linkedin-post-image"`

Removed from `Domain/Constants/ConfigurationFunctionNames.cs`:
- `LinkedInPostText`
- `LinkedInPostImage`

Removed from `Domain/Constants/Metrics.cs`:
- `LinkedInPostText`
- `LinkedInPostImage`

All remaining constants (`LinkedInPostLink`, `FacebookPostStatusToPage`, etc.) are still active — they name the queue channels used by the new `SocialMediaPublishRequest` pipeline.

### Remaining References (All Valid)

References like `Queues.LinkedInPostLink`, `Queues.FacebookPostStatusToPage`, `ConfigurationFunctionNames.LinkedInPostLink`, and `Metrics.LinkedInPostLink` remain — they are **queue channel names**, not DTO type references. The queue names did not change; only the message payload type changed (from old DTOs to `SocialMediaPublishRequest`).

### Build and Test Results

- **Build:** 0 errors, 0 warnings
- **Tests:** 1,222 passed, 0 failed (41 skipped — integration tests requiring live services)
- **Commit:** `af705129` — `refactor(#980): Phase 6 — delete obsolete platform queue DTOs`
---
## Overall #980 Refactor Summary

### What the Codebase Looked Like Before

Each social media platform had:
- **A platform-specific queue DTO** (`FacebookPostStatus`, `TwitterTweetMessage`, `LinkedInPostLink`, `LinkedInPostImage`, `LinkedInPostText`) used as the queue message payload
- **Hard-coded credentials** embedded in the DTO (access tokens, author IDs passed through the queue)
- **Process* functions** that composed the full message text inline using hard-coded templates
- **Send* functions** that dequeued the platform-specific DTO and called platform-specific manager methods directly
- **Manager methods** that took platform-specific parameters (text, accessToken, authorId, etc.)

### What the Codebase Looks Like Now

**One unified queue message type:** `SocialMediaPublishRequest` is the queue payload across ALL 4 platforms.

**Pipeline:**
```
EventGrid event
  → Process* Function (composes message text via IPostComposer + per-user IMessageTemplateLookup)
  → enqueues SocialMediaPublishRequest{Text, Title, LinkUrl, Hashtags, OwnerEntraOid, ...}
    to platform queue
  → Send* Function dequeues SocialMediaPublishRequest
  → resolves per-user credentials via Settings Manager (KeyVault-backed)
  → hydrates request.AccessToken, request.AuthorId
  → calls manager.PublishAsync(request)
  → platform manager dispatches internally based on request fields
```

**Key architectural improvements:**
1. **No credentials in the queue** — `OwnerEntraOid` travels instead; credentials resolved at send time from per-user settings
2. **No hard-coded templates** — `IMessageTemplateLookup` fetches per-user, per-platform templates from the database
3. **Single `ISocialMediaPublisher` interface** — all platform managers implement `PublishAsync(SocialMediaPublishRequest)`
4. **`PostComposer`** handles Handlebars template composition centrally
5. **Per-user Twitter auth** — Twitter credentials (consumerKey, consumerSecret, accessToken, accessTokenSecret) all per-user from `IUserPublisherTwitterSettingsManager`
6. **LinkedIn image/text/link dispatch unified** — `LinkedInManager.PublishAsync()` inspects `request.ImageBytes`/`request.ImageUrl`/`request.LinkUrl` to pick the right LinkedIn API call

### Phase Summary

| Phase | Change |
|-------|--------|
| 1 | Deleted dead plural `*PublisherSettings`; created `IPostComposer`, `PostComposer`, `IMessageTemplateLookup` |
| 2 | Extended `SocialMediaPublishRequest`; created `MessageTemplateLookup`; DI registrations |
| 3 | User-scoped `GetAsync` on `IMessageTemplateDataStore`; migrated all 20 Process* Functions |
| 4 | Stripped composition from all 4 publisher managers; Twitter per-user auth |
| 5 | Send* Functions dequeue `SocialMediaPublishRequest`; all managers use `PublishAsync(request)` |
| 6 | Deleted all obsolete platform-specific queue DTOs; removed dead functions and constants |

### Branch State

Branch `issue-980-publisher-architecture-refactor` is **ready for review**.  
Pending PRs #978 and #979 should merge first if they affect shared infrastructure.
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
---
### 2026-05-19T13:33:25: User directive
**By:** Joe (via Copilot)
**What:** All dates displayed in the web application must be shown in the local user's time. Use the `local-time` tag helper for every date/datetime value rendered in Razor views — no exceptions.
**Why:** User request — captured for team memory
---
### 2026-05-18T16:51:26-07:00: User directive — Composers project
**By:** Joseph Guadagno (via Copilot)
**What:** Extract composition logic (`PostComposer` and future composers) into a dedicated class library `JosephGuadagno.Broadcasting.Composers`. This project will house all message/content composition concerns — social post composition today, email composition in the future. The pattern for email composition should mirror the pattern used for social post composition.
**Why:** User request — `PostComposer` is a utility with no database dependencies, making it a natural fit for a standalone composable library. Separating it from `JosephGuadagno.Broadcasting.Managers` clarifies the layering: Managers = business logic + data orchestration; Composers = pure content transformation. The Web project can reference `Composers` directly (no DI boundary violation) since it contains no data access or Manager-level concerns.
**Implications:**
- New project: `src/JosephGuadagno.Broadcasting.Composers/JosephGuadagno.Broadcasting.Composers.csproj`
- `PostComposer` moves from `JosephGuadagno.Broadcasting.Managers` to `JosephGuadagno.Broadcasting.Composers`
- `IPostComposer` interface moves from `JosephGuadagno.Broadcasting.Domain.Interfaces` to either `JosephGuadagno.Broadcasting.Domain.Interfaces` (unchanged) or the new Composers project
- All consumers (Functions, API, Web) reference the new project
- Web project's DI registration of `IPostComposer` is valid once it comes from Composers (not Managers)
- Future: `EmailComposer` follows the same pattern in the same project
---
### 2026-05-18T17:32:19-07:00: User directive — No hard-coded platform icons
**By:** Joseph Guadagno (via Copilot)
**What:** Never hard-code social media platform icons (Bootstrap icon class strings) in Razor views or C# code. Always source them from the `SocialMediaPlatforms` database table via the appropriate service.
**Why:** User request — platform icons are stored in the DB and that is the authoritative source. Hard-coding creates drift if icons are changed in the DB.
**Implication:** `SetupStatus` model needs a `PlatformSummary` record (Name + Icon) populated by `SetupService` via `ISocialMediaPlatformService`. Views bind to `Model.ConfiguredPublisherSummaries` rather than doing an inline dictionary lookup.
---
### 2026-05-18T18:31: User directive — user-facing pages are always user-scoped

**By:** Joseph Guadagno (via Copilot)

**What:**
All user-facing pages (MessageTemplates, SocialMediaPlatforms, Publishers, Collectors, etc.) must
only show data belonging to the currently authenticated user. Admin access to data across all users
belongs exclusively in the dedicated Admin section (issue #975), not in user-facing controllers or
service methods.

**Implication for the API:**
Any API controller action that currently branches on `User.IsSiteAdministrator()` to return
all-users data on a user-facing endpoint must be refactored. The user-facing endpoint always
returns the requesting user's data. Admin endpoints are separate routes under the Admin section.

**Why:** User request — captured for team memory
---
### 2026-05-18T16:46:48-07:00: User directive
**By:** Joseph Guadagno (via Copilot)
**What:** The Web project (`JosephGuadagno.Broadcasting.Web`) must NEVER inject or register Manager classes directly. All business logic access must go through `IXxxService` HTTP-client wrapper interfaces defined in `JosephGuadagno.Broadcasting.Web.Interfaces`. This is a hard architectural boundary — not a style preference. Any PR that registers a Manager in Web's `Program.cs` or injects a Manager interface into a Web controller/service must be rejected.
**Why:** User request — the rule existed but was not captured as a formal directive. Neo's agent missed it in a prior session, allowing `IMessageTemplateLookup` and `IPostComposer` to be incorrectly registered in the Web DI container. Captured so all agents carry this constraint going forward.
---
### 2026-05-19T10:59:48: User directive
**By:** Joseph Guadagno (via Copilot)
**What:** Do NOT suppress MSAL/IdentityModel debug log output in DEBUG builds. The verbose logging is intentionally kept visible locally so that issues like L1 cache misses remain detectable. The three MinimumLevel.Override calls added for Microsoft.Identity, Microsoft.IdentityModel, and MSAL in LoggingExtensions.cs should be reverted.
**Why:** User preference — "if we had the MSAL excluded, we might not have found the issue"
---
# Neo — Issue #978 Complete: Post-Approval User Onboarding Setup Flow

**Date**: 2026-05-18
**Branch**: `issue-978-user-onboarding-flow`
**PR**: https://github.com/jguadagno/jjgnet-broadcast/pull/982

## What was built

A post-approval user onboarding checklist that guides newly approved users through the three areas required to start broadcasting: Collectors, Publishers, and Message Templates.

### Components

| File | Purpose |
|------|---------|
| `Models/SetupStatus.cs` | Model with per-area booleans and `IsComplete` computed property |
| `Interfaces/ISetupService.cs` | Single-method interface: `GetSetupStatusAsync(bool forceRefresh)` |
| `Services/SetupService.cs` | Queries 8 existing Web services; caches result per-user (5 min IMemoryCache) |
| `Controllers/SetupController.cs` | `/Setup` route, `RequireContributor` policy, always bypasses cache |
| `ViewComponents/SetupStatusViewComponent.cs` | Nav badge component; reads cached status |
| `Views/Setup/Index.cshtml` | 3-card checklist with per-step status and direct links to settings pages |
| `Views/Shared/Components/SetupStatus/Default.cshtml` | Nav gear+warning badge; renders only when `!IsComplete` |

Modified: `Program.cs` (service registration), `Views/Shared/_Layout.cshtml` (nav component invoke).

## Design decisions

### Checklist over wizard
Issue #978 describes a checklist-style page with setup status cards. A multi-step wizard was considered but rejected because the three areas are independent — users may configure them in any order and come back at different times. The checklist links to existing settings pages, keeping code and maintenance surface minimal.

### IMemoryCache with 5-minute TTL
Each setup area makes API calls (collectors: 3 calls, publishers: 4 calls, templates: 1 call = 8 total). Rechecking all 8 on every page render (including the nav badge) would degrade performance. A 5-minute per-user cache in `IMemoryCache` (already a singleton in the app) balances freshness against overhead.

`SetupController.Index` always calls `forceRefresh: true` so the checklist page always reflects the current state after a user saves settings in another tab.

### No auto-redirect post-approval
UserApprovalMiddleware gates authenticated routes. Once a user is approved, they land on the app. The nav badge signals incomplete setup but does not force redirect — users may explore before finishing. Forcing a redirect would be disruptive and was not requested in the issue.

### Templates completion logic
`HasMessageTemplates = configuredPublishers.Count == 0 || missingTemplatePlatforms.Count == 0`

If no publishers are enabled, message templates are not yet actionable; the Templates step shows "Complete" until at least one publisher is configured. This avoids a confusing "incomplete" state when a brand new user has touched nothing yet.

### `IHttpContextAccessor` registration
`AddHttpContextAccessor()` was not previously registered explicitly in `Program.cs`. It is required by `SetupService` to resolve the current user's OID for the cache key. Added at the top of the service registration block.

## Test results

- Build: 0 errors, 0 new warnings
- Tests: 1246 passed, 5 pre-existing failures (`MessageTemplateDataStoreTests` EF Core entity-tracking, present on `main` before this branch), 41 skipped
---
# ADR: Intersection-Based Template Completeness Check (#978)

**Date:** 2026-05-18
**Author:** Neo (Lead)
**Branch:** issue-978-user-onboarding-flow
**Status:** Implemented

## Context

The original `SetupService.BuildSetupStatusAsync()` checked template completeness by asking:
*"Does every configured publisher platform have at least one template (of any message type)?"*

This was too coarse. A user with a Twitter publisher and only a `RandomPost` template would be
marked complete, even though they had no `NewSyndicationFeedItem` template for their RSS feed
collector. Conversely, a user with only a SyndicationFeed collector was incorrectly required to
have YouTube and SpeakingEngagement templates for publishers they'd enabled, even if they had
zero YouTube/engagement collectors.

## Decision

Replace the per-platform check with an **intersection-based check**:

> For each *(publisher × collector-type)* combination the user actually has configured,
> a message template with that exact `(Platform, MessageType)` pair must exist.

### Required-pair construction

1. Determine which collector types the user has configured:
   - `SyndicationFeedSource` → `MessageTypes.NewSyndicationFeedItem`
   - `YouTubeChannel` → `MessageTypes.NewYouTubeItem`
   - `SpeakingEngagement` → `MessageTypes.NewSpeakingEngagement`

2. Determine which publishers are enabled (Bluesky, Twitter, LinkedIn, Facebook).

3. Cross-product: `requiredPairs = configuredPublishers × collectorTypes`

4. Fetch all user templates via `IMessageTemplateService.GetAllAsync(MaxPageSize)`.

5. `missingTemplatePairs = requiredPairs ∖ existingTemplates`

6. `HasMessageTemplates = publishers.Empty || collectors.Empty || missingPairs.Empty`

### Example

| Collectors | Publishers | Required templates |
|---|---|---|
| SyndicationFeed | Twitter, Bluesky | Twitter×NewSyndicationFeedItem, Bluesky×NewSyndicationFeedItem |
| SyndicationFeed + YouTube | Twitter | Twitter×NewSyndicationFeedItem, Twitter×NewYouTubeItem |
| (none) | Twitter | complete (vacuously) |
| SyndicationFeed | (none) | complete (vacuously) |

## Files Changed

| File | Change |
|---|---|
| `Models/MissingTemplateKey.cs` | New `record MissingTemplateKey(string Platform, string MessageType)` |
| `Models/SetupStatus.cs` | Added `ConfiguredCollectorTypes`, `MissingTemplatePairs`; updated `HasMessageTemplates` doc |
| `Services/SetupService.cs` | Rewrote template check to intersection logic |
| `Views/Setup/Index.cshtml` | Shows missing pairs as `Platform × MessageType` list items |

## Alternatives Rejected

**Per-platform check (previous):** Too coarse — passes when a publisher has a `RandomPost`
template but no collector-specific template. Fails when a user lacks collector types that
aren't relevant to their setup.

**Individual API calls per pair:** Would replace the single `GetAllAsync` call with N×M API
calls. Rejected — violates sequential-await convention and would cause overhead for users
with many publishers/collectors.

## Verification

- `dotnet build .\src\ --no-restore --configuration Release` → Build succeeded, 0 errors
- `dotnet test .\src\ --no-build --configuration Release --filter "FullyQualifiedName!~SyndicationFeedReader"` → All tests passed
---
# Decision: Default Message Templates (Issue #979)

**Date:** 2026-05-18  
**Branch:** issue-979-default-message-templates  
**PR:** #984  
**Production migration:** Issue #983

## Decision

Add system-level default Scriban templates (empty-string `CreatedByEntraOid` = sentinel) that users can adopt with one click. No template is silently forced on existing users.

## Key choices

| Choice | Rationale |
|--------|-----------|
| Empty string `''` as system sentinel (not NULL, not a UUID) | NULL requires nullable FK logic; a special UUID would need constant management. Empty string is simple, safe, and self-describing. |
| PK becomes triplet `(SocialMediaPlatformId, MessageType, CreatedByEntraOid)` | Allows both a system default and a per-user override to coexist for the same platform+type pair. |
| `GetAsync(2-arg)` delegates to `GetAsync(3-arg, SystemOwnerEntraOid)` | Zero-change backward compatibility for Functions/publishers that always want the system default. |
| Remove Forbid on non-owner GET/UPDATE | With the 3-arg lookup, the user can only ever retrieve or mutate their own row; a missing row returns 404, not 403. Simpler and correct. |
| "Available Defaults" computed in Web controller (not API) | Minimises API round-trips; the gap between user templates and system defaults is a presentation concern. |
| `data-seed.sql` uses IF NOT EXISTS guards | Idempotent: AppHost replays creation script for fresh environments without duplicate-key errors. |

## What was NOT done

- `IMessageTemplateLookup` composite interface (mentioned in issue for PostComposer) — deferred, out of scope.
- Automatic migration of existing user rows — old rows keep their existing (possibly NULL) `CreatedByEntraOid`; production migration (#983) handles backfill.
---
# Decision: MsalDistributedTokenCacheAdapterOptions.AbsoluteExpirationRelativeToNow Affects L2 (SQL) Cache

**Date:** 2026-05-19  
**Author:** Neo  
**Status:** PENDING APPROVAL — no code changes made yet  
**Related commit:** `3af53e7f`
---
## Context

When configuring `MsalDistributedTokenCacheAdapterOptions`, setting `AbsoluteExpirationRelativeToNow` does **not** only affect the L1 (in-memory) cache. It is also passed verbatim to `IDistributedCache.SetAsync()` as `DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow`, setting the L2/SQL `TokenCache` entry expiration.

This was discovered by reading the Microsoft.Identity.Web source (`MsalDistributedTokenCacheAdapter.WriteCacheBytesAsync`):

```csharp
DistributedCacheEntryOptions distributedCacheEntryOptions = new DistributedCacheEntryOptions
{
    AbsoluteExpiration = cacheExpiry,
    AbsoluteExpirationRelativeToNow = _distributedCacheOptions.AbsoluteExpirationRelativeToNow, // applied to L2
    SlidingExpiration = _distributedCacheOptions.SlidingExpiration,
};
await _distributedCache.SetAsync(cacheKey, bytes, distributedCacheEntryOptions, ...);
```

## The Bug

Commit `3af53e7f` set `AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)` intending to fix L1 TTL derivation. Side effect: SQL `TokenCache` entries now expire 15 minutes after the last write, overriding `DefaultSlidingExpiration = TimeSpan.FromDays(14)`. Users must re-login after any 15-minute idle period or app restart.

## Decision

**Do not set `AbsoluteExpirationRelativeToNow` to a short value** (e.g., minutes) in `MsalDistributedTokenCacheAdapterOptions` without understanding its L2 side-effect. L2 lifetime is controlled by `AddDistributedSqlServerCache(options => options.DefaultSlidingExpiration = ...)`.

If a short L1 TTL is needed independently of L2, the `L1ExpirationTimeRatio` property on `MsalDistributedTokenCacheAdapterOptions` provides this — but it is `internal` in the current library version (4.9.0) and not publicly settable.

## Recommended Configuration

```csharp
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.DisableL1Cache = false;
    // Do NOT set AbsoluteExpirationRelativeToNow here — it overrides L2/SQL lifetime.
    // L2 lifetime is governed by AddDistributedSqlServerCache DefaultSlidingExpiration (14 days).
});
```

## Skill Update Needed

The `msal-cache-handling` skill SKILL.md documents `AbsoluteExpirationRelativeToNow` as controlling "L1 TTL" and recommends `TimeSpan.FromMinutes(15)`. This is **incorrect for L2**: the same value propagates to SQL. The skill should be updated to warn about this behaviour and omit the property (or note the L2 risk).
---
# Decision: CollectorIcons static constants class for Web project

**Date:** 2026-05-18  
**Author:** Switch (Frontend Engineer)  
**Requested by:** Joe

## Context

Collector type icons (RSS, YouTube, Speaking Engagement, and the overall Collectors section) were hard-coded as Bootstrap icon class strings scattered across multiple Razor views. Joe directed that these values be centralised so they stay consistent and are easy to update from one place.

## Decision

A `CollectorIcons` static constants class was created at:

```
src/JosephGuadagno.Broadcasting.Web/Constants/CollectorIcons.cs
```

### Structure

| Member | Value |
|--------|-------|
| `CollectorIcons.Collection` | `bi-collection` |
| `CollectorIcons.FeedSource.Icon` | `bi-rss` |
| `CollectorIcons.FeedSource.Label` | `RSS / Atom Feed` |
| `CollectorIcons.FeedSource.MessageType` | `NewSyndicationFeedItem` |
| `CollectorIcons.YouTubeChannel.Icon` | `bi-youtube` |
| `CollectorIcons.YouTubeChannel.Label` | `YouTube Channel` |
| `CollectorIcons.YouTubeChannel.MessageType` | `NewYouTubeItem` |
| `CollectorIcons.SpeakingEngagement.Icon` | `bi-mic-fill` |
| `CollectorIcons.SpeakingEngagement.Label` | `Speaking Engagement` |
| `CollectorIcons.SpeakingEngagement.MessageType` | `NewSpeakingEngagement` |
| `CollectorIcons.ByMessageType` | `IReadOnlyDictionary<string, (Icon, Label)>` for Razor view lookups |

## Convention

- **Never hard-code `bi-rss`, `bi-youtube`, or `bi-mic-fill`** in Web Razor views. Always reference `CollectorIcons.*`.
- The `ByMessageType` dictionary is the canonical source for views that map a string message type to a display badge (e.g., Setup/Index).
- The namespace `JosephGuadagno.Broadcasting.Web.Constants` is registered in `_ViewImports.cshtml` for all regular views. For `_Layout.cshtml` (which does not inherit ViewImports), the `@using` directive is added at the top of the file.

## Files Updated

- `src/JosephGuadagno.Broadcasting.Web/Constants/CollectorIcons.cs` *(created)*
- `src/JosephGuadagno.Broadcasting.Web/Views/_ViewImports.cshtml`
- `src/JosephGuadagno.Broadcasting.Web/Views/Setup/Index.cshtml`
- `src/JosephGuadagno.Broadcasting.Web/Views/CollectorSettings/Index.cshtml`
- `src/JosephGuadagno.Broadcasting.Web/Views/CollectorFeedSources/Index.cshtml`
- `src/JosephGuadagno.Broadcasting.Web/Views/CollectorYouTubeChannels/Index.cshtml`
- `src/JosephGuadagno.Broadcasting.Web/Views/Shared/_Layout.cshtml`
- `src/JosephGuadagno.Broadcasting.Web/Views/Shared/_LoginPartial.cshtml`
- `src/JosephGuadagno.Broadcasting.Web/Views/SyndicationFeedItems/Index.cshtml`
- `src/JosephGuadagno.Broadcasting.Web/Views/YouTubeItems/Index.cshtml`
---
# Decision: local-time tag helper is mandatory for all date displays

**Date:** 2026-05-19  
**Agent:** Switch  
**Requested by:** Joe  

## Directive applied

All raw `.ToString()` date formatting in Razor views has been replaced with the `<local-time>` tag helper across the entire Web application. This directive is now recorded and enforced going forward.

## Views updated (commit 536db628)

| View | Fields fixed |
|------|-------------|
| `CollectorFeedSources/Details.cshtml` | `CreatedOn`, `LastUpdatedOn` |
| `CollectorFeedSources/Delete.cshtml` | `CreatedOn` |
| `CollectorFeedSources/Index.cshtml` | `item.CreatedOn` |
| `CollectorYouTubeChannels/Details.cshtml` | `CreatedOn`, `LastUpdatedOn` |
| `CollectorYouTubeChannels/Delete.cshtml` | `CreatedOn` |
| `CollectorYouTubeChannels/Index.cshtml` | `item.CreatedOn` |
| `CollectorSpeakingEngagements/Details.cshtml` | `CreatedOn`, `LastUpdatedOn` |
| `CollectorSpeakingEngagements/Delete.cshtml` | `CreatedOn` |
| `CollectorSpeakingEngagements/Index.cshtml` | `item.CreatedOn` |
| `SyndicationFeedItems/Details.cshtml` | `PublicationDate`, `AddedOn`, `LastUpdatedOn` |
| `SyndicationFeedItems/Delete.cshtml` | `PublicationDate` |
| `YouTubeItems/Details.cshtml` | `PublicationDate`, `AddedOn`, `LastUpdatedOn` |
| `YouTubeItems/Delete.cshtml` | `PublicationDate` |

## Views verified as already compliant (no changes needed)

- `SiteAdmin/Users.cshtml`
- `Schedules/Index.cshtml`, `Orphaned.cshtml`, `Unsent.cshtml`, `Upcoming.cshtml`
- `Engagements/Index.cshtml`, `Edit.cshtml`
- `YouTubeItems/Index.cshtml`
- `SyndicationFeedItems/Index.cshtml`

## Rule going forward

No new Razor view may use `.ToString("F")`, `.ToString("g")`, `.ToString("f")`, or any other date format string for display. Always use:

```razor
<local-time value="@Model.SomeDate" />
```

Use `date-only="true"` only for pure calendar-date fields with no time component.
---
# Decision: Scope MSAL L1 Cache Pin to Release Builds Only

**Date:** 2026-05-19  
**Author:** Trinity  
**Status:** Accepted (Joe confirmed)

## Context

`MsalDistributedTokenCacheAdapterOptions.AbsoluteExpirationRelativeToNow` was added to `Web/Program.cs` to prevent the L1 (in-memory) token cache from evicting near-expiry tokens and triggering ~1.75s SQL reads on every request. However, this option applies to **both** the L1 and L2 (SQL distributed) cache layers. Setting it to 15 minutes unconditionally overrides the SQL store's 14-day sliding expiration, causing forced re-login after 15 minutes of inactivity or on every app restart.

## Decision

Wrap `options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);` in `#if !DEBUG` / `#endif`:

```csharp
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.DisableL1Cache = false;
#if !DEBUG
    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
#endif
});
```

## Consequences

- **DEBUG (local dev):** `AbsoluteExpirationRelativeToNow` is not set → SQL cache retains 14-day sliding expiry → no forced re-login during development.
- **Release (production):** `AbsoluteExpirationRelativeToNow = 15 min` remains → L1 cache pin prevents per-request SQL reads → production performance preserved.

## Rule

`MsalDistributedTokenCacheAdapterOptions` settings affect **both** cache layers. Any TTL-limiting option must be evaluated against the L2 (SQL/distributed) cache impact, not just L1.
---
# Decision: Sequential Awaits in OnboardingManager.ComputeIsOnboardedAsync

**Date:** 2026-05-19  
**Author:** Trinity  
**Branch:** `issue-980-publisher-architecture-refactor`  
**Commit:** cd55423b
---
## Decision

`OnboardingManager.ComputeIsOnboardedAsync` was changed from `Task.WhenAll` parallel fan-out to sequential `await` calls.

## Rationale

All 8 data store calls share the same scoped `BroadcastingContext`. EF Core's `DbContext` is not thread-safe. The `Task.WhenAll` fan-out caused concurrent reads on the same connection, throwing:

> `System.InvalidOperationException: BeginExecuteReader requires an open and available Connection. The connection's current state is closed.`

This fired on any mutation that called `RecalculateAsync` (e.g., deleting a FeedSource).

**Rule reinforced:** Never use `Task.WhenAll` when the underlying data stores share a single scoped `DbContext`. Use sequential `await` calls instead.
---
## IsActive Filtering in Collector Data Stores

`GetByUserAsync` in the three collector data stores was missing the `IsActive` filter:

- `UserCollectorFeedSourceDataStore.GetByUserAsync` — added `&& c.IsActive`
- `UserCollectorYouTubeChannelDataStore.GetByUserAsync` — added `&& c.IsActive`
- `UserCollectorSpeakingEngagementDataStore.GetByUserAsync` — added `&& c.IsActive`

Inactive collectors (where `IsActive = false`) now correctly do NOT count toward a user's onboarded status.
---
## Publisher IsEnabled Handling

Publisher data stores (`Bluesky`, `Twitter`, `LinkedIn`, `Facebook`) use `IsEnabled` (not `IsActive`) to indicate whether a platform is configured and active. `OnboardingManager` already checks `?.IsEnabled == true` for all four publishers — no change was needed.
---
### 2026-05-18: Web DI Layer Fix — Manager Classes Removed

**By:** Trinity  
**Branch:** `issue-980-publisher-architecture-refactor`  
**Commit:** abc9737b  

#### Decision

The Web project (`JosephGuadagno.Broadcasting.Web`) must **never** register or inject Manager
classes from `JosephGuadagno.Broadcasting.Managers` directly in its DI container, except for
managers that have no Service API wrapper equivalent (e.g., `IUserApprovalManager`,
`IEmailTemplateManager`).

`PostComposer` and `MessageTemplateLookup` were removed from `Web/Program.cs` because:

1. Neither is consumed by any Web controller, service, or middleware.
2. Both require `ISocialMediaPlatformManager` in their constructor chain — a deep backend
   dependency that must not leak into the Web layer.
3. The Web project must reach message-template and composition logic via
   `IMessageTemplateService` (HTTP client wrapper), not through direct Manager instantiation.

#### Ownership of PostComposer and MessageTemplateLookup

These classes belong in the **API** and **Functions** projects only, where direct Manager
injection is permitted and appropriate.

#### Hard Rule

Any PR that registers a `JosephGuadagno.Broadcasting.Managers.*` concrete class in
`Web/Program.cs` for a type that has a Service API wrapper must be rejected at review.

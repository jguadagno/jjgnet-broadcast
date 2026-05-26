# History

> Learnings before 2026-04-25 archived to history-archive.md (2026-05-25)

## MSAL Session Persistence Regression — 2026-05-19

**Trigger**: Joseph reported having to log in every time the Web app restarts after commit `3af53e7f` (fix(auth): suppress MSAL/IdentityModel debug noise and pin L1 cache TTL).

### Root Cause (confirmed from library source)

`MsalDistributedTokenCacheAdapterOptions` inherits from `DistributedCacheEntryOptions`. The `AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)` set in commit `3af53e7f` is passed **directly** to `IDistributedCache.SetAsync()` inside `MsalDistributedTokenCacheAdapter.WriteCacheBytesAsync()` as `DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow`. This overrides the `DefaultSlidingExpiration = TimeSpan.FromDays(14)` on `AddDistributedSqlServerCache`. SQL `TokenCache` entries now expire **15 minutes after the last write** instead of 14 days. After any 15+ minute idle period (including development restarts), MSAL finds no account in the SQL cache, `ValidatePrincipal` calls `context.RejectPrincipal()`, and the user is forced to log in.

Source confirmed: `AzureAD/microsoft-identity-web` → `MsalDistributedTokenCacheAdapter.WriteCacheBytesAsync()`:
```csharp
DistributedCacheEntryOptions distributedCacheEntryOptions = new DistributedCacheEntryOptions
{
    AbsoluteExpiration = cacheExpiry,
    AbsoluteExpirationRelativeToNow = _distributedCacheOptions.AbsoluteExpirationRelativeToNow, // ← 15 min passed to SQL
    SlidingExpiration = _distributedCacheOptions.SlidingExpiration,
};
```

### Exact Code Location

`Program.cs`, lines 115–119:
```csharp
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.DisableL1Cache = false;
    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15); // ← CULPRIT
});
```

### Recommended Fix (pending Joseph approval)

Remove `AbsoluteExpirationRelativeToNow` from `MsalDistributedTokenCacheAdapterOptions`. The SQL cache's `DefaultSlidingExpiration = TimeSpan.FromDays(14)` is restored. The L1 (memory) cache reverts to using the token's `SuggestedCacheExpiry` (pre-change behavior).

```csharp
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.DisableL1Cache = false;
    // AbsoluteExpirationRelativeToNow intentionally omitted:
    // setting it also sets the L2/SQL expiry (15 min forced re-login regression).
    // L2 lifetime is controlled by AddDistributedSqlServerCache DefaultSlidingExpiration (14 days).
});
```

**Trade-off**: The minor performance issue (~1.75s SQL read on first request when access token is near-expiry) may return. Pre-existing, preferable to forced re-login every restart.

**Status**: Diagnosis delivered to Joseph. No code changes made. Awaiting approval.

---

## OAuth Token Architecture Review — 2026-05-21

**Requested by**: Joseph Guadagno  
**Output**: `.squad/decisions/inbox/neo-oauth-token-architecture.md`

### Learnings

**Three token storage mechanisms discovered (two of them inconsistent):**

1. `UserOAuthTokens` table (`scripts/database/table-create.sql` line 483) — per-user, per-platform SQL table with `AccessToken`, `RefreshToken`, expiry columns, `LastNotifiedAt`. Used by LinkedIn `PostLink.cs` and `NotifyExpiringTokens.cs`. Platform-agnostic by design (FK to `SocialMediaPlatforms`).

2. Settings Manager KV path (`publisher-{ownerOid}-{platform}-{settingName}`) via `KeyVaultSecretNameBuilder` — used for app credentials (AppSecret, ClientSecret). For LinkedIn, also has dead `GetAccessTokenAsync`/`StoreAccessTokenAsync`/`HasAccessToken` that were never wired into the publisher pipeline.

3. Global KV path (`jjg-net-facebook-{token-name}-access-token`) — used ONLY by `Facebook/RefreshTokens.cs` via `IFacebookApplicationSettings`. This is a non-user-scoped approach that is disconnected from `PostPageStatus.cs`, which reads per-user KV tokens instead.

**Key file paths:**
- `src/Functions/Facebook/RefreshTokens.cs` — uses `IFacebookApplicationSettings` (global) + `ITokenRefreshManager`; does NOT use `IUserPublisherFacebookSettingsManager`
- `src/Functions/Facebook/PostPageStatus.cs` — uses `IUserPublisherFacebookSettingsManager` (per-user KV); does NOT use `IUserOAuthTokenManager`
- `src/Functions/LinkedIn/PostLink.cs` — uses `IUserOAuthTokenManager` (SQL table) for token; uses `IUserPublisherLinkedInSettingsManager` for `AuthorId` only
- `src/Functions/LinkedIn/NotifyExpiringTokens.cs` — queries `UserOAuthTokens` expiry window
- `src/Managers/UserPublisherLinkedInSettingsManager.cs` — has dead `GetAccessTokenAsync`/`StoreAccessTokenAsync` (KV path never called by pipeline)
- `src/Managers/UserPublisherFacebookSettingsManager.cs` — has `GetPageAccessTokenAsync`, `GetLongLivedAccessTokenAsync`, etc. (KV per-user); these are what PostPageStatus uses
- `src/Domain/Interfaces/IUserOAuthTokenDataStore.cs` — needs `GetExpiringByPlatformAsync()` added
- `src/Domain/Constants/SocialMediaPlatformIds.cs` — LinkedIn = 3, Facebook = 4

**Architectural decision made:**
- `UserOAuthTokens` is the authoritative store for per-user OAuth access tokens with expiry (both LinkedIn and Facebook should use it)
- Settings Manager (KV + SQL flags) is for app credentials only (AppSecret, ClientSecret, AppId, PageId)
- Facebook `RefreshTokens.cs` needs full rewrite to iterate per-user via `UserOAuthTokens`
- LinkedIn `HasAccessToken` KV path is dead code — should be removed
- `TokenRefreshes` table becomes obsolete once Facebook migrates; defer deletion to cleanup pass

---

### Learnings — 2026-05-25: AutoMapper Blast Radius — EngagementDataStore

**Task:** Assess whether Trinity's manual mapping (`ApplyEngagementValues`/`ApplyTalkValues`) should be replaced with a corrected AutoMapper profile.

**Findings:**

1. **Both manual helpers are complete.** Every scalar field on `Domain.Models.Engagement` and `Domain.Models.Talk` is covered. `Talks`, `SocialMediaPlatforms`, and PKs are intentionally excluded.

2. **Root cause of the original bug:** `BroadcastingProfile.cs` line `CreateMap<Models.Engagement, Domain.Models.Engagement>().ReverseMap()` generates a domain→data map that copies the `Talks` collection from the domain object — replacing the EF-tracked `ICollection<Talk>` with untracked AutoMapper-created objects, causing the "Detached" error.

3. **The fix pattern is already established in this codebase.** `Domain.Models.Talk → Models.Talk` already has `.ForMember(d => d.Engagement, opt => opt.Ignore())`. `MessageTemplate` and `SyndicationFeedItem` do the same for their nav props. The Engagement mapping simply needs the same treatment.

4. **Blast radius is minimal.** Only one extra call site uses the generated reverse map (`AddTalkToEngagementAsync`, new-entity path). Ignoring `Talks` and `SocialMediaPlatforms` in that path is safe.

5. **Timestamp logic must stay in data store code.** `CreatedOn` and `LastUpdatedOn` have conditional defaults (UtcNow fallback, isNew gate) that cannot be deterministically expressed in a mapping profile. Keep as explicit post-map assignments.

**Recommendation filed:** `.squad/decisions/inbox/neo-engagement-automapper-blast-radius.md` — REFACTOR (low risk, 3-line profile change + delete `ApplyEngagementValues`).

---

## Event Grid vs Per-User Dispatch Analysis — 2026-05-26

**Trigger:** Issue #995 — Random Post interface needed (Joseph's architecture question)

### Findings

**Event Grid is confirmed incompatible with per-user publisher selection.** Subscriptions are statically registered infrastructure; every subscriber on a topic gets every event. Per-user routing within Event Grid requires either per-user topic provisioning (unmanageable) or embedding routing hints in the payload and having every subscriber check them anyway (defeats the purpose).

**Current `RandomPosts.cs`** picks a single random post for the system-level collector owner OID using global `IRandomPostSettings` (app config). It is not per-user today.

**The four `ProcessNewRandomPost` functions** (Bluesky, Facebook, LinkedIn, Twitter) are purely intermediate — they bridge an Event Grid event to a Storage Queue. Storage Queues are already the actual delivery mechanism. Event Grid is only a fan-out hop.

### Architecture Decision Filed

Decision: replace Event Grid publisher dispatch with direct per-user queue dispatch. The publisher function iterates all users with Random Post enabled, applies per-user settings, and enqueues `SocialMediaPublishRequest` directly to the appropriate platform queues — only for platforms that user has configured.

**New tables needed:** `UserRandomPostSettings` (per-user frequency, cutoff, excluded categories), `UserPublisherEventTypes` (user × platform × event type junction).

**File:** `.squad/decisions/inbox/neo-event-grid-vs-per-user-dispatch.md`

**Comment posted:** GitHub issue #995

### Key File Paths (for future reference)
- `src/Functions/Publishers/RandomPosts.cs` — global timer, single OID, Event Grid dispatch
- `src/Data/EventPublisher.cs` — all Event Grid publish methods
- `src/Functions/event-grid-simulator-config.json` — five topics: new-random-post, new-speaking-engagement, new-syndication-feed-item, new-youtube-item, scheduled-item-fired
- `src/Domain/Interfaces/IRandomPostSettings.cs` — global settings (ExcludedCategories, CutoffDate)
- `scripts/database/table-create.sql` — `UserPublisherSettings` table is the right FK anchor for event-type flags

---



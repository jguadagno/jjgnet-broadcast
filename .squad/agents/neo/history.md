## Executive Summary

**Neo — Architectural Lead, Code Reviewer**

- **Current focus:** Publisher architecture refactor #980 (Phases 1-4 complete), PR reviews (#963, #967, #950)
- **Key decisions:** Per-user credentials, IPostComposer extracted, IMessageTemplateLookup extracted, Phase 4 composition stripped
- **Architecture pattern:** Domain → Data/Managers → API/Web/Functions; composition layer separated via PostComposer; templates user-scoped
- **Team pattern established:** LogSanitizer mandatory for all user-controlled log args, DI constructor injection, enum types over raw strings
- **Phase 4 outcome:** All publisher managers simplified to single PublishAsync(SocialMediaPublishRequest) entry point; TwitterHealthCheck deleted; 19 files changed, 86 insertions, 1158 deletions; all tests pass

---

## Publisher Refactor #980 — Architecture Overview

**6-Phase refactor**: Extract PostComposer, MessageTemplateLookup, migrate Functions, strip manager composition, unify queue DTOs.

### Phase 1 ✅ COMPLETE (commit dbfa2589)
- Deleted 4 dead `*PublisherSettings` (plural) domain classes
- Created IPostComposer/PostComposer with Scriban 7.2.0
- Registered in API, Functions, Web
- Result: 422 tests pass

### Phase 2 ✅ COMPLETE (commit e63c4012)
- Created IMessageTemplateLookup/MessageTemplateLookup
- Extended SocialMediaPublishRequest with OwnerEntraOid + Twitter OAuth properties
- Added user-scoped template lookup (deferred to Phase 3)
- Result: all tests pass

### Phase 3 ✅ COMPLETE (commit 47e8ecec)
- Added user-scoped GetAsync() to IMessageTemplateDataStore
- Migrated all 20 Process* Functions to use IMessageTemplateLookup + IPostComposer
- Updated 4 ProcessScheduledItemFiredTests
- Result: 155 Functions tests pass

### Phase 4 ✅ COMPLETE (current)
- Stripped ComposeMessageAsync/TryRenderTemplateAsync/GetMessageType from all 4 managers
- Twitter rewritten: per-user TwitterContext from SocialMediaPublishRequest credentials
- Removed global TwitterContext DI, TwitterHealthCheck deleted
- Result: 19 files changed, 86 insertions(+), 1158 deletions(-); all 422 tests pass

---

## Key Architectural Decisions

1. **Per-user credentials only** — TwitterManager builds TwitterContext from SocialMediaPublishRequest; no global context
2. **request.Text is canonical** — Composed output by PostComposer, received by managers as-is
3. **Hashtags inline via template** — {{ tags }} variable; Bluesky parses AT Protocol facets from request.Text
4. **Templates required, no fallback** — IMessageTemplateLookup.GetAsync() returns null; Process* functions bail on null
5. **Composition centralized** — PostComposer owns all Scriban rendering; managers own platform APIs only

---

## Recent PR Reviews

**#963** (Blocking ❌) — 3 log injection sites in UserPublisherSettingService; requires LogSanitizer.Sanitize() wraps  
**#967** (Blocking ❌) — KeyVaultSecretOwnerType enum required but missing from Domain; string parameter violates directive  
**#950** (Blocking) — Enum type validation across scope

---

## Issues Tracked

- **#975** — Site admin CRUD for publisher/collector settings (future enhancement)
- **#978** — Post-approval user onboarding setup flow (prerequisite for full template coverage)
- **#979** — Default message templates for new publishers (prerequisite for full template coverage)
- **#980** — Publisher architecture refactor (current, Phases 1-4 complete; Phase 5-6 pending)

---

## Implementation Patterns Established

- **Template composition**: Every Process* function → fetch entity → validate ownerEntraOid → build SocialMediaPublishRequest → lookup template → compose → return DTO
- **Null handling**: Null template/ownerEntraOid/composed text → log warning (with LogSanitizer), return null (skip enqueue)
- **DI pattern**: TryAddScoped<I, T>() in each consumer (API, Functions, Web) Program.cs
- **LinkedIn**: IUserOAuthTokenManager retained for OAuth; OAuth moves to PostLink function in Phase 5
- **Tests**: All manager unit tests updated; Twitter tests rewritten for per-user credentials

---

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


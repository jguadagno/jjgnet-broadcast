---
name: msal-cache-handling
description: 'Handle MSAL distributed token cache collisions and stale entries in ASP.NET Core applications'
---

# Skill: MSAL Distributed Token Cache Handling

**Domain:** Authentication, MSAL, Token Cache, Microsoft.Identity.Web  
**Language:** C#  
**Framework:** ASP.NET Core, Microsoft.Identity.Web

## Overview

Robust pattern for handling MSAL token cache collisions and stale entries in ASP.NET Core applications using SQL-backed distributed token caches. Prevents "Token already exists in cache" errors on app recycles.

> ⚠️ **Project-specific constraint**: In `RejectSessionCookieWhenAccountNotInCacheEvents.ValidatePrincipal`, call `context.RejectPrincipal()` only — do **NOT** call `context.HttpContext.SignOutAsync()`. Calling `SignOutAsync()` inside the cookie validation event creates an authentication redirect loop in this project. The sample code below shows `SignOutAsync()` as it is a general pattern, but it must be omitted here.

> 📢 **Project logging directive**: Do **NOT** add `MinimumLevel.Override` suppressions for `Microsoft.Identity`, `Microsoft.IdentityModel`, or `MSAL` namespaces in DEBUG builds. Verbose MSAL debug output is intentionally kept visible locally — it enables detection of cache miss issues and other auth problems. Production (`#else`) suppression via `MinimumLevel.Override("Microsoft", ...)` is correct and must remain.

## When to Use

- ASP.NET Core Web apps using Microsoft.Identity.Web authentication
- SQL-backed distributed token cache via `AddDistributedSqlServerCache()`
- Multi-instance deployments or apps that recycle frequently
- Need automatic recovery from cache collision errors

## Core Pattern

### 1. Cookie Authentication Events with Cache Error Handling

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

internal class RejectSessionCookieWhenAccountNotInCacheEvents : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        try
        {
            var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();
            var token = await tokenAcquisition.GetAccessTokenForUserAsync(
                scopes: new[] { "profile" },
                user: context.Principal);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex) when (AccountDoesNotExitInTokenCache(ex))
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RejectSessionCookieWhenAccountNotInCacheEvents>>();
            logger?.LogWarning("Token cache issue detected: {ErrorCode}", 
                ex.InnerException is MsalUiRequiredException msalEx ? msalEx.ErrorCode : "unknown");
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
        catch (MsalServiceException ex) when (IsTokenCacheCollision(ex))
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RejectSessionCookieWhenAccountNotInCacheEvents>>();
            logger?.LogWarning("Multiple tokens in cache: {ErrorCode}", ex.ErrorCode);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
        catch (MsalClientException ex) when (IsTokenCacheCollision(ex))
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RejectSessionCookieWhenAccountNotInCacheEvents>>();
            logger?.LogWarning("Token cache collision: {Message}", ex.Message);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    }
    
    private static bool AccountDoesNotExitInTokenCache(MicrosoftIdentityWebChallengeUserException ex)
    {
        return ex.InnerException is MsalUiRequiredException { ErrorCode: "user_null" };
    }
    
    private static bool IsTokenCacheCollision(MsalException ex)
    {
        return ex.ErrorCode == "multiple_matching_tokens_detected" ||
               ex.Message.Contains("multiple tokens", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("cache contains multiple", StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2. Program.cs Configuration

#### Cookie Authentication with Events
```csharp
builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.Events = new RejectSessionCookieWhenAccountNotInCacheEvents();
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.AccessDeniedPath = "/Account/AccessDenied";
    });
```

#### Distributed SQL Server Cache with Expiration
```csharp
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("DatabaseConnectionString");
    options.SchemaName = "dbo";
    options.TableName = "TokenCache";
    options.DefaultSlidingExpiration = TimeSpan.FromDays(14); // Refresh on access
    options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30); // Auto-cleanup
});
```

#### MSAL L1 Cache Adapter Options (REQUIRED — prevents per-request SQL reads)
```csharp
// Configure AFTER .AddDistributedTokenCaches()
// Without pinning AbsoluteExpirationRelativeToNow, near-expiry tokens evict from L1
// almost immediately, causing a ~1.75s SQL distributed-cache read on every request.
builder.Services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
{
    options.DisableL1Cache = false;
    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
});
```
Required using: `Microsoft.Identity.Web.TokenCacheProviders.Distributed`

> Note: `AbsoluteExpirationRelativeToNow` (inherited from `DistributedCacheEntryOptions`) is the property that pins the L1 TTL. The adapter reads this in its constructor to set `_memoryCacheExpirationTime` for all L1 `MemoryCacheEntryOptions`.

#### MSAL Authentication with Distributed Cache
```csharp
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi(allScopes)
    .AddDistributedTokenCaches(); // Uses the configured distributed cache
```

## Key Decisions

### Error Detection Strategy
- **Multiple exception types:** Catch `MsalServiceException` (server) and `MsalClientException` (local) separately
- **Pattern matching:** Error code `multiple_matching_tokens_detected` + message content checks
- **Defensive:** Case-insensitive string matching handles variations in error messages

### Recovery Strategy
- **RejectPrincipal():** Marks the authentication cookie as invalid; user is redirected to sign in
- **SignOutAsync():** In general patterns this clears the session cookie — however, in this project calling `SignOutAsync()` inside `ValidatePrincipal` creates a redirect loop; call `RejectPrincipal()` only
- **Automatic re-auth:** User is redirected to sign in, which creates fresh cache entry

### Logging Strategy
- **Log level:** Warning (not Error) — this is expected behavior during app recycles
- **Content:** Error codes and messages only — no tokens, secrets, or PII
- **Purpose:** Monitoring and alerting on cache collision frequency

### Cache Expiration Strategy
- **Sliding expiration:** 14 days keeps active users' tokens fresh without manual refresh
- **Cleanup interval:** 30 minutes balances performance (DB load) with cache size
- **Rationale:** Expired entries can cause collisions if not cleaned up promptly

## Database Schema

```sql
CREATE TABLE dbo.TokenCache
(
    Id                         NVARCHAR(449)  NOT NULL PRIMARY KEY,
    Value                      VARBINARY(MAX) NOT NULL,
    ExpiresAtTime              DATETIMEOFFSET NOT NULL,
    SlidingExpirationInSeconds BIGINT,
    AbsoluteExpiration         DATETIMEOFFSET
)

CREATE INDEX Index_ExpiresAtTime ON TokenCache (ExpiresAtTime)
```

## Common Pitfalls

1. **Calling `SignOutAsync()` in `ValidatePrincipal`:** In this project, this creates an auth redirect loop — call `context.RejectPrincipal()` only
2. **Missing `L1ExpirationTimeSpan`:** Without this, near-expiry tokens cause L1 cache eviction immediately after write, forcing a ~1.75s SQL read on every request
3. **Missing using directive:** `MsalDistributedTokenCacheAdapterOptions` requires `using Microsoft.Identity.Web.TokenCacheProviders.Distributed`
4. **Logger retrieval:** Must use `GetService<ILogger<T>>()` at request time, not constructor DI in cookie events
5. **Only catching `user_null`:** Cache collisions have different error codes/messages — must check multiple patterns
6. **No cache expiration:** Without cleanup, cache grows indefinitely and collision probability increases
7. **Caching in memory only:** Loses tokens on app recycle, forces all users to re-authenticate

## Testing Checklist

- [ ] Build succeeds with no errors
- [ ] All existing tests pass
- [ ] Deploy to staging, restart app pool, verify users can sign in
- [ ] Monitor logs for cache collision warnings
- [ ] Verify expired cache entries are deleted within 30 minutes
- [ ] Stress test: Multiple concurrent users signing in after app recycle

## Security Considerations

- ✅ **No secrets in logs:** Only log error codes/messages
- ✅ **Automatic recovery:** Users never stuck in broken auth state
- ✅ **Defense in depth:** Handles multiple error types
- ✅ **Session hygiene:** SignOut clears stale cookies
- ⚠️ **User experience:** Users must re-authenticate on collision (acceptable trade-off)

## Related Patterns

- **[AuthorizeForScopes]:** Attribute to trigger consent/re-auth on downstream API calls
- **MsalExceptionMiddleware:** Global fallback for unhandled MSAL exceptions (use sparingly)
- **Claims Transformation:** Modify claims after token acquisition
- **Token Cache Serialization:** Custom serialization for non-SQL backends (Redis, Cosmos, etc.)

## References

- Microsoft.Identity.Web docs: https://aka.ms/ms-id-web
- MSAL token cache serialization: https://aka.ms/msal-net-token-cache
- Distributed caching in ASP.NET Core: https://aka.ms/aspnetcore-distributed-cache
- Cookie authentication events: https://aka.ms/aspnetcore-cookie-events

## Version History

- **v1.0 (2026-04-06):** Initial pattern from Issue #81 / PR #648

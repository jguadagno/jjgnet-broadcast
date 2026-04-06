# Skill: MSAL Distributed Token Cache Handling

**Domain:** Authentication, MSAL, Token Cache, Microsoft.Identity.Web  
**Language:** C#  
**Framework:** ASP.NET Core, Microsoft.Identity.Web

## Overview

Robust pattern for handling MSAL token cache collisions and stale entries in ASP.NET Core applications using SQL-backed distributed token caches. Prevents "Token already exists in cache" errors on app recycles.

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
- **RejectPrincipal():** Marks the authentication cookie as invalid
- **SignOutAsync():** Clears the session cookie to prevent redirect loops
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

1. **Missing `SignOutAsync()`:** Without this, stale cookies can cause redirect loops
2. **Missing using directive:** `SignOutAsync()` requires `Microsoft.AspNetCore.Authentication`
3. **Logger retrieval:** Must use `GetService<ILogger<T>>()` at request time, not constructor DI in cookie events
4. **Only catching `user_null`:** Cache collisions have different error codes/messages — must check multiple patterns
5. **No cache expiration:** Without cleanup, cache grows indefinitely and collision probability increases
6. **Caching in memory only:** Loses tokens on app recycle, forces all users to re-authenticate

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

---
name: msal-obo-flow
description: On-Behalf-Of (OBO) Flow for web APIs to call downstream APIs while preserving user identity in MSAL.NET
tags:
  - msal
  - obo
  - on-behalf-of
  - token-exchange
  - confidential-client
  - multi-tier
  - downstream-api
  - user-assertion
source: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/.github/skills/msal-obo-flow/SKILL.md
---

# On-Behalf-Of (OBO) Flow Skill

## Overview
OBO (On-Behalf-Of) Flow enables a web API to act on behalf of an authenticated user to access downstream APIs. The web API receives a user token, validates it, and exchanges it for a token to call another API while maintaining the user's identity and context.

## When to Use
- Web APIs receiving user tokens from clients
- Need to access downstream APIs on behalf of authenticated users
- Multi-tier applications with user context propagation
- User authorization context must flow through service chain

## Flow Steps
1. Client calls web API with user access token in Authorization header
2. Web API validates the incoming token
3. Web API exchanges user token for new token scoped for downstream API
4. Web API calls downstream API on behalf of user

## Important: Token Types
⚠️ **Always pass an access token, NOT an ID token** to `AcquireTokenOnBehalfOf()`  
ID tokens are for authentication; access tokens are for authorization and API access.

## Example: Web API with Certificate
```csharp
// In web API controller receiving user token
[HttpGet("api/data")]
public async Task<IActionResult> GetData()
{
    // Extract access token from Authorization header
    var authHeader = Request.Headers["Authorization"].ToString();
    var userToken = authHeader.Replace("Bearer ", "");
    
    var app = ConfidentialClientApplicationBuilder
        .Create(clientId)
        .WithCertificate(cert)
        .WithAuthority($"https://login.microsoftonline.com/{tenantId}/v2.0")
        .Build();

    // Create UserAssertion with access token (not ID token)
    var userAssertion = new UserAssertion(userToken, "urn:ietf:params:oauth:grant-type:jwt-bearer");
    
    var result = await app.AcquireTokenOnBehalfOf(
        new[] { "scope-uri" },
        userAssertion)
        .ExecuteAsync();

    // Use result.AccessToken to call downstream API
    return Ok(result.AccessToken);
}
```

## Best Practices
- Use token caching (see `msal-shared/references/token-caching-strategies.md`) for optimal session-based token caching
- Always validate incoming token before using in OBO
- Extract `tid` claim from user token for guest user scenarios — use tenant-specific authority, not `/common`
- For multi-instance deployments and advanced caching, use `AddDistributedTokenCaches()` with `MsalDistributedTokenCacheAdapterOptions`

## Common OBO Errors
- `MsalUiRequiredException`: MFA or conditional access required — requires client re-authentication
- Invalid token: Verify access token (not ID token) is passed

## Project-Specific Notes (JJGNet Broadcasting)
- This project uses `Microsoft.Identity.Web`'s `ITokenAcquisition.GetAccessTokenForUserAsync()` for OBO — it handles the token exchange automatically
- The Web app (`JosephGuadagno.Broadcasting.Web`) calls the API (`JosephGuadagno.Broadcasting.Api`) using OBO flow
- Token cache is SQL-backed via `AddDistributedTokenCaches()` → `AddDistributedSqlServerCache()`
- `MsalDistributedTokenCacheAdapterOptions.AbsoluteExpirationRelativeToNow` should be set (e.g., 15 minutes) to prevent L1 cache misses causing per-request SQL reads

## Decision Help
**Choose OBO if:**
- Building multi-tier web API architecture
- Receiving user tokens in web API
- Need to maintain user context through service chain

**Avoid if:**
- Direct client-to-API communication (use Auth Code Flow)
- Service-to-service with no user context (use Client Credentials)

## References
- [Token cache serialization](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization?tabs=msal)
- [OBO flow documentation](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow)

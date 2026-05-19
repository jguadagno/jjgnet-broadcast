---
name: msal-auth-code-flow
description: Authorization Code Flow for web applications using MSAL.NET confidential client to sign in users and access APIs on their behalf
tags:
  - msal
  - auth-code
  - authorization-code
  - web-app
  - confidential-client
  - user-sign-in
  - redirect
  - consent
source: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/main/.github/skills/msal-auth-code-flow/SKILL.md
---

# Authorization Code Flow Skill

## Overview
Authorization Code Flow is used by web applications to authenticate users and obtain access tokens on their behalf.

## When to Use
- Web applications with server-side backend
- Need to access user-scoped APIs
- User sign-in required
- Refresh tokens needed

## Flow Steps
1. Redirect user to AAD login page
2. User logs in and consents to permissions
3. AAD returns authorization code to callback URL
4. Server exchanges code for token using confidential credentials
5. Token cached and used to access APIs

## Example: Web Application with Certificate
```csharp
// In controller's callback method
[HttpGet("auth/callback")]
public async Task HandleCallback(string code, string state)
{
    var app = ConfidentialClientApplicationBuilder
        .Create(clientId)
        .WithCertificate(cert)
        .WithAuthority($"https://login.microsoftonline.com/{tenantId}/v2.0")
        .WithRedirectUri("https://myapp.com/auth/callback")
        .Build();

    var result = await app.AcquireTokenByAuthorizationCode(
        new[] { "scope-uri" },
        code)
        .ExecuteAsync();

    // Result contains AccessToken, RefreshToken, ExpiresOn
}
```

## Best Practices
- Use token caching (see `msal-shared/references/token-caching-strategies.md`) for optimal token acquisition
- Store refresh tokens securely
- Use PKCE for native clients
- For multi-instance deployments, use distributed token cache — see `msal-cache-handling` skill

## Project-Specific Notes (JJGNet Broadcasting)
- This project uses `AddMicrosoftIdentityWebAppAuthentication()` which handles the auth code flow automatically
- Cookie authentication is layered on top — `RejectSessionCookieWhenAccountNotInCacheEvents` validates on every request
- **Do NOT call `SignOutAsync()` in `ValidatePrincipal`** — this creates an auth redirect loop; call `context.RejectPrincipal()` only

## Decision Help
**Choose Auth Code Flow if:**
- Building web application with server backend
- Need to access user resources with user consent
- Want to maintain long-lived sessions (using refresh tokens)

**Avoid if:**
- Building single-page app (use implicit/hybrid instead)
- Don't have secure backend for credentials

## References
- [Auth code flow documentation](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow)
- [Microsoft.Identity.Web docs](https://aka.ms/ms-id-web)

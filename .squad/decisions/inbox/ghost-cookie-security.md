# Ghost — Cookie Security Hardening (Issue #336)

**Date:** 2026-03-19
**Sprint:** Sprint 8
**PR:** #510

## What Was Done

Three separate cookie surfaces were hardened in `src/JosephGuadagno.Broadcasting.Web/Program.cs`:

### 1. Auth Cookie (`CookieAuthenticationOptions`)
Previously only set `Events`. Now also sets:
- `HttpOnly = true`
- `SecurePolicy = CookieSecurePolicy.Always`
- `SameSite = SameSiteMode.Lax`

*Lax is appropriate for the auth cookie — it must survive top-level cross-site navigations (e.g., OIDC redirect back from Azure AD).*

### 2. Session Cookie (`AddSession`)
Previously used `AddSession()` with no options. Now:
- `HttpOnly = true`
- `SecurePolicy = CookieSecurePolicy.Always`
- `SameSite = SameSiteMode.Lax`
- `IsEssential = true` — prevents session cookie from being blocked by GDPR middleware before consent

### 3. Antiforgery Cookie (`AddAntiforgery`)
Not previously configured at all. Added explicit:
- `HttpOnly = true`
- `SecurePolicy = CookieSecurePolicy.Always`
- `SameSite = SameSiteMode.Strict`

*Strict is correct for the antiforgery token — it never needs to be sent on cross-site requests. This provides the strongest CSRF protection.*

## Findings / Learnings

- `ImplicitUsings=enable` on the Web project means `Microsoft.AspNetCore.Http` types (`CookieSecurePolicy`, `SameSiteMode`) are available without explicit `using` statements.
- `AddAntiforgery` is called before `AddControllersWithViews` so our explicit configuration wins over the default registered by MVC.
- The `Configure<CookieAuthenticationOptions>` post-configuration pattern used by MSAL (`RejectSessionCookieWhenAccountNotInCacheEvents`) still works fine when security options are added to the same lambda.
- SameSite=Lax (not Strict) is required for the auth cookie because the OIDC `redirect_uri` is a cross-site POST from Azure AD — Strict would break login.

## Decision

> Cookie security flags must be explicitly set on all cookie surfaces (auth, session, antiforgery) rather than relying on framework defaults. This is now the pattern for this project.

# Decision: MSAL Exception Handling Architecture (Issue #85)

**Date:** 2026-03-21
**Agent:** Ghost (Security & Auth Engineer)
**Issue:** #85 - Handle Exceptions with Microsoft Entra (Microsoft Identity) login
**Status:** Design Complete — Awaiting Sprint Assignment

---

## Context

The Web application uses Microsoft.Identity.Web 4.5.0 with OpenID Connect authentication. When Azure AD configuration errors or service failures occur during initial login, users see unhandled `OpenIdConnectProtocolException` with raw AAD error messages (e.g., "AADSTS650052: The app needs access to a service..."). This creates poor UX and exposes technical details.

**Existing protections (from PR #532):**
- `[AuthorizeForScopes]` on 4 API-calling controllers handles `MsalUiRequiredException` for incremental consent
- `RejectSessionCookieWhenAccountNotInCacheEvents` handles cookie validation when account is missing from token cache

**Gap:** No handling for initial login failures, AAD service errors, or MSAL client exceptions outside of token acquisition.

---

## Decision: Layered Exception Handling Architecture

### Layer 1: OpenID Connect Event Handlers (Highest Priority)
Catch authentication failures at the earliest point — during the OIDC callback.

**Implementation:**
- Add `OnRemoteFailure` event handler to catch `OpenIdConnectProtocolException`
- Add `OnAuthenticationFailed` event handler for token validation errors
- Route to dedicated `/Home/AuthError` page with sanitized error messages

**Rationale:**
- Handles root cause of issue #85 (initial login failures)
- Provides context-specific error messages (config error vs. service down vs. wrong tenant)
- Logs full exception details server-side while showing user-friendly messages
- Applied via `builder.Services.Configure<OpenIdConnectOptions>` in `Program.cs`

**AAD Error Code Mapping:**
- `AADSTS650052` → "Application not properly registered"
- `AADSTS700016` → "Application not found in directory"
- `invalid_client` → "Authentication configuration error"
- All others → Generic "Authentication failed" message

### Layer 2: Global MSAL Exception Middleware (Safety Net)
Catch MSAL exceptions that escape controller-level handling.

**Implementation:**
- Custom middleware `MsalExceptionMiddleware` placed AFTER `UseRouting()`, BEFORE `UseAuthentication()`
- Catches: `MsalServiceException`, `MsalClientException`, fallback `MicrosoftIdentityWebChallengeUserException`

**Rationale:**
- Provides safety net for edge cases (e.g., MSAL exceptions in filters, view rendering)
- Centralizes MSAL exception logic (vs. per-controller try/catch)
- Does NOT interfere with `[AuthorizeForScopes]` (middleware is fallback only)

**Exception Routing:**
- `MsalServiceException` → "Service unavailable" (AAD down/throttling)
- `MsalClientException` (multiple_matching_tokens) → Clear cache + force sign-out (Issue #83)
- `MsalClientException` (other) → Force re-authentication
- `MsalUiRequiredException` → Should be caught by `[AuthorizeForScopes]` (log warning if reaches middleware)

### Layer 3: Dedicated Authentication Error Page
Replace generic `Error.cshtml` usage for auth failures with purpose-built view.

**Implementation:**
- `HomeController.AuthError(string message)` action marked `[AllowAnonymous]`
- `AuthError.cshtml` view with user-friendly messaging, retry button, support contact
- `AuthErrorViewModel` to pass sanitized error message + retry URL

**Rationale:**
- Auth errors need different UX than app errors (retry login vs. contact support)
- `[AllowAnonymous]` required since auth errors occur pre-authentication
- Separate page allows future enhancements (e.g., AAD admin consent button, diagnostics for admins)

### Layer 4: Token Cache Resilience (Issue #83)
Enhance `RejectSessionCookieWhenAccountNotInCacheEvents` to handle cache collisions.

**Implementation:**
- Add catch block for `MsalClientException` with error code `multiple_matching_tokens_detected`
- Clear token cache for the user
- Reject principal → forces sign-out

**Rationale:**
- Issue #83 is distinct from #85 but related (cache-level MSAL error)
- Cookie validation is the correct place to detect stale/corrupt cache state
- Separating into Phase 3 allows independent testing and rollout

---

## Implementation Phases

### Phase 1: Critical Login Path (addresses issue #85)
**Scope:** Layer 1 + Layer 3
**Deliverables:**
1. OpenID Connect event handlers in `Program.cs`
2. `HomeController.AuthError` action + `AuthErrorViewModel`
3. `AuthError.cshtml` view

**Testing:** Simulate AAD config errors (invalid client ID, AADSTS650052, AAD service down)

### Phase 2: Global Safety Net
**Scope:** Layer 2 + Harden existing `Error.cshtml`
**Deliverables:**
1. `MsalExceptionMiddleware.cs`
2. Register middleware in `Program.cs`
3. Update `Error.cshtml` to hide Request ID in production

**Testing:** Simulate MSAL exceptions outside of OIDC flow

### Phase 3: Token Cache Resilience
**Scope:** Layer 4 (closes issue #83)
**Deliverables:**
1. Enhance `RejectSessionCookieWhenAccountNotInCacheEvents`
2. Add cache-clearing utility (if not in Microsoft.Identity.Web)

**Testing:** Simulate token cache collision (multiple tokens for same user/scope)

---

## Security Considerations

1. **Error Message Sanitization:** Never expose raw exception messages, AAD error codes, or stack traces to users (log server-side only)
2. **AllowAnonymous Scope:** Only auth error page should bypass authentication — regular error page remains protected
3. **Logging:** Auth failures logged as `LogError` (with full exception) for security monitoring
4. **Retry Limits:** Considered adding backoff/retry limits to prevent auth loops (deferred to implementation phase)

---

## Alternative Approaches Considered

### Alt 1: Single Global Exception Filter
**Rejected:** Exception filters run after controller action selection. OIDC callback failures occur before controller execution, so they wouldn't be caught.

### Alt 2: Custom AuthenticationHandler
**Rejected:** Over-engineering. OpenID Connect event handlers provide the exact hook points needed without replacing the entire authentication handler.

### Alt 3: Per-Controller Try/Catch
**Rejected:** Duplicates logic across controllers. Middleware is more maintainable and catches non-controller paths.

---

## Open Questions for Implementation

1. **Sign-out flow:** Should token cache clearing use `Account/SignOut` or custom sign-out route? (Recommendation: Use existing `/MicrosoftIdentity/Account/SignOut` from Microsoft.Identity.Web.UI)
2. **Cache clearing API:** Does Microsoft.Identity.Web 4.5.0 expose cache eviction methods? (Needs research during implementation)
3. **Retry backoff:** Should there be a limit on auth retries to prevent loops? (Recommendation: Start without, add if abuse detected)

---

## Success Metrics

**User Experience:**
- ✅ Zero raw exception pages shown to users for auth failures
- ✅ Clear, actionable error messages (e.g., "contact administrator" for config issues)
- ✅ Retry/sign-out buttons on error page

**Operational:**
- ✅ All auth failures logged with full context (error codes, correlation IDs, user identifiers)
- ✅ Reduced support tickets related to "unhandled exception during login"

---

## References

- **Issue #85:** https://github.com/jguadagno/jjgnet-broadcast/issues/85
- **Issue #83 (cache collision):** https://github.com/jguadagno/jjgnet-broadcast/issues/83
- **PR #532 ([AuthorizeForScopes]):** https://github.com/jguadagno/jjgnet-broadcast/pull/532
- **Microsoft.Identity.Web docs:** https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web
- **MSAL error codes:** https://learn.microsoft.com/en-us/azure/active-directory/develop/reference-aadsts-error-codes

---

**Decision Owner:** Ghost
**Approved For Implementation By:** Joseph Guadagno (pending sprint assignment)
**Next Action:** Joseph to prioritize Phase 1 for upcoming sprint

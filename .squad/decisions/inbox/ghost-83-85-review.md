# Ghost Decision: Issues #83 and #85 NOT Resolved by PR #532

**Date:** 2026-03-21  
**Author:** Ghost  
**Status:** For team awareness

## Context

Joseph requested review of whether issues #83 and #85 are fully resolved by PR #532 (merged).

## Analysis

### PR #532 Scope

PR #532 added `[AuthorizeForScopes]` class-level attribute to all 4 API-calling MVC controllers:
- EngagementsController
- TalksController
- SchedulesController
- MessageTemplatesController

**What it fixes:** Handles `MicrosoftIdentityWebChallengeUserException` (wrapping `MsalUiRequiredException`) when MSAL token cache has evicted API scope tokens but the account is still in cache. The attribute catches the exception and redirects to AAD for incremental consent instead of throwing a 500 error.

### Issue #83: MsalClientException — "multiple tokens in cache"

**Exception:** `Microsoft.Identity.Client.MsalClientException`  
**Error message:** "The cache contains multiple tokens satisfying the requirements. Try to clear token cache."  
**Date reported:** 2022-06-04  

**Resolution status:** ❌ NOT resolved by PR #532

**Reason:** Different exception type. The `[AuthorizeForScopes]` filter only catches `MicrosoftIdentityWebChallengeUserException`. The "multiple tokens" error is a cache collision/partitioning issue within MSAL itself — either the cache key construction is incorrect or there's a bug in the token selection logic.

**Next steps:** 
1. Attempt to reproduce with current SQL-backed token cache configuration
2. If reproducible, investigate MSAL cache partitioning — may need custom `ITokenCacheSerializer` or explicit cache keys per user+scope
3. Check if MSAL library version update resolves (currently using 4.42.0 per error message)

### Issue #85: OpenIdConnectProtocolException — AADSTS650052

**Exception:** `Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectProtocolException`  
**Error code:** AADSTS650052  
**Error message:** "The app needs access to a service ('api://027edf6f-...') that your organization '...' has not subscribed to or enabled."  
**Date reported:** 2022-06-26  

**Resolution status:** ❌ NOT resolved by PR #532

**Reason:** Different scenario entirely. This error occurs during the OpenID Connect callback (initial login flow), BEFORE any token caching happens. It's an Azure AD app registration issue:
- The Web app's Azure AD registration is missing API permissions for the Broadcasting API app
- OR admin consent has not been granted
- OR the API app is not published/available in the target tenant

**Next steps:**
1. Verify Azure AD app registrations for both Web and API apps
2. Check API permissions on Web app registration — must include all required scopes from Broadcasting API
3. Ensure admin consent is granted (or user consent if allowed by tenant policy)
4. This is likely environment-specific — may work in dev tenant but fail in a different org's tenant

## Decision

Both issues remain OPEN and have been labeled `squad:ghost` for continued investigation. PR #532 addressed a separate (but related) auth issue — it did not resolve either #83 or #85.

Updated both issues with detailed analysis comments explaining why they are not resolved by #532 and outlining recommended next steps.

## Impact

- Issue #83 requires code investigation/fix (MSAL cache handling)
- Issue #85 requires infrastructure/config fix (Azure AD app registrations)
- Both are auth/security issues that fall under Ghost's charter

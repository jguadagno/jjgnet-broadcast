# Ghost Decision: LinkedIn Token Refresh Automation (S4-1)

**Date:** 2026-03-17  
**Author:** Ghost (Security & Identity Specialist)  
**Branch:** `feature/s4-1-linkedin-token-automation`

---

## How LinkedIn Token Refresh Differs from Facebook

| Aspect | Facebook | LinkedIn |
|--------|----------|----------|
| Refresh mechanism | Call Graph API with existing long-lived token | OAuth2 `grant_type=refresh_token` using a stored refresh token |
| Token lifetime | ~60 days (long-lived) | 60 days (access), 365 days (refresh) |
| Refresh token issued | No — same token is extended | Yes — refresh token may rotate on use |
| Human interaction required | Never (fully automated) | Only on first authorization and if refresh token expires (365-day window) |
| Manager method | `IFacebookManager.RefreshToken(string token)` | `ILinkedInManager.RefreshTokenAsync(clientId, clientSecret, refreshToken, url)` |

**Key asymmetry:** Facebook's long-lived token can refresh itself. LinkedIn requires a separate refresh token stored in Key Vault, obtained during the initial OAuth2 authorization code flow in the Web UI (`LinkedInController`). The Web controller's `Callback` action was updated in this PR to persist `jjg-net-linkedin-refresh-token` alongside the access token.

---

## Key Vault Secrets Used

| Secret Name | Contents | Set By |
|-------------|----------|--------|
| `jjg-net-linkedin-access-token` | LinkedIn OAuth2 access token (60-day expiry) | `LinkedInController.Callback` (Web UI) / `LinkedIn.RefreshTokens` (Function, after refresh) |
| `jjg-net-linkedin-refresh-token` | LinkedIn OAuth2 refresh token (365-day expiry) | `LinkedInController.Callback` (Web UI) / `LinkedIn.RefreshTokens` (Function, if LinkedIn issues a new one) |

**Critical prerequisite:** `jjg-net-linkedin-refresh-token` must exist in Key Vault before the Function can run. It is populated when a user completes the OAuth2 flow in the Web UI for the first time (or re-authorizes). If the secret is missing or empty, the Function logs a `LogError` and exits gracefully — no crash.

---

## Timer Schedule Chosen

**Setting key:** `linkedin_refresh_tokens_cron_settings`  
**Production value (recommended):** `0 0 9 * * *` (daily at 09:00 UTC)  
**Local dev value:** `0 0 9 * * *`

### Rationale

- LinkedIn access tokens expire in **60 days**. The 5-day proactive buffer means refresh triggers when `expiry - 5 days < now`, i.e. from day 55 onward.  
- A **daily check at 09:00 UTC** is sufficient — no need to check every 2 minutes (unlike Facebook's development cron). Over-frequent checks risk unnecessary API calls and rate-limit exposure.  
- Facebook uses `0 */2 * * * *` only in dev for fast local iteration; production would also use a daily schedule. We set LinkedIn's dev cron directly to daily since there is no local token to test with anyway.

---

## Limitations Discovered

1. **Bootstrap requirement:** The refresh token flow cannot be bootstrapped without a human completing the OAuth2 authorization code flow at least once via the Web UI. This is inherent to LinkedIn's API — they do not support machine-only initial authorization.

2. **Refresh token rotation:** LinkedIn may issue a new refresh token on every refresh call. The Function handles this by saving the new refresh token back to Key Vault if one is returned.

3. **No `ILinkedInApplicationSettings.AccessTokenUrl` previously:** The settings model did not include the token endpoint URL. Added with default `https://www.linkedin.com/oauth/v2/accessToken`. This default can be overridden in Azure App Service settings.

4. **`TokenRefresh` tracking record name:** Uses the string `"LinkedIn"` as the token name in Table Storage, consistent with Facebook's `"LongLived"` / `"Page"` convention.

5. **Refresh token expiry not tracked in Table Storage:** The `TokenRefresh` model only tracks access token expiry. If the refresh token expires (365 days), the Function will log an error and require manual re-authorization. Consider adding a separate `TokenRefresh` record for the refresh token in a future sprint.

6. **LinkedIn's refresh token grant requires `offline_access` scope** (or equivalent — verify current LinkedIn documentation). The existing Web controller scopes (`_linkedInSettings.Scopes`) must include the permission that enables programmatic refresh. If the scope is not set correctly, the initial authorization will succeed but no refresh token will be issued.

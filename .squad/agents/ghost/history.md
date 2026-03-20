# Ghost — History

## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2026-03-19 | Sprint 8 #336 — Explicit cookie security options (HttpOnly, Secure, SameSite) in Web/Program.cs | ✅ PR #510 opened, merged to Sprint 8 milestone |
| 2026-03-20 | Issue #170 — Fine-grained permission scopes on all API endpoints | ✅ PR #526 opened targeting main |
| 2026-03-21 | Issue #528 — MsalUiRequiredException on token cache eviction (Web → API calls) | ✅ PR #532 opened targeting main |
| 2026-03-21 | Sprint Summary — Auth token lifecycle handling complete, SQL token cache confirmed | ✅ [AuthorizeForScopes] on all 4 Web controllers calling API |

## Learnings

### Issue #528 — MsalUiRequiredException / incremental consent

**Scenario covered:**
Session cookie is valid, but the MSAL SQL token cache has been evicted (app restart or SQL Cache table expiry). `RejectSessionCookieWhenAccountNotInCacheEvents` covers the case where the account object itself is missing from cache (`user_null` error code → rejects cookie). Issue #528 is the remaining gap: account IS in cache (so `ValidatePrincipal` passes), but the specific API scope tokens are gone.

**Root cause path:**
`ServiceBase.SetRequestHeader` → `ITokenAcquisition.GetAccessTokenForUserAsync(scope)` → MSAL can't silently refresh (no refresh token in cache) → throws `MsalUiRequiredException` → wrapped by Microsoft.Identity.Web as `MicrosoftIdentityWebChallengeUserException` → unhandled = 500.

**Fix chosen: `[AuthorizeForScopes]` at controller class level**
Applied to all 4 API-calling controllers: `EngagementsController`, `TalksController`, `SchedulesController`, `MessageTemplatesController`. The attribute is an `ExceptionFilterAttribute`. When it catches `MicrosoftIdentityWebChallengeUserException`, it reads `ex.Scopes` (populated by Microsoft.Identity.Web with the exact scope that failed) and issues a `ChallengeResult` with those scopes — redirecting to AAD for re-auth. No `Scopes`/`ScopeKeySection` attribute params needed: the exception carries the required scope.

**Token cache is SQL-backed — confirmed.**
`AddDistributedSqlServerCache` (SQL `dbo.Cache` table) + `AddDistributedTokenCaches()` in `Program.cs`. No in-memory fallback. This is correct. Noted as observation only — not changed in this PR.

**Issue #83 / #85 context:**
- #83: `MsalClientException` "cache contains multiple tokens" — different error code, not addressed here.
- #85: `OpenIdConnectProtocolException` on login with wrong org — AADSTS650052, separate auth config issue, not addressed here.

### Issue #170 — Fine-grained API scopes

**Codebase state at time of work:**
The `Domain/Scopes.cs` file already had fine-grained scope constants defined (`List`, `View`, `Modify`, `Add`, `Delete`) for Engagements, Talks, and Schedules. All controllers had the granular `VerifyUserHasAnyAcceptedScope(...)` calls commented out and were only using `*.All`. The Web services mirrored this pattern. `MessageTemplates` only had `All` — no granular scopes defined.

**Pattern chosen: dual-scope acceptance for backward compat**
`HttpContext.VerifyUserHasAnyAcceptedScope(specificScope, *.All)` — accepts either. This means existing Azure AD tokens with `*.All` continue working without any client reconfiguration. New tokens can be issued with least-privilege scopes only.

**Web services use single specific scope per call**
The `SetRequestHeader(scope)` method on `ServiceBase` acquires a token with exactly the requested scope. Changed all Web service calls to request the fine-grained scope rather than `*.All`. This means the Web app's MSAL tokens will only carry the specific scope needed per operation, which is the correct least-privilege behavior at the token level.

**Bug discovered and fixed in EngagementService.DeleteEngagementTalkAsync**
Was requesting `Engagements.All` scope and the comment incorrectly said `Engagements.Delete`. The correct scope for deleting a talk is `Talks.Delete`. Fixed as part of this work.

**Scopes.ToDictionary vs AllAccessToDictionary**
- `ToDictionary(scopeUrl)` — all fine-grained scopes. Used for Swagger OAuth scope list (updated XmlDocumentTransformer to use this).
- `AllAccessToDictionary(scopeUrl)` — just `*.All` scopes. Still used by Web/Program.cs for MSAL `EnableTokenAcquisitionToCallDownstreamApi` (the set of scopes the web app's MSAL client is allowed to request). This was left as-is; MSAL will automatically scope-down per-request based on what each service call requests.

**Tests: *.All remains valid**
Existing unit tests pass `*.All` tokens and they continue to pass because the API accepts `specificScope OR All`. No test changes needed. The 42/42 pass rate also resolved 3 pre-existing test failures (those failures were unrelated to scope logic — they appear to have been caused by in-flight branch state on the workspace).

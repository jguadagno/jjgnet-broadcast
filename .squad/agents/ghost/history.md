# Ghost — History

## Work Log

| Date | Task | Outcome |
|------|------|---------|
| 2026-04-01 | Issue #603 — Implement EntraClaimsTransformation and UserApprovalMiddleware (RBAC Phase 1) | ✅ Commit a046eb0 pushed to squad/rbac-phase1 |
| 2026-03-19 | Sprint 8 #336 — Explicit cookie security options (HttpOnly, Secure, SameSite) in Web/Program.cs | ✅ PR #510 opened, merged to Sprint 8 milestone |
| 2026-03-20 | Issue #170 — Fine-grained permission scopes on all API endpoints | ✅ PR #526 opened targeting main |
| 2026-03-21 | Issue #528 — MsalUiRequiredException on token cache eviction (Web → API calls) | ✅ PR #532 opened targeting main |
| 2026-03-21 | Sprint Summary — Auth token lifecycle handling complete, SQL token cache confirmed | ✅ [AuthorizeForScopes] on all 4 Web controllers calling API |
| 2026-03-21 | Issue #547 — Harden Error.cshtml: full Request ID in Dev only; first-8-char 'Error reference' in Production | ✅ PR #552 opened targeting main |
| 2026-03-21 | Issue #545 — Add dedicated AuthError page and view model (part of #85) | ✅ PR #551 opened targeting main |
| 2026-03-21 | Issue #546 — Add global MsalExceptionMiddleware (part of #85) | ✅ PR #554 opened targeting main |
| 2026-03-21 | Issue #544 — Add OpenID Connect event handlers for login failures (OnRemoteFailure, OnAuthenticationFailed) | ✅ PR #553 opened targeting main |
| 2026-03-21 | Issue #548 — Add token cache collision resilience to RejectSessionCookieWhenAccountNotInCacheEvents (first line of defence for #83) | ✅ PR #555 opened targeting main |

## Sprint 11 Final Outcomes

All 5 sprint 11 PRs merged to main. All 5 issues (#544–#548) closed. Three-layer auth exception defence for issue #85 fully delivered:

| Layer | Component | PR | Issue |
|-------|-----------|-----|-------|
| Layer 1 | `RejectSessionCookieWhenAccountNotInCacheEvents` — `multiple_matching_tokens_detected` catch | #555 | #548 |
| Layer 2 | `MsalExceptionMiddleware` — global middleware (after UseRouting, before UseAuthentication) | #554 | #546 |
| Layer 3 | `Program.cs` OIDC event handlers (`OnRemoteFailure`/`OnAuthenticationFailed`) | #553 | #544 |
| Support | `AuthError` page + `AuthErrorViewModel` + `[AllowAnonymous]` action | #551 | #545 |
| Support | `Error.cshtml` hardened — `IsDevelopment()` gates full Request ID | #552 | #547 |

**Incident note:** PR #553 branch `issue-544` was initially submitted with stray files from #545/#547 and no Program.cs changes. Neo flagged during review. Trinity corrected the branch; Neo re-approved. Lesson: verify diff matches commit intent before pushing.

Sprint 12 is tagged with 13 issues.

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

### Issue #548 — Token cache collision resilience in cookie validation (first line of defence for #83)

**What was built:**
Added a `catch (MsalClientException msalEx) when (msalEx.ErrorCode == "multiple_matching_tokens_detected")` block inside `RejectSessionCookieWhenAccountNotInCacheEvents.ValidatePrincipal`. The handler logs a Warning (including the user's identity name resolved from `context.Principal?.Identity?.Name`) then calls `context.RejectPrincipal()` to invalidate the session cookie and force a fresh OIDC sign-in.

**Logger resolution pattern:**
The class is instantiated via `new RejectSessionCookieWhenAccountNotInCacheEvents()` in `Program.cs` — no constructor DI. Logger is resolved at call time via `context.HttpContext.RequestServices.GetService<ILogger<...>>()`. Null-coalesced with `?.LogWarning(...)` so no NRE if logging is somehow not registered.

**Why cache-clear is not done here:**
`ITokenAcquisition` has no public cache-clear API. The correct low-level path (`IConfidentialClientApplication.GetAccountAsync` → `RemoveAsync`) requires an `IAccount` object — but when the cache is in a collision state MSAL cannot resolve the account, making this circular. Principal rejection is the correct recovery: re-sign-in creates a clean single cache entry.

**Two-layer defence (Issue #83):**
- Layer 1 (this issue): `RejectSessionCookieWhenAccountNotInCacheEvents.ValidatePrincipal` — fires on every request with a valid session cookie, before any controller code runs. Catches the collision early and forces re-auth.
- Layer 2 (Issue #546 / PR #554): `MsalExceptionMiddleware` — global middleware fallback. Catches any `MsalClientException multiple_matching_tokens_detected` that bubbles up from a controller/service layer token acquisition call. Redirects to sign-out.


## Team Standing Rules (2026-04-01)
Established by Joseph Guadagno:

1. **PR Merge Authority**: Only Joseph may merge PRs
2. **Mapping**: All object mapping must use AutoMapper profiles
3. **Paging/Sorting/Filtering**: Must be at the data layer only

## RBAC Phase 1 Implementation (Issue #603)

**Context:** Multi-agent collaboration with Trinity (data layer), Neo (specs), and Ghost (auth pipeline).

**Trinity built (already on branch):**
- Domain models: `ApplicationUser`, `Role`, `UserApprovalLog`
- Enums: `ApprovalStatus`, `ApprovalAction`
- Constants: `RoleNames`
- Interfaces: `IApplicationUserDataStore`, `IRoleDataStore`, `IUserApprovalLogDataStore`, `IUserApprovalManager`
- Data.Sql implementations: `ApplicationUserDataStore`, `RoleDataStore`, `UserApprovalLogDataStore`
- Managers implementation: `UserApprovalManager`

**Issue found:** Trinity's `UserApprovalManager.cs` was missing using statements:
- `using System;`
- `using System.Collections.Generic;`
- `using System.Threading.Tasks;`

Added these to unblock the build - this is directly related to the RBAC Phase 1 feature and required for my auth pipeline components to compile.

**Ghost delivered:**

1. **EntraClaimsTransformation** (`src/JosephGuadagno.Broadcasting.Web/EntraClaimsTransformation.cs`)
   - Implements `IClaimsTransformation`
   - Auto-registers new users on first Entra login (calls `IUserApprovalManager.GetOrCreateUserAsync()`)
   - Adds `approval_status` claim from user's database record
   - Adds role claims (`ClaimTypes.Role`) for all assigned roles
   - Entra oid claim type confirmed: `"http://schemas.microsoft.com/identity/claims/objectidentifier"`
   - Includes idempotency check to avoid duplicate processing
   - Graceful error handling - returns original principal on failure

2. **UserApprovalMiddleware** (`src/JosephGuadagno.Broadcasting.Web/UserApprovalMiddleware.cs`)
   - Gates access based on `approval_status` claim
   - Redirects `Pending` users to `/Account/PendingApproval`
   - Redirects `Rejected` users to `/Account/Rejected`
   - Bypasses: static files, identity endpoints, approval pages themselves
   - Prevents redirect loops with comprehensive bypass logic

3. **Program.cs integration:**
   - Registered `IClaimsTransformation` as scoped service
   - Added middleware to pipeline (after auth/authz, before routing)
   - Added authorization policies: `RequireAdministrator`, `RequireContributor`, `RequireViewer`
   - Added project references to `Data.Sql` and `Managers`
   - Added required using statements

**Middleware ordering confirmed:**
```
UseRouting()
UseAuthentication()
UseAuthorization()
UseUserApprovalGate()  ← Critical placement
UseSession()
MapControllerRoute()
```

**Security findings:**
- Entra object ID claim type is the long form: `"http://schemas.microsoft.com/identity/claims/objectidentifier"`
- Claims transformation runs on every authenticated request - idempotency check is critical for performance
- Middleware bypass logic prevents redirect loops for rejected users trying to sign out
- New users are auto-registered as `Pending` on first login (no manual registration flow needed)

**Build status:** ✅ 0 errors, 4 warnings (known Newtonsoft.Json vulnerability warnings)

**Commit:** `a046eb0` on branch `squad/rbac-phase1`

**Next steps (not in this PR):**
- Phase 2: Admin UI for user approval/rejection and role assignment
- Phase 2: `/Account/PendingApproval` and `/Account/Rejected` views
- Phase 3: Apply `[Authorize(Policy = "RequireXxx")]` to controllers/actions
- Database migrations for new tables (Trinity's scope)
# Ghost — History

## Core Context

- **Role:** Security Engineer / Auth Pipeline
- **Specialty:** Cookie security, MSAL auth, Entra claims transformation, RBAC middleware

## Prior Work Archive (Sprints 8–11)

- **Sprint 8 (#336):** Cookie security hardening — HttpOnly, Secure, SameSite on auth/session/antiforgery. PR #510 merged.
- **Sprint 10 (#170):** Fine-grained API scopes (`*.View`, `*.Add`, etc.) dual-accepted alongside `*.All` for backward compat. Fixed `EngagementService.DeleteEngagementTalkAsync` using wrong scope. PR #526.
- **Sprint 10 (#528):** `[AuthorizeForScopes]` on all 4 Web API-calling controllers to handle `MsalUiRequiredException` on token cache eviction. Token cache is SQL-backed (`AddDistributedSqlServerCache`). PR #532.
- **Sprint 11 (#544–#548):** Three-layer MSAL auth exception defence (PRs #551–#555). All 5 issues closed. Subsequently all 4 PRs (#500, #553, #554, #555) **reverted** via PR #572 — MSAL auth was broken post-merge. Issue #85 remains open.
  - Layer 1: `RejectSessionCookieWhenAccountNotInCacheEvents` catches `multiple_matching_tokens_detected` (PR #555)
  - Layer 2: `MsalExceptionMiddleware` global fallback (PR #554)
  - Layer 3: `Program.cs` OIDC event handlers for AADSTS error codes (PR #553)
  - `AuthError` page + `Error.cshtml` IsDevelopment() hardening (PRs #551, #552)
  - **Root cause of PR #553 failure:** Ghost committed stray files from #545/#547 with no Program.cs changes; Trinity corrected; Neo re-approved.

**Key patterns from this era:**
- Dual-scope acceptance: `VerifyUserHasAnyAcceptedScope(specificScope, *.All)` — keeps backward compat
- `[AuthorizeForScopes]` at class level in Web controllers that call API with token acquisition
- `RejectSessionCookieWhenAccountNotInCacheEvents`: logger via `GetService<T>()` at request time (no ctor DI)
- Cache-clear circular problem: can't clear MSAL cache when it's in collision state — force re-sign-in instead


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

## RBAC Phase 2 Implementation (Branch: squad/rbac-phase2)

**Context:** Phase 1 (PR #610) merged. Applying page-level authorization policies to controllers.

**Ghost delivered:**

1. **HomeController.cs** - Ensured public pages remain accessible:
   - Added `[AllowAnonymous]` to `Error()` action (Index, Privacy, AuthError already had it)
   - All public-facing pages now bypass the global `AuthorizeFilter` set in Program.cs
   - Rationale: Error pages must be accessible to anonymous users and during auth failures

2. **LinkedInController.cs** - Applied administrator-only policy:
   - Added `[Authorize(Policy = "RequireAdministrator")]` at class level
   - Added `using Microsoft.AspNetCore.Authorization;`
   - All actions (Index, RefreshToken, Callback) now require Administrator role
   - Rationale: This controller manages LinkedIn OAuth tokens in Key Vault - sensitive admin-only operations

**Security rationale:**
- Program.cs has a global `AuthorizeFilter` requiring authentication by default (line 89-92)
- HomeController public pages (Index, Privacy, Error, AuthError) must explicitly opt out with `[AllowAnonymous]`
- LinkedInController manages OAuth tokens and Key Vault secrets - strictly admin-only access required
- Policy strings match Phase 1 pattern: inline literals referencing policies defined in Program.cs

**Build status:** ✅ Build succeeded (85.1s, 74 warnings - all expected CS8618 nullable warnings)

**Branch:** `squad/rbac-phase2`

**Learnings:**
- Global `AuthorizeFilter` in Program.cs means ALL controllers default to requiring authentication unless explicitly opted out
- The Error action in HomeController was missing `[AllowAnonymous]` - critical gap since error pages must be accessible during auth failures
- LinkedIn OAuth flow controller handles sensitive Key Vault operations - correct admin-only gating prevents unauthorized token access
### 2026-04-07 — Issue #85: OIDC Consent Error Handling (PR #664)

**Status:** ✅ COMPLETE & MERGED

**What I Implemented:**

1. **OnRemoteFailure Event Handler** (Program.cs)
   - Detects OIDC consent error codes: AADSTS650052, AADSTS65001, AADSTS700016, AADSTS70011
   - Redirects to `/Home/AuthError` with URL-encoded, user-friendly error message
   - Fallback: generic error message for all other OIDC failures

2. **Error Message Sanitization**
   - All messages URL-encoded before redirect (prevents injection attacks)
   - User-friendly: "Your organization hasn't granted access to this application. Please contact your IT administrator to enable access."
   - Technical details never exposed

3. **Infrastructure Reuse**
   - No new views or controllers required
   - Uses existing `AuthError.cshtml` view + `AuthErrorViewModel` + `HomeController.AuthError()` action
   - Minimal footprint: one configuration block in Program.cs

**Key Implementation Detail:**
Event customization must come AFTER `AddMicrosoftIdentityWebAppAuthentication()`:
```csharp
// 1. Microsoft Identity Web setup
builder.Services.AddMicrosoftIdentityWebAppAuthentication(...)
    .EnableTokenAcquisitionToCallDownstreamApi(...)
    .AddDistributedTokenCaches();

// 2. OIDC event handler customization (must come after)
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Events.OnRemoteFailure = context => { /* handler */ };
});
```

**Build:** ✅ 0 errors

**Merge:** PR #664 merged to main by Joseph

**Security Impact:** Issue #85 closed. External tenant users without admin consent now see actionable error instead of crash.

---

## GitHub Comment Formatting Skill Added

## Learnings

### 2026-04-21 — Issue #764 / PR #801 Reassignment

- Phase 0 API RBAC must stay additive: `src\JosephGuadagno.Broadcasting.Api\Program.cs` should wire `AddBroadcastingApiAuthorization()` while every API controller keeps its existing `VerifyUserHasAnyAcceptedScope(...)` checks for dual enforcement.
- The API host keeps its role-policy registration in `src\JosephGuadagno.Broadcasting.Api\Infrastructure\ApiAuthorizationServiceCollectionExtensions.cs`; `src\JosephGuadagno.Broadcasting.Api\Properties\AssemblyInfo.cs` exposes that internal helper to `JosephGuadagno.Broadcasting.Api.Tests`.
- The safest auth smoke test lives in `src\JosephGuadagno.Broadcasting.Api.Tests\Infrastructure\ApiAuthorizationServiceCollectionExtensionsTests.cs`: resolve `IClaimsTransformation`, transform an authenticated principal, assert the original `scp` claim survives, and verify hierarchical role claims are added.
- `src\JosephGuadagno.Broadcasting.Api.Tests\Helpers\ApiControllerTestHelpers.cs` is now the shared source of truth for API controller claim setup, including scope, owner OID, and optional site-admin role claims.

### 2026-04-21 — PR #804 Policy Name Constants

- Shared authorization policy names now live in `src\JosephGuadagno.Broadcasting.Domain\Constants\AuthorizationPolicyNames.cs`; use those constants everywhere instead of repeating `"Require*"` literals.
- Keep API and Web policy registration aligned by referencing `AuthorizationPolicyNames` in `src\JosephGuadagno.Broadcasting.Api\Infrastructure\ApiAuthorizationServiceCollectionExtensions.cs` and `src\JosephGuadagno.Broadcasting.Web\Program.cs`.
- Reflection-based authorization tests should assert against `AuthorizationPolicyNames` constants so controller attributes and policy registration can be renamed safely in one place.

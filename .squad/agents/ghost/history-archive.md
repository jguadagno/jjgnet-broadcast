# Ghost — History Archive

Archived auth/security context.

---

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

### 2026-04-21 — PR #805 Revision Under Reviewer Lockout

- Before rewriting a stacked Phase 2 branch, verify the prerequisite merge on `origin/main` with `git fetch`, `git merge-base HEAD origin/main`, and the current `origin/main` head; Neo's blocker on PR #805 was stale once PR #804 appeared on the fetched remote.
- Keep recovery work isolated in `\_worktrees\issue-766-api-test-role-claims` so a dirty root workspace and reviewer-lockout reassignment do not leak unrelated changes onto the PR branch.
- For API RBAC test migration, `src\JosephGuadagno.Broadcasting.Api.Tests\Helpers\ApiControllerTestHelpers.cs` should seed only role and owner-OID claims, while `src\JosephGuadagno.Broadcasting.Api.Tests\Infrastructure\ApiAuthorizationServiceCollectionExtensionsTests.cs` remains the single place that still asserts `scp` preservation through claims transformation.

### 2026-05-01 — MI.Web v4 OID Claim Mapping Fix

- **Root cause:** `Microsoft.Identity.Web` v2+ (v4.8.0 specifically) uses `JsonWebTokenHandler` for JWT bearer token validation. Unlike the legacy `JwtSecurityTokenHandler`, `JsonWebTokenHandler` does **not** perform claim type mapping from short RFC names to XML schema URIs. The `oid` claim in a real Entra access token therefore arrives as `"oid"`, not `"http://schemas.microsoft.com/identity/claims/objectidentifier"`.
- **Effect:** `EntraClaimsTransformation` looked only for the URI form, so `objectIdClaim` was always null in the API's JWT bearer context → no role claims added → all `[Authorize(Policy = "RequireXxx")]` checks returned 403. The Web app was unaffected because `AddMicrosoftIdentityWebApp` uses the OIDC handler which **does** map claims in the ID token / cookie identity.
- **Fix pattern:** Check both forms with null-coalescing: `FindFirst(EntraObjectId) ?? FindFirst(EntraObjectIdShort)`. Always check the URI form first (for OIDC/cookie contexts where mapping is performed), then fall back to the short form (for JWT bearer contexts).
- **Files changed:**
  - `src\JosephGuadagno.Broadcasting.Domain\Constants\ApplicationClaimTypes.cs` — added `EntraObjectIdShort = "oid"` constant
  - `src\JosephGuadagno.Broadcasting.Managers\EntraClaimsTransformation.cs` — updated OID lookup to check both forms
  - `src\JosephGuadagno.Broadcasting.Api.Tests\Infrastructure\ApiAuthorizationServiceCollectionExtensionsTests.cs` — added short-form OID test
  - `src\JosephGuadagno.Broadcasting.Web.Tests\EntraClaimsTransformationTests.cs` — added short-form OID test

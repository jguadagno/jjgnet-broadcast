---
name: "api-rbac-foundation"
description: "Register shared claims transformation and hierarchical role policies in the API without breaking existing scope enforcement."
domain: "authentication"
confidence: "high"
source: "earned"
---

## Context
Use this when the API host needs role-based authorization infrastructure before controller actions are migrated away from scope checks.

## Patterns
- Keep Phase 0 additive: register role infrastructure in the host, but do not remove `VerifyUserHasAnyAcceptedScope(...)` calls yet.
- Put API-specific RBAC wiring in a small infrastructure extension (for example `AddBroadcastingApiAuthorization()`) so `Program.cs` stays thin and tests can verify the real registration path.
- Register `IClaimsTransformation` to the shared `JosephGuadagno.Broadcasting.Managers.EntraClaimsTransformation` implementation rather than duplicating claims logic in the API host.
- Mirror the Web host’s cumulative role chain exactly:
  - `RequireSiteAdministrator` → Site Administrator
  - `RequireAdministrator` → Site Administrator, Administrator
  - `RequireContributor` → Site Administrator, Administrator, Contributor
  - `RequireViewer` → Site Administrator, Administrator, Contributor, Viewer
- Add a smoke-level test that resolves `IClaimsTransformation` from API DI, transforms an authenticated principal, confirms role claims are added, and confirms the original `scp` claim is still present.

## Examples
- Host wiring: `src\JosephGuadagno.Broadcasting.Api\Program.cs`
- Registration helper: `src\JosephGuadagno.Broadcasting.Api\Infrastructure\ApiAuthorizationServiceCollectionExtensions.cs`
- Verification tests: `src\JosephGuadagno.Broadcasting.Api.Tests\Infrastructure\ApiAuthorizationServiceCollectionExtensionsTests.cs`

## Anti-Patterns
- Removing scope checks during the same change that introduces role infrastructure.
- Re-implementing `EntraClaimsTransformation` inside the API host.
- Defining policy names or role ordering that diverges from the Web host.

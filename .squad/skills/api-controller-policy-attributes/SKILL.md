---
name: "api-controller-policy-attributes"
description: "Migrate API controller scope checks to framework-enforced authorization policies while preserving ownership checks."
domain: "authorization"
confidence: "high"
source: "earned"
---

## Context
Use this when an ASP.NET Core API controller currently calls `VerifyUserHasAnyAcceptedScope(...)` inside actions and the host already has hierarchical authorization policies registered.

## Patterns
- Keep the controller's class-level `[Authorize]` attribute so authentication still gates every action.
- Replace in-method scope checks with action-level `[Authorize(Policy = ...)]` attributes.
- Use the cumulative role chain consistently:
  - `RequireViewer` for list/view/get/read actions
  - `RequireContributor` for add/create/update/modify actions
  - `RequireAdministrator` for delete actions
- Leave ownership checks (`GetOwnerOid()`, `IsSiteAdministrator()`, per-resource owner comparisons) unchanged; policy migration should not broaden data visibility.
- After migration, remove controller `using Microsoft.Identity.Web.Resource;` and any resource-specific scope references that are no longer needed.
- Add a reflection-based test that asserts the expected policy per action so Phase 1 can verify the migration without rewriting direct controller behavior tests.

## Examples
- Controllers: `src\JosephGuadagno.Broadcasting.Api\Controllers\EngagementsController.cs`, `SchedulesController.cs`, `SocialMediaPlatformsController.cs`, `UserPublisherSettingsController.cs`, `MessageTemplatesController.cs`
- Tests: `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\ControllerAuthorizationPolicyTests.cs`

## Anti-Patterns
- Removing class-level `[Authorize]` while adding policy attributes.
- Mixing policy attributes with leftover `VerifyUserHasAnyAcceptedScope(...)` calls in the same controller.
- Folding ownership logic changes into the policy migration phase.

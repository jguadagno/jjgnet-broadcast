---
name: "per-user-settings-api-crud"
description: "Build per-user API CRUD controllers that stamp owner OIDs from claims, preserve partial updates, and fit the existing Publishers/Collectors route conventions."
domain: "api"
confidence: "high"
source: "earned"
---

# Per-user Settings API CRUD

## Context

Use this when adding a new per-user settings controller in the API project for
 a domain model that stores `CreatedByEntraOid` and belongs to the current
 authenticated user.

## Patterns

- Put publisher-owned endpoints under the `Publishers/...` route family and
  collector-owned endpoints under `Collectors/...`.
- Keep class-level `[Authorize]`, `[IgnoreAntiforgeryToken]`, and
  `[Produces("application/json")]` on API controllers.
- Use `RequireViewer` for GET actions and `RequireContributor` for POST, PUT,
  and DELETE actions.
- On create, map from a create DTO, then stamp
  `CreatedByEntraOid = User.GetOwnerOid()` in the controller instead of
  accepting owner IDs from the request body.
- On item GET, PUT, and DELETE actions, load the existing record first and
  return `Forbid()` when `CreatedByEntraOid` does not match the caller and the
  caller is not a site administrator.
- Use separate create and update DTOs when required-ness differs.
- For update DTO mappings, configure AutoMapper with conditional member mapping
  so null source values do not overwrite existing persisted values.
- Return `CreatedAtAction(...)` from POST actions and `NoContent()` from
  successful DELETE actions.
- Recalculate onboarding after successful create, update, and delete operations
  for user-owned publisher and collector settings.
- Add reflection coverage in `ControllerAuthorizationPolicyTests.cs` and
  controller unit tests for happy path, empty list, not found, and ownership
  enforcement.

## Examples

- Controllers:
  `src\JosephGuadagno.Broadcasting.Api\Controllers\Publishers\UserRandomPostSettingsController.cs`,
  `UserEventPublisherMappingController.cs`
- DTOs and mapping:
  `src\JosephGuadagno.Broadcasting.Api\Dtos\UserPublisherSettingsDtos.cs`,
  `src\JosephGuadagno.Broadcasting.Api\MappingProfiles\ApiBroadcastingProfile.cs`
- Tests:
  `src\JosephGuadagno.Broadcasting.Api.Tests\Controllers\UserRandomPostSettingsControllerTests.cs`,
  `UserEventPublisherMappingControllerTests.cs`

## Anti-Patterns

- Accepting `CreatedByEntraOid` from client payloads.
- Using a single request DTO for both create and partial update when PUT must
  preserve omitted optional fields.
- Returning `404` instead of `403` after a successful load that fails ownership
  checks.
- Forgetting to sanitize owner OIDs or other user-controlled strings in log
  messages.
- Using `Task.WhenAll` across manager calls that may share the scoped
  `BroadcastingContext`.

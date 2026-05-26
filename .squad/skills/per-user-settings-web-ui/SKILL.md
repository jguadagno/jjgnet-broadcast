---
name: "per-user-settings-web-ui"
description: "Build Web MVC CRUD for per-user publisher settings using downstream API services, shared option metadata, and UTC-safe datetime-local forms."
domain: "web"
confidence: "high"
source: "earned"
---

# Per-user Settings Web UI

## Context

Use this when adding a Web MVC management UI for a user-owned settings resource
that already has authenticated API CRUD endpoints.

## Patterns

- Create `I...Service` interfaces in `Web/Interfaces` and matching
  `...Service` HTTP-client wrappers in `Web/Services`; do not inject managers
  directly into Web controllers.
- Put publisher settings pages under the `Publishers/...` route family so the
  URLs align with the existing Publisher Settings area.
- Use `RequireViewer` for read actions and `RequireContributor` for create,
  edit, and delete actions.
- Add `[ValidateAntiForgeryToken]` to every Web `POST` action.
- Resolve platform dropdowns and display names through
  `ISocialMediaPlatformService` rather than hard-coding platform lists.
- Centralize event-type labels and icons in a shared constant/helper so views
  and controllers use the same mapping.
- For editable `DateTimeOffset` fields, render a `datetime-local` input for the
  browser and submit a hidden UTC field populated with JavaScript.
- Use `<local-time>` for read-only timestamps so list, details, and delete views
  display stored UTC values in the user's local time.
- Add Web controller unit tests for index/create flows and service tests for the
  downstream API wrappers.

## Examples

- Controllers:
  `src/JosephGuadagno.Broadcasting.Web/Controllers/UserRandomPostSettingsController.cs`,
  `UserEventPublisherMappingController.cs`
- Services:
  `src/JosephGuadagno.Broadcasting.Web/Services/UserRandomPostSettingsService.cs`,
  `UserEventPublisherMappingService.cs`
- Shared metadata:
  `src/JosephGuadagno.Broadcasting.Web/Constants/PublisherEventTypes.cs`
- Tests:
  `src/JosephGuadagno.Broadcasting.Web.Tests/Controllers/UserRandomPostSettingsControllerTests.cs`,
  `UserEventPublisherMappingControllerTests.cs`

## Anti-Patterns

- Injecting `I...Manager` into the Web project as a shortcut around the service
  layer.
- Hard-coding platform dropdown values or collector icon classes in individual
  views.
- Binding `datetime-local` directly to stored UTC values without a browser-local
  conversion step.
- Forgetting to repopulate dropdown options when validation fails and the form
  is redisplayed.

# Switch: Decisions for S4-4-UI MessageTemplate Management

**Date:** 2025-07-11
**Author:** Switch (Frontend Engineer)
**Branch:** `feature/s4-4-ui-message-template-management`

## Summary

Implemented the controller, ViewModel, service interface, and service layer for the MessageTemplates management UI.

## Decisions

### 1. Service layer over direct DataStore injection

The Web project communicates with the API via HTTP client services (same pattern as `EngagementService`, `ScheduledItemService`). `IMessageTemplateDataStore` was NOT injected directly into the Web controller because the Web project has no DB context registration — it talks to the API. Instead:

- Created `IMessageTemplateService` in `Web/Interfaces/`
- Created `MessageTemplateService : ServiceBase` in `Web/Services/`
- Registered via `services.TryAddScoped<IMessageTemplateService, MessageTemplateService>()` in `Program.cs`

### 2. Added UpdateAsync to IMessageTemplateDataStore and MessageTemplateDataStore

The existing interface only had `GetAsync` and `GetAllAsync`. Added:

```csharp
Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate);
```

Implementation uses `FirstOrDefaultAsync` (no `AsNoTracking`) on the composite PK, mutates `Template` and `Description`, then calls `SaveChangesAsync`.

### 3. New API MessageTemplatesController

Added `src/JosephGuadagno.Broadcasting.Api/Controllers/MessageTemplatesController.cs` with:
- `GET /messagetemplates` — GetAllAsync
- `GET /messagetemplates/{platform}/{messageType}` — GetAsync
- `PUT /messagetemplates/{platform}/{messageType}` — UpdateAsync

Injects `IMessageTemplateDataStore` directly (no manager layer needed for this simple entity). Uses `Domain.Scopes.MessageTemplates.All` for authorization.

### 4. Added MessageTemplates scope

Added `Scopes.MessageTemplates` class with `All = "MessageTemplates.All"` in `Domain/Scopes.cs`. Updated `AllAccessToDictionary` to include this scope so the Web's MSAL token acquisition requests it.

### 5. Web MessageTemplatesController actions

- `Index()` — GET, lists all templates (no route params)
- `Edit(string platform, string messageType)` — GET, renders edit form
- `Edit(MessageTemplateViewModel model)` — POST, saves and redirects to Index on success

On save failure, re-renders the edit form with a `ModelState` error (consistent with other controllers).

### 6. AutoMapper in WebMappingProfile

Added bidirectional mappings:
```csharp
CreateMap<Models.MessageTemplateViewModel, Domain.Models.MessageTemplate>();
CreateMap<Domain.Models.MessageTemplate, Models.MessageTemplateViewModel>();
```
All properties are 1:1 — no custom `ForMember` calls needed.

### 7. No Delete action

The task scope is Index (list) + Edit (update template body). Delete is intentionally excluded — templates are seeded configuration data, not user-created records. Adding/removing templates requires a DB seed change.

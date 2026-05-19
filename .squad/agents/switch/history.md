# Switch — History

## Summary

Switch (Web/Frontend Developer) implements the ASP.NET MVC Web layer with Razor views, controllers, and Web-layer services. Primary focus: RBAC Phase 1/2 UI (user approval, role management), add-platform flows, form handling, and authorization enforcement. Key work includes EngagementService (maps API DTOs to Domain models), Web-layer ViewModels (prevents Domain model references in Web project), CSRF protection (@Html.AntiForgeryToken on all POST forms), double-submit prevention (button disable via site.js), and self-demotion guards. Established pattern: create Web-specific ViewModels using AutoMapper, consume API responses through explicit contract types, validate on server-side before calling managers, and enforce authorization at controller level with [RequireAdministrator]/[RequireContributor] attributes. Works closely with Trinity (API contracts), Tank (Web integration tests), and Sparks (UI refinements). Notable: Switch maintains separation of concerns by never allowing Web layer to reference Domain models directly, always mapping through ViewModels. Key decision: Web services act as adapters between controllers and API, handling both request payload construction and response DTO-to-ViewModel mapping. Pattern: double-check authorization boundaries when adding new forms—verify both GET (show form) and POST (submit form) enforce appropriate roles.

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Frontend Engineer
### 2026-04-14T00-30-00Z — Issue #708: Web Service Contract Audit
- **Task:** Audit `EngagementService.AddPlatformToEngagementAsync` after manual testing still failed in the downstream API call path.
- **Outcome:** ✅ Web-side contract hardening complete.
- **What I found:**
  - The Web controller/form flow was already correct; the remaining Web risk was the service depending on anonymous request payloads and direct Domain-model deserialization for the add-platform endpoints.
  - The API now returns a created resource shape (`EngagementSocialMediaPlatformResponse`) and a nested `SocialMediaPlatform` DTO, so I made the Web service consume that response shape explicitly instead of assuming Domain-model JSON.
- **What I changed:**
  - `src\JosephGuadagno.Broadcasting.Web\Services\EngagementService.cs`
    - Added explicit internal request/response contract types for engagement-platform API calls.
    - Mapped API DTO-shaped responses into Domain models before returning to controllers.
    - Applied the same contract mapping to both `GetPlatformsForEngagementAsync` and `AddPlatformToEngagementAsync`.
  - `src\JosephGuadagno.Broadcasting.Web.Tests\Services\EngagementServiceTests.cs`
    - Added/updated service-level tests to verify the request payload, endpoint path, and DTO→Domain mapping.
- **Testing:** `dotnet build .\src\ --no-restore --configuration Release` ✅ and `dotnet test .\src\JosephGuadagno.Broadcasting.Web.Tests\JosephGuadagno.Broadcasting.Web.Tests.csproj --no-build --configuration Release` ✅ (149 passing).

### 2026-04-14T00-00-00Z — Issue #708: Web Audit on social-media-708social-media-708
- **Task:** Audit the Web-side add-platform flow after manual testing still reported HttpRequestException/BadRequest.
- **Outcome:** ✅ Web audit complete; no additional Web-layer fix was needed.
- **What I verified:**
  - `site.js` already uses button-click disabling, so the earlier double-submit race fix is present.
  - `_Layout.cshtml` already renders `TempData["WarningMessage"]`, so warning UX support is present.
  - `EngagementsController.AddPlatform(EngagementSocialMediaPlatformViewModel vm)` already uses the ViewModel-only POST pattern, so the old route/model-binding mismatch is not the active Web issue.
  - The AddPlatform Razor form posts the ViewModel payload expected by the current controller and service flow.
- **Assessment:** If a valid single submit still saves the association and then ends in BadRequest/HttpRequestException, the remaining fault is downstream of Web (API response/contract behavior), not the Razor/JS form flow.
- **Testing:** Focused Issue #708 coverage still passed — 9 Web tests and 10 API platform tests.

### 2026-04-13T17-34-54Z — Issue #708: Double-Submit Race Condition Fix
- **Task:** Fix actual duplicate-submit path for issue #708
- **Outcome:** ✅ Complete
- **Changes:**
  - `src/JosephGuadagno.Broadcasting.Web/wwwroot/js/site.js` (lines 8-26)
  - Moved button disable from form submit event to button click event
  - Rationale: Click event fires BEFORE form submit, preventing race condition
  - Validation-aware: Checks client validation BEFORE disabling
  - Pattern: All future forms automatically protected via site.js
- **Testing:** All 147 Web.Tests pass; build clean
- **Decisions documented:** `switch-real-fix-708.md` (double-submit prevention), `switch-708-conflict-handling.md` (409 handling)
- **Team:** Coordinated with Tank (regression tests) and Trinity (backend validation)
- **Status:** Ready for merge. Complements Tank's regression coverage and Trinity's backend 409 handling.

## Learnings

### 2026-05-18 — CollectorIcons constants class

- When replacing hard-coded icon strings in views, do a repo-wide search first — instances were found in `_LoginPartial.cshtml`, `SyndicationFeedItems/Index.cshtml`, and `YouTubeItems/Index.cshtml` beyond the originally scoped files.
- `_Layout.cshtml` does not inherit from `_ViewImports.cshtml` automatically; a `@using` directive must be added at the top of the file when referencing non-default namespaces.
- The `ByMessageType` dictionary pattern on the constants class is the right approach for Razor views that need to map a string key to (Icon, Label) — keeps the fallback logic out of the view.

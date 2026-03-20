# Morpheus — History

## Core Context

- **Project:** A .NET broadcasting application using Azure Functions, ASP.NET Core API/MVC, SQL Server, and Azure infrastructure to collect and distribute social media content.
- **Role:** Data Engineer
- **Joined:** 2026-03-14T16:37:57.749Z

## Learnings

### 2025-01-XX — PR #512 Review Fixes
- **Task:** Fixed blocking issues in Trinity's PR #512 (feature/s8-315-api-dtos)
- **Issue 1 (BOM):** Removed UTF-8 BOM from MessageTemplatesController.cs line 1 using PowerShell UTF8Encoding without BOM
- **Issue 2 (Route-as-ground-truth):** Removed `EngagementId` property from `TalkRequest` DTO per team decision that route parameters are authoritative and should not be duplicated in request body. The `ToModel` method in EngagementsController already injects `engagementId` from the route parameter.
- **Verification:** Build passed with only expected NU1903 and CS8618 warnings (tracked, safe to ignore)
- **Pattern:** DTOs should NOT include fields that come from route parameters — the controller mapping layer injects them at the call site

<!-- Append learnings below -->

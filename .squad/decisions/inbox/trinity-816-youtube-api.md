# Decision: YouTubeSourcesController API Endpoints (Issue #816)
**Date:** 2026-04-24  
**Author:** Trinity  
**Status:** Implemented — PR #825

## Context
Issue #816 requested full CRUD API endpoints for `YouTubeSource`. The `EngagementsController` was established as the canonical pattern for ownership-aware API controllers.

## Decisions

### 1. Followed EngagementsController pattern exactly
Same class-level attributes (`[ApiController]`, `[Authorize]`, `[IgnoreAntiforgeryToken]`, `[Route("[controller]")]`), same `GetOwnerOid()` / `IsSiteAdministrator()` private helpers, same admin-bypass logic on list endpoints.

### 2. No PUT endpoint
Issue #816 specified only GET (list + single), POST, and DELETE. No update endpoint was requested. This matches the issue specification and can be added in a follow-up issue.

### 3. AddedOn and LastUpdatedOn set in controller on POST
The domain model requires both fields. Since the data store sets these on insert, they are set in the controller before calling `SaveAsync()` as a belt-and-suspenders approach. This ensures the values are populated even if the data store logic is revised.

### 4. Tags mapped with null-coalescing
`YouTubeSourceRequest.Tags` is `IList<string>?` (optional). AutoMapper maps it as `s.Tags ?? new List<string>()` to ensure the domain model's `IList<string>` never receives null.

### 5. DI registrations added to Program.cs
`IYouTubeSourceDataStore` and `IYouTubeSourceManager` were not previously registered in the API's DI container. Both added with `TryAddScoped`.

### 6. Authorization policy tests extended
`ControllerAuthorizationPolicyTests` now covers all four `YouTubeSourcesController` actions with their expected policies (RequireViewer × 2, RequireContributor × 1, RequireAdministrator × 1).

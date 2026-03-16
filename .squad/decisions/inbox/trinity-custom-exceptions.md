# Decision: Custom Exception Types for Social Managers (Issue #273)

**Date:** 2026-03-16
**Author:** Trinity (Backend Dev)
**Applies to:** Facebook Manager, LinkedIn Manager, Domain

## What was done

Introduced a typed exception hierarchy to replace generic `ApplicationException` and `HttpRequestException` throws in the social media manager classes.

### New Types

| Type | Location | Purpose |
|------|----------|---------|
| `BroadcastingException` | `Domain/Exceptions/` | Abstract base for all broadcasting-related exceptions. Carries optional `ApiErrorCode` and `ApiErrorMessage` properties. |
| `FacebookPostException` | `Managers.Facebook/Exceptions/` | Thrown by `FacebookManager` on API or deserialization failures. |
| `LinkedInPostException` | `Managers.LinkedIn/Exceptions/` | Thrown by `LinkedInManager` on API or deserialization failures. |

## Decisions Made

### 1. Base exception lives in Domain
`BroadcastingException` is placed in the `Domain` project so it can be referenced by any layer (API, Functions, Web) that needs to catch platform-specific errors without coupling to individual manager assemblies.

### 2. Domain reference added to both manager projects
`Managers.Facebook` and `Managers.LinkedIn` did not previously reference `Domain`. References were added via `dotnet add reference` to enable the inheritance chain.

### 3. `ArgumentNullException` throws left unchanged
Parameter validation guards that throw `ArgumentNullException` were intentionally left as-is — those represent programming errors (invalid call-site contract), not API failures.

### 4. All `HttpRequestException` and `ApplicationException` throws in the managers replaced
Every API-failure throw site in both managers was updated to the typed exception. This includes `ExecuteGetAsync`, `CallPostShareUrl`, `GetUploadResponse`, and `UploadImage` in `LinkedInManager`, and both methods in `FacebookManager`.

### 5. `throw;` re-throws left unchanged
Bare `throw;` statements in catch blocks remain as-is — they preserve the original stack trace and are correct.

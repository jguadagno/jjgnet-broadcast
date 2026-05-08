---
name: "http-status-aware-error-handling"
description: "Graceful HTTP error handling in Web controllers with status-code-specific user messaging"
domain: "error-handling, web-ux"
confidence: "high"
source: "earned (Issue #708 fix)"
---

## Context

ASP.NET Core Web controllers calling downstream APIs via HttpClient should differentiate HTTP error responses based on status code, providing appropriate user feedback:
- **4xx client errors:** User-actionable messages (validation, conflict, not found)
- **5xx server errors:** Technical error messages with details for support
- **409 Conflict (idempotency):** Warning-level feedback, not error-level

This applies when:
- Web layer calls API via IDownstreamApi or HttpClient
- Operations can return 409 Conflict for "already done" scenarios (duplicate resource, idempotent retry)
- User experience should distinguish between "this is fine" vs "something broke"

## Patterns

### 1. Check HttpRequestException.StatusCode

```csharp
try
{
    var result = await _service.AddResourceAsync(id, data);
    if (result is null)
    {
        TempData["ErrorMessage"] = "Failed to add resource.";
    }
    else
    {
        TempData["SuccessMessage"] = "Resource added successfully.";
    }
}
catch (HttpRequestException ex)
{
    // Differentiate based on status code
    if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
    {
        TempData["WarningMessage"] = "This resource is already associated.";
    }
    else if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        TempData["ErrorMessage"] = "The target resource was not found.";
    }
    else
    {
        TempData["ErrorMessage"] = $"Failed to add resource: {ex.Message}";
    }
}
```

### 2. Support TempData Warning Messages in Layout

Ensure `_Layout.cshtml` (or equivalent) displays warning-level messages:

```html
@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert">
        @TempData["SuccessMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
@if (TempData["WarningMessage"] != null)
{
    <div class="alert alert-warning alert-dismissible fade show" role="alert">
        @TempData["WarningMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
@if (TempData["ErrorMessage"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show" role="alert">
        @TempData["ErrorMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
```

### 3. Test Both Happy and Error Paths

```csharp
[Fact]
public async Task Action_When409Conflict_RedirectsWithWarningMessage()
{
    // Arrange
    var conflictException = new HttpRequestException(
        "Conflict",
        null,
        System.Net.HttpStatusCode.Conflict);
    _service.Setup(s => s.AddAsync(1, 2))
        .ThrowsAsync(conflictException);

    // Act
    var result = await _controller.Add(viewModel);

    // Assert
    var redirectResult = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("This resource is already associated.", _controller.TempData["WarningMessage"]);
}
```

## Examples

### Real-world case: Issue #708 AddPlatform

**Problem:** Adding a social media platform to an engagement returns 409 if already associated. Original code showed generic error.

**Solution:**
- EngagementsController.AddPlatform POST catches HttpRequestException
- 409 → `TempData["WarningMessage"] = "This platform is already associated with this engagement."`
- Other errors → `TempData["ErrorMessage"]` with technical details

**Files:**
- `src/JosephGuadagno.Broadcasting.Web/Controllers/EngagementsController.cs` (lines 250-280)
- `src/JosephGuadagno.Broadcasting.Web/Views/Shared/_Layout.cshtml` (lines 148-161)
- `src/JosephGuadagno.Broadcasting.Web.Tests/Controllers/EngagementsControllerTests.cs` (AddPlatform tests)

## Anti-Patterns

### ❌ Treat all HttpRequestException the same
```csharp
catch (HttpRequestException ex)
{
    TempData["ErrorMessage"] = $"Failed: {ex.Message}"; // ← No differentiation
}
```
**Problem:** User can't tell if retry is safe or if something is broken.

### ❌ Suppress 409 entirely
```csharp
if (ex.StatusCode == HttpStatusCode.Conflict)
{
    // Silent ignore ← User has no feedback
}
```
**Problem:** User doesn't know the operation was a no-op.

### ❌ Treat 409 as success
```csharp
if (ex.StatusCode == HttpStatusCode.Conflict)
{
    TempData["SuccessMessage"] = "Resource added successfully."; // ← Misleading
}
```
**Problem:** User thinks resource was just added when it was already there.

## Guidelines

1. **409 Conflict:** Use warning-level message ("This resource is already..."). Safe to retry, benign outcome.
2. **404 Not Found (on add/update):** Error-level message. Parent resource missing or ID invalid.
3. **400 Bad Request:** Error-level message with details. Validation or contract issue.
4. **500 Server Error:** Error-level message. Log details, show generic user message.

## HTTP Status Code Reference for Web Controllers

| Status Code | Level   | User Message Example                          | Retry Safe? |
|-------------|---------|-----------------------------------------------|-------------|
| 200/201/204 | Success | "Resource added successfully."                | N/A         |
| 400         | Error   | "Invalid request: {details}"                  | No (fix data) |
| 404         | Error   | "Resource not found."                         | No          |
| 409         | Warning | "This resource is already associated."        | Yes (no-op) |
| 422         | Error   | "Validation failed: {details}"                | No (fix data) |
| 500         | Error   | "An error occurred. Please try again later."  | Maybe       |

# Skill: CodeQL Security Baseline

## Purpose

Prevent recurring CodeQL security alerts from entering the codebase. Two checks
are treated as hard gates on every PR:

- `cs/web/missing-token-validation` — CSRF protection for Web MVC POST actions
- `cs/log-forging` — Log injection from unsanitized user-controlled strings

---

## CSRF (`cs/web/missing-token-validation`)

### Rule

Every `[HttpPost]` method in the **Web** project must have `[ValidateAntiForgeryToken]`.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(MyViewModel model) { ... }
```

### API controllers are exempt

API controllers use Bearer token authentication and are not vulnerable to CSRF.
They must carry `[IgnoreAntiforgeryToken]` at the **class** level:

```csharp
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("[controller]")]
public class MyApiController : ControllerBase { ... }
```

Do **not** add `[ValidateAntiForgeryToken]` to API controllers — it will break
token-based flows.

### Checklist when adding a Web POST action

- [ ] `[HttpPost]` present
- [ ] `[ValidateAntiForgeryToken]` present on the same method (or at class level)
- [ ] The corresponding Razor view includes `@Html.AntiForgeryToken()` or uses
  `asp-action` tag helpers (which inject the token automatically)

---

## Log injection (`cs/log-forging`)

### Rule

Never pass user-controlled values directly to `_logger.Log*()`. Sanitize first:

```csharp
private static string SanitizeForLog(string? value) =>
    value?.Replace("\r", string.Empty).Replace("\n", string.Empty) ?? string.Empty;
```

### What counts as user-controlled

- Route parameters (`[FromRoute]`)
- Query string parameters (`[FromQuery]`)
- Request body fields (`[FromBody]`, model properties)
- Any string derived from the above

### What is safe to log without sanitization

- Integer IDs
- Enum values
- Config values loaded at startup
- Exception messages (use structured logging with `{Exception}` parameter)
- Counts, totals, boolean flags

### Pattern

```csharp
// UNSAFE
_logger.LogInformation("Updated platform={Platform}", platform);

// SAFE
_logger.LogInformation("Updated platform={Platform}", SanitizeForLog(platform));
```

Add `SanitizeForLog()` as a private static method in each controller that needs
it. Do not centralize unless explicitly asked.

---

## Pre-commit validation checklist

Before committing any controller change:

1. Search for `[HttpPost]` in the file — every one must have `[ValidateAntiForgeryToken]`
   (Web only).
2. Search for `_logger.Log` in the file — every call that passes a string variable
   must use `SanitizeForLog()` if that variable could come from user input.
3. Run `dotnet build src/` — must pass.
4. Run `dotnet test src/ --no-build --configuration Release` — all tests must pass.

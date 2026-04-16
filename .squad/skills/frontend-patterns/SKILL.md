# SKILL: Frontend Patterns

**Applicable to:** Web layer JavaScript (site.js, page-specific scripts)  
**Last Updated:** 2026-04-11  
**Maintainer:** Sparks (Frontend Developer)

## Event Handler Best Practices

### Always Prevent Default Explicitly

When writing event listeners that conditionally block browser default behavior, **always** accept the `event` parameter and call `preventDefault()` before early returns.

**❌ WRONG:**
```javascript
form.addEventListener('submit', function () {
    if (someCondition) return;  // ⚠️ Form still submits!
    // ... handler logic
});
```

**✅ CORRECT:**
```javascript
form.addEventListener('submit', function (event) {
    if (someCondition) {
        event.preventDefault();  // ✅ Form submission blocked
        return;
    }
    // ... handler logic
});
```

### Why It Matters

- Early returns without `preventDefault()` don't stop the browser's default action
- This causes issues like:
  - Double-submits on forms (Issue #708)
  - Unintended navigation when clicking disabled links
  - Form submission when validation fails client-side

### Common Event Types Requiring preventDefault()

1. **Form Submit** - Block duplicate submissions or invalid forms
2. **Link Click** - Prevent navigation for disabled/loading states
3. **Keypress** - Block Enter key in certain inputs
4. **Drag/Drop** - Override browser default file handling

### Pattern in site.js (Global Form Handler)

The project's global form submit handler (site.js) implements double-submit prevention. **IMPORTANT:** Use button click event, NOT form submit event, to prevent race conditions.

**❌ WRONG - Race Condition:**
```javascript
form.addEventListener('submit', function (event) {
    if (btn.disabled) {
        event.preventDefault();
        return;
    }
    btn.disabled = true;  // ⚠️ Too late! Second click already queued submit event
});
```

**✅ CORRECT - Click Event:**
```javascript
btn.addEventListener('click', function (event) {
    if (btn.disabled) {
        event.preventDefault();  // Block if already disabled
        return;
    }
    
    // Check client validation BEFORE disabling
    if (typeof $ !== 'undefined' && $(form).valid && !$(form).valid()) {
        return;  // Let validation run, don't disable
    }
    
    btn.disabled = true;  // Disable immediately on first click
    btn.innerHTML = '<span>...</span>Saving...';  // Visual feedback
});
```

**Why Click Event Prevents Race:**
- Click event fires BEFORE form submit event
- Button disables on FIRST click, preventing second click from queuing another submit
- Validation check runs before disable, so invalid forms don't stay disabled

**Key Points:**
- Use button click event, not form submit event
- Check client validation BEFORE disabling button
- Disable happens atomically on first click (no race window)
- Re-enables button on validation errors (invalid-form.validate event)

## Form UX Patterns

### Submit Button States

1. **Default State:** Enabled, shows action text (e.g., "Save", "Add Platform")
2. **Submitting State:** Disabled, shows spinner + "Saving..."
3. **Validation Error State:** Re-enabled via jQuery Validate's `invalid-form.validate` event

### Visual Feedback

Use Bootstrap spinner for loading states:
```javascript
btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Saving...';
```

### Preserving Original Content

Store original button HTML to restore on error:
```javascript
btn.dataset.originalHtml = btn.innerHTML;  // Save before change
// ... later, on error:
btn.innerHTML = btn.dataset.originalHtml;  // Restore
delete btn.dataset.originalHtml;
```

## Integration with jQuery Validation

The project uses jQuery Unobtrusive Validation. Listen for validation failures to reset button state:

```javascript
$(form).on('invalid-form.validate', function () {
    // Restore button if it was disabled for submit
    if (btn.dataset.originalHtml) {
        btn.innerHTML = btn.dataset.originalHtml;
        delete btn.dataset.originalHtml;
        btn.disabled = false;
    }
});
```

## References

- **Issue #708:** Double-submit race condition fix (click event vs submit event)
- **File:** `JosephGuadagno.Broadcasting.Web/wwwroot/js/site.js`
- **Decision:** `.squad/decisions/inbox/switch-real-fix-708.md` (2026-04-13)
- **Pattern:** Button click event for double-submit prevention (atomic disable before form submit fires)

## Razor Forms and Model Binding

### Prefer ViewModel-Only POST Actions When the Form Already Has the Data

If the posted ViewModel already contains every value the action needs, keep the POST signature to the ViewModel alone. Do not duplicate the same value as a separate simple parameter unless routing truly requires it.

**✅ PREFERRED:**
```razor
@model EngagementSocialMediaPlatformViewModel

<form asp-action="AddPlatform" method="post">
    <input type="hidden" asp-for="EngagementId" />
    <!-- ... -->
</form>
```

Controller:
```csharp
[HttpPost]
public async Task<IActionResult> AddPlatform(EngagementSocialMediaPlatformViewModel vm)
{
    // ✅ Single source of truth: vm.EngagementId
}
```

### When Route Parameters Are Actually Needed

Use `asp-route-*` only when the action signature includes a separate simple parameter that is not coming solely from the ViewModel, for example:

```csharp
[HttpPost]
public async Task<IActionResult> Edit(int id, EngagementViewModel vm)
```

In that case, the route value is part of action matching and should stay in the form or URL generation.

### Quick Rule

- **ViewModel has everything + action accepts only ViewModel** → use hidden fields/model binding, no duplicate route parameter needed.
- **Action has separate simple parameters** → provide matching route values.
- **Seeing a 400 after a successful save** → inspect downstream API response generation/contract issues before blaming Razor form markup.

### Related Issues

- **Issue #708:** Current AddPlatform POST pattern is `AddPlatform(EngagementSocialMediaPlatformViewModel vm)`, so the active Web-side form uses ViewModel-only posting.
- **Decision:** `.squad/decisions.md` entries `trinity-708-model-binding-pattern` and `trinity-issue-708-createdataction`
- **Files:** `src\JosephGuadagno.Broadcasting.Web\Controllers\EngagementsController.cs`, `src\JosephGuadagno.Broadcasting.Web\Views\Engagements\AddPlatform.cshtml`

## Web Service Contract Adapters

### Prefer Explicit Internal Contract Types at the MVC/API Boundary

When a Web service in `JosephGuadagno.Broadcasting.Web\Services` talks to the API, prefer explicit internal request/response types that mirror the API payload over anonymous request objects or direct Domain-model deserialization.

**✅ PREFERRED:**
```csharp
var request = new EngagementSocialMediaPlatformApiRequest
{
    SocialMediaPlatformId = socialMediaPlatformId,
    Handle = handle
};

var response = await apiClient.PostForUserAsync<EngagementSocialMediaPlatformApiRequest, EngagementSocialMediaPlatformApiResponse>(
    ApiServiceName,
    request,
    options => options.RelativePath = $"/engagements/{engagementId}/platforms");

return response is null ? null : MapPlatform(response);
```

### Why This Pattern Helps

- Keeps Web resilient when API DTOs and Domain models are similar but not identical.
- Makes nested response-shape expectations explicit (`SocialMediaPlatform`, paging DTOs, etc.).
- Gives tests a stable seam to verify path, payload, and mapping behavior without depending on anonymous reflection tricks.

### Use It When

- The Web layer calls the API through `IDownstreamApi`.
- The API returns DTOs rather than Domain models.
- The controller only needs Domain models after the service boundary.

### Related Issue

- **Issue #708:** `EngagementService` now adapts `EngagementSocialMediaPlatformResponse` payloads into Domain models instead of assuming direct Domain JSON.


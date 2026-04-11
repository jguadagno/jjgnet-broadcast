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

The project's global form submit handler (site.js) implements double-submit prevention:

```javascript
form.addEventListener('submit', function (event) {
    if (btn.disabled) {
        event.preventDefault();  // Block repeat submits
        return;
    }
    btn.disabled = true;  // Disable for this submit
    btn.innerHTML = '<span>...</span>Saving...';  // Visual feedback
});
```

**Key Points:**
- Checks disabled state to catch rapid clicks
- Prevents default BEFORE returning
- Provides visual feedback (spinner + "Saving...")
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

- **Issue #708:** Double-submit bug fix (root cause and solution)
- **File:** `JosephGuadagno.Broadcasting.Web/wwwroot/js/site.js`
- **Decision:** `.squad/decisions/inbox/sparks-708-double-submit-fix.md`

## Razor Forms and Model Binding

### Route Parameters Required for Controller Action Matching

When a controller POST action has simple-type parameters (int, string, guid) that are NOT properties of the ViewModel, those parameters MUST be included in the route. Without them, ASP.NET Core routing cannot match the action, causing HTTP 400 Bad Request.

**❌ WRONG:**
```razor
@model EngagementSocialMediaPlatformViewModel

<!-- Missing route parameter - causes 400 error! -->
<form asp-action="AddPlatform" method="post">
    <input type="hidden" asp-for="EngagementId" />
    <!-- ... -->
</form>
```

Controller:
```csharp
[HttpPost]
public async Task<IActionResult> AddPlatform(int engagementId, EngagementSocialMediaPlatformViewModel vm)
{
    // ⚠️ Form POSTs to /Engagements/AddPlatform (no engagementId in route)
    // ⚠️ ASP.NET Core expects route like /Engagements/AddPlatform/5
    // ⚠️ Route doesn't match → HTTP 400
}
```

**✅ CORRECT:**
```razor
@model EngagementSocialMediaPlatformViewModel

<!-- Route parameter required for action matching -->
<form asp-action="AddPlatform" asp-route-engagementId="@Model.EngagementId" method="post">
    <input type="hidden" asp-for="EngagementId" />
    <!-- ... -->
</form>
```

Controller:
```csharp
[HttpPost]
public async Task<IActionResult> AddPlatform(int engagementId, EngagementSocialMediaPlatformViewModel vm)
{
    // ✅ Form POSTs to /Engagements/AddPlatform/5
    // ✅ Route matches action signature
    // ✅ engagementId = 5 (from route), vm.EngagementId = 5 (from POST body)
}
```

### Route vs. Model Binding: Different Purposes

Having BOTH `asp-route-X` and a matching ViewModel property is **NOT a conflict**:
- **Route parameter**: Used by ASP.NET Core routing to match the controller action
- **Model property**: Used by controller logic to access the value from the ViewModel
- Both mechanisms are independent and don't interfere with each other

### When to Use Route Parameters vs Model Properties

**Use route parameters (asp-route-*) when:**
- Controller action signature has simple-type parameters (int, string) that are not ViewModel properties
- The parameter is required for route matching
- Example: `Action(int id, ViewModelType model)`

**Use model properties (hidden fields) when:**
- The value is part of the ViewModel and used in controller logic
- The value must be validated with other form fields
- Example: `vm.EngagementId` used to verify data integrity

**Use BOTH when:**
- The controller action signature is `Action(int routeParam, ViewModelType vm)` where `vm` has a property matching `routeParam`
- The route parameter satisfies routing requirements
- The model property satisfies business logic requirements

### Related Issues

- **Issue #708:** AddPlatform form returned 400 when route parameter was removed
- **Decision:** `.squad/decisions/inbox/sparks-708-route-parameter-correction.md` (SUPERSEDES previous incorrect decision)
- **File:** `JosephGuadagno.Broadcasting.Web/Views/Engagements/AddPlatform.cshtml`


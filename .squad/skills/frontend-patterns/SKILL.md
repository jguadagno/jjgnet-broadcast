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

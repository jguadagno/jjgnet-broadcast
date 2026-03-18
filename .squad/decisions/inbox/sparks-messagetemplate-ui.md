# Sparks: Decisions for S4-4-UI MessageTemplate Views

**Date:** 2025-07-11
**Author:** Sparks (Frontend Developer)
**Branch:** `feature/s4-4-ui-message-template-management`

## Summary

Implemented Razor views and nav entry for the MessageTemplates management UI.

## Decisions

### 1. Index view: grouped table by Platform

Templates are rendered as one Bootstrap `table-striped table-hover` per platform, with an `<h4>` heading for each group. Sorted by Platform then MessageType for predictable order. This is clearer than a flat table with a Platform column because the 4×5 matrix is small and logically organized by platform.

### 2. Template truncation with Bootstrap tooltip

The template body can be long. Index shows first 80 chars with `…` and the full template in a `title` / `data-bs-toggle="tooltip"` attribute. Bootstrap tooltips are initialized via a small vanilla JS snippet in `@section Scripts` — no new dependencies.

### 3. Edit view: two-column layout

Used Bootstrap `row g-4` / `col-lg-8` + `col-lg-4`:
- Left: the edit form (Platform, MessageType as read-only text inputs, Description, Template textarea)
- Right: Scriban variable reference card (`card border-info`)

The variable reference panel documents `title`, `url`, `description`, `tags`, `image_url` with availability notes per item type, derived from `TryRenderTemplateAsync` in the Functions project.

### 4. Template textarea: monospace, 6 rows

Used `style="font-family: monospace; font-size: 0.9em;"` inline on the `<textarea>` — consistent with the task spec and keeps it simple without adding a CSS class. Placeholder text shows example Scriban syntax.

### 5. Scriban syntax in the reference panel uses Razor escaping

Scriban `{{ variable }}` conflicts with Razor syntax. Used `{{ "{{" }} variable {{ "}}" }}` to safely render the double-braces in the HTML without Razor attempting to interpret them.

### 6. Nav link placement

Added "Message Templates" as a plain `nav-item` between the Schedules dropdown and Privacy, matching the existing nav item style. A simple link (not a dropdown) is sufficient since there is only one page under this section (Index, with Edit reachable via row button).

### 7. No new JS dependencies

All interactivity (tooltip initialization) uses Bootstrap 5's built-in JS that is already loaded by `_Layout.cshtml`. No additional scripts or LibMan entries needed.

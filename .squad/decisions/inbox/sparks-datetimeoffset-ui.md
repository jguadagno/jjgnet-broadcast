# Sparks: DateTimeOffset Timezone-Aware Display in Web UI

**Date:** 2026-03-18
**Branch:** `feature/datetimeoffset-consistency`
**Author:** Sparks (Frontend Developer)

## Summary

All `DateTimeOffset` values in the Web UI are now displayed in the **browser's local timezone** rather than as raw UTC strings. This work was originally delivered in PR #213 (`feat: add local time display to all DateTimeOffset views in Web project`) and is now confirmed consistent with the `feature/datetimeoffset-consistency` branch where Morpheus completed the domain/data layer audit.

---

## Approach

### 1. `LocalTimeTagHelper` (`TagHelpers/LocalTimeTagHelper.cs`)

A custom ASP.NET Core Tag Helper that renders a `<time>` element carrying:
- `datetime` attribute — ISO 8601 string (`"o"` format) for JavaScript consumption
- `data-local-time` attribute — either `"date"` or `"datetime"` (controlled by the `date-only` parameter)
- Inner text — server-side fallback using `"d"` (short date) or `"f"` (full date/time) format specifiers

```html
<!-- Razor source -->
<local-time value="@Model.SendOnDateTime" />

<!-- Rendered HTML -->
<time datetime="2026-03-18T14:30:00+00:00" data-local-time="datetime">Tuesday, March 18, 2026 2:30 PM</time>
```

### 2. Client-Side Conversion (`wwwroot/js/site.js`)

A small `DOMContentLoaded` listener queries all `time[data-local-time]` elements and replaces their text content with the browser-locale string using the built-in `Date` constructor and `toLocaleString()` / `toLocaleDateString()`. No external libraries.

### 3. `_Layout.cshtml` Integration

`site.js` is already referenced globally at the bottom of `_Layout.cshtml` via `<script src="~/js/site.js" asp-append-version="true"></script>`, so all pages automatically get timezone conversion.

---

## Views Updated

All display views use `<local-time>` — **no raw `.ToString()` calls** remain on datetime fields in any view.

| View | Fields |
|------|--------|
| `Schedules/Index.cshtml` | `SendOnDateTime` |
| `Schedules/Upcoming.cshtml` | `SendOnDateTime` |
| `Schedules/Unsent.cshtml` | `SendOnDateTime` |
| `Schedules/Calendar.cshtml` | `SendOnDateTime` |
| `Schedules/Details.cshtml` | `SendOnDateTime`, `MessageSentOn` |
| `Schedules/Delete.cshtml` | `SendOnDateTime` |
| `Engagements/Index.cshtml` | `StartDateTime`, `EndDateTime`, `LastUpdatedOn` (date-only) |
| `Engagements/Details.cshtml` | `StartDateTime`, `EndDateTime`, `CreatedOn`, `LastUpdatedOn`, nested talk times |
| `Engagements/Edit.cshtml` | Nested talk `StartDateTime`, `EndDateTime` |
| `Engagements/Delete.cshtml` | `StartDateTime`, `EndDateTime` |
| `Talks/Details.cshtml` | `StartDateTime`, `EndDateTime` |
| `Talks/Delete.cshtml` | `StartDateTime`, `EndDateTime` |

Add/Edit forms use `<input type="datetime-local">` (native browser date/time picker) — no change needed there.

---

## Decisions

### 1. Tag Helper over inline spans
Used a reusable Tag Helper (`<local-time value="...">`) rather than copy-pasting `<span class="local-time" data-utc="...">` inline in every view. This keeps views clean and the ISO 8601 serialization logic in one place.

### 2. `<time>` element with `datetime` attribute
Used the semantic HTML `<time>` element with the standard `datetime` attribute (not `data-utc`). This is both semantically correct and accessible.

### 3. Server-side fallback text
The server renders a human-readable fallback (`"f"` or `"d"` format) inside the `<time>` element. If JavaScript is disabled or slow to load, users still see a meaningful date/time string (in UTC/server timezone).

### 4. `toLocaleString()` / `toLocaleDateString()` — no `Intl.DateTimeFormat` options
Kept the JS simple with no explicit locale options. The browser uses the user's system locale for formatting. This matches the broadest range of user preferences without over-specifying.

### 5. No `datetime-local.js` — used `site.js` instead
The suggested `datetime-local.js` approach was folded into the existing `site.js` to avoid adding a redundant script reference to `_Layout.cshtml`. `site.js` is already globally included.

---

## Coordination Note

- Morpheus confirmed (on the same branch) that all SQL and domain model datetime fields are `DateTimeOffset` — no conversions or casts are needed server-side.
- The `"o"` round-trip format specifier in C# produces strings like `2026-03-18T14:30:00+00:00`, which the browser `Date` constructor parses correctly.

# Switch: Calendar Widget — FullCalendar.js for Speaking Engagements

**Date:** 2026-07-14
**Author:** Switch (Frontend Engineer)
**Branch:** `feature/calendar-widget`
**Issue:** Calendar placeholder replaced per squad tasking

---

## What Was Done

Replaced the `<!-- TODO: Add real calender -->` placeholder in `Views/Schedules/Calendar.cshtml`
with a functional FullCalendar.js month-view calendar that displays speaking engagements fetched
asynchronously from a new JSON endpoint.

---

## Where the Calendar View Lives

`src/JosephGuadagno.Broadcasting.Web/Views/Schedules/Calendar.cshtml`

Served by `SchedulesController.Calendar(int? year, int? month)` at route `/Schedules/Calendar`.
The existing navigation link in `_Layout.cshtml` (Schedules → Calendar) continues to work
unchanged.

---

## Controller Action Added for JSON Events

**File:** `src/JosephGuadagno.Broadcasting.Web/Controllers/EngagementsController.cs`

```csharp
[HttpGet]
public async Task<JsonResult> GetCalendarEvents()
```

**Route:** `GET /Engagements/GetCalendarEvents`

Returns a JSON array in FullCalendar's native event format:

```json
[
  {
    "id": "42",
    "title": "Conference Name",
    "start": "2026-05-15T09:00:00",
    "end": "2026-05-16T18:00:00",
    "url": "https://..."
  }
]
```

Data sourced from `IEngagementService.GetEngagementsAsync()` (all engagements, no date filter —
FullCalendar shows the relevant month and users can navigate freely).

**Rationale for placement in EngagementsController:** The data is engagement data; putting the
endpoint on `EngagementsController` keeps data access co-located with the domain. The Calendar
view (in Schedules) simply fetches from this endpoint.

---

## LibMan Entry Added

**File:** `src/JosephGuadagno.Broadcasting.Web/libman.json`

```json
{
  "library": "fullcalendar@6.1.15",
  "destination": "wwwroot/libs/fullcalendar",
  "files": ["index.global.min.js"]
}
```

**Notes:**
- Provider: `jsdelivr` (project default)
- Only `index.global.min.js` is needed — FullCalendar 6's global build auto-injects its own CSS
  at runtime (no separate `.css` file ships in the npm package).
- `wwwroot/libs/` is in `.gitignore`; LibMan restores at dev setup via `libman restore`.

---

## Layout Change

**File:** `src/JosephGuadagno.Broadcasting.Web/Views/Shared/_Layout.cshtml`

Added `@await RenderSectionAsync("Styles", required: false)` inside `<head>` (after `site.css`).
This enables per-page `@section Styles { }` blocks. The Calendar view uses this to set a
`max-width` on the `#calendar` container.

---

## Design Decisions

1. **All engagements, no date filter** — `GetCalendarEvents` returns all engagements. FullCalendar
   handles display by month; users navigate with prev/next. A future enhancement could add
   `start`/`end` query params to filter server-side if the dataset grows large.

2. **JS only, no Razor model rendering** — The Calendar view no longer renders server-side event
   data. The `@model List<ScheduledItemViewModel>?` declaration is kept for controller
   compatibility (the `Calendar` action still passes the model) but the view ignores it.

3. **Two calendar views** — `dayGridMonth` (default) and `listYear` are exposed via the header
   toolbar. List view is useful for scanning upcoming talks by date.

4. **Event click → new tab** — Engagement URLs open in a new browser tab, keeping the app open.

5. **No jQuery dependency** — FullCalendar 6 global build is vanilla JS; no additional framework
   needed beyond what's already on the page.

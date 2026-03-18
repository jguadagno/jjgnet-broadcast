# Sparks: ImageUrl Field Added to ScheduledItem Views (Issue #269)

**Date:** 2025-07-11
**Branch:** `issue-269`
**Author:** Sparks (Frontend Developer)

## Summary

Added `ImageUrl` as an optional form field to both the Add and Edit views for ScheduledItems.

## Files Changed

| File | Change |
|------|--------|
| `src/JosephGuadagno.Broadcasting.Web/Models/ScheduledItemViewModel.cs` | Added `public string? ImageUrl { get; set; }` with `[Url]` and `[Display(Name = "Image URL")]` annotations |
| `src/JosephGuadagno.Broadcasting.Web/Views/Schedules/Add.cshtml` | Added `ImageUrl` form field after the `Message` field |
| `src/JosephGuadagno.Broadcasting.Web/Views/Schedules/Edit.cshtml` | Added `ImageUrl` form field after the `Message` field |

## ViewModel Change

Added to `ScheduledItemViewModel`:

```csharp
[Url]
[Display(Name = "Image URL")]
public string? ImageUrl { get; set; }
```

- `[Url]` provides client-side and server-side URL format validation
- `[Display(Name = "Image URL")]` drives the label rendered by `asp-for`
- Nullable (`string?`) — field is optional, no `[Required]`

## AutoMapper — No Changes Needed

`WebMappingProfile` maps `ScheduledItemViewModel` ↔ `Domain.Models.ScheduledItem` via `CreateMap`. Both have a property named `ImageUrl`, so AutoMapper maps it by convention. No explicit `.ForMember()` call was needed.

## Form Layout

The field appears between **Message** and **Sent on Date/Time** in both views:

```html
<div class="mb-3">
    <label asp-for="ImageUrl" class="form-label"></label>
    <input asp-for="ImageUrl" type="url" class="form-control" placeholder="https://example.com/image.jpg" />
    <span asp-validation-for="ImageUrl" class="text-danger"></span>
</div>
```

- Uses `type="url"` for native browser URL validation hint
- Placeholder: `https://example.com/image.jpg`
- Label text rendered from `[Display(Name = "Image URL")]` via `asp-for`
- Validation span for unobtrusive client-side error display
- No new JS dependencies

## Build Result

`Build succeeded. 0 Error(s)` — all pre-existing warnings only (CS8618 nullable, unrelated to this change).

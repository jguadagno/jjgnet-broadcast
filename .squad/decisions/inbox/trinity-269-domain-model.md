# Trinity Decisions: ScheduledItem Domain Model (Issue #269)

## Date
2026-03-17

## Summary
Added `MessageTemplate` and `ImageUrl` properties to the `ScheduledItem` domain model and EF Core
data layer to match the two SQL columns Morpheus added in the same branch.

## Files Changed

| File | Change |
|------|--------|
| `src/JosephGuadagno.Broadcasting.Domain/Models/ScheduledItem.cs` | Added `public string? MessageTemplate { get; set; }` and `public string? ImageUrl { get; set; }` after `Message` |
| `src/JosephGuadagno.Broadcasting.Data.Sql/Models/ScheduledItem.cs` | Added `public string MessageTemplate { get; set; }` and `public string ImageUrl { get; set; }` (nullable disabled file — nullable context is `#nullable disable`) |
| `src/JosephGuadagno.Broadcasting.Data.Sql/BroadcastingContext.cs` | Added `entity.Property(e => e.ImageUrl).HasMaxLength(2048)` to the `ScheduledItem` EF configuration block |

## Design Choices

### 1. No separate API DTOs exist for ScheduledItem
`SchedulesController` uses `Domain.Models.ScheduledItem` directly in all endpoints — there are no
request/response DTO classes to update. Adding properties to the Domain model is sufficient to
keep the API contract in sync.

### 2. AutoMapper — no changes needed
`BroadcastingProfile` maps `Data.Sql.Models.ScheduledItem` ↔ `Domain.Models.ScheduledItem` by
convention. Because both models now carry `MessageTemplate` and `ImageUrl` with matching names,
AutoMapper resolves them automatically. No explicit `ForMember` calls were added.

### 3. `MessageTemplate` — no max-length EF config
`NVARCHAR(MAX)` is the SQL type (per Morpheus decision). EF Core maps an unconstrained `string`
property to `NVARCHAR(MAX)` by default, so no `.HasMaxLength()` call is needed (and adding one
would cause a mismatch).

### 4. `ImageUrl` — `.HasMaxLength(2048)` added to EF config
Matches the `NVARCHAR(2048)` column Morpheus defined. Placed in the existing `ScheduledItem` entity
configuration block in `OnModelCreating`.

### 5. Web ViewModel not touched
`JosephGuadagno.Broadcasting.Web.Models.ScheduledItemViewModel` was intentionally left unchanged.
The Web layer update is next in the pipeline (Sparks). Touching it here would create a merge risk.

### 6. Build result
`dotnet build` — **Build succeeded, 0 errors**. All warnings are pre-existing CS8618 / CS8602
nullable reference warnings unrelated to this change.

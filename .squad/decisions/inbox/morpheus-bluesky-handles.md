# Schema Decision: BlueSkyHandle on Engagements and Talks

**Date:** 2026-03-21
**Author:** Morpheus (Data Engineer)
**Issues:** #167 (Engagement BlueSkyHandle), #166 (Scheduled Talk BlueSkyHandle)
**PR:** #523

## Decision

Added `BlueSkyHandle NVARCHAR(255) NULL` to both the `dbo.Engagements` and `dbo.Talks` tables.

## Column Spec

| Table        | Column        | Type            | Nullable | Max Length |
|--------------|---------------|-----------------|----------|------------|
| Engagements  | BlueSkyHandle | NVARCHAR(255)   | YES      | 255        |
| Talks        | BlueSkyHandle | NVARCHAR(255)   | YES      | 255        |

## Rationale

- **Nullable:** No existing rows have a BlueSky handle. Making it nullable is the only backward-compatible choice.
- **NVARCHAR(255):** BlueSky handles follow the format `@user.bsky.social` (max ~253 chars). 255 is consistent with other handle/name columns in this schema.
- **Both tables:** An engagement (conference/event) may have its own BlueSky account. A talk's speaker may have a different BlueSky handle than the event itself.

## Files Changed

- `scripts/database/table-create.sql` — base schema updated
- `scripts/database/migrations/2026-03-21-add-bluesky-handle.sql` — ALTER TABLE for existing databases
- `src/JosephGuadagno.Broadcasting.Domain/Models/Engagement.cs` — `public string? BlueSkyHandle { get; set; }`
- `src/JosephGuadagno.Broadcasting.Domain/Models/Talk.cs` — `public string? BlueSkyHandle { get; set; }`
- `src/JosephGuadagno.Broadcasting.Data.Sql/Models/Engagement.cs` — EF entity property added
- `src/JosephGuadagno.Broadcasting.Data.Sql/Models/Talk.cs` — EF entity property added
- `src/JosephGuadagno.Broadcasting.Data.Sql/BroadcastingContext.cs` — `HasMaxLength(255)` configured for both

## Follow-on Work

- **Trinity:** Update DTOs (`EngagementResponse`, `TalkRequest`/`TalkResponse`) to expose the field
- **Sparks:** Add BlueSkyHandle input fields to Engagement and Talk Add/Edit forms

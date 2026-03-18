# Morpheus Decisions: ScheduledItems New Columns + MessageTemplates Table (Issue #269)

## Date
2026-03-17 (revised)

## Summary
**Revised design**: `MessageTemplate` is NOT stored as a per-row column on `ScheduledItems`.
Instead, a dedicated `MessageTemplates` lookup table holds Scriban templates keyed by
`(Platform, MessageType)`. `ScheduledItems` retains only the new `ImageUrl` nullable column.

## Column / Table Definitions

### `ScheduledItems` change (kept)

| Column    | Type             | Nullable | Purpose                                               |
|-----------|------------------|----------|-------------------------------------------------------|
| `ImageUrl` | `NVARCHAR(2048)` | YES      | URL of an image to attach/embed in the broadcast post |

### New `MessageTemplates` table

| Column        | Type             | Nullable | Purpose                                                       |
|---------------|------------------|----------|---------------------------------------------------------------|
| `Platform`    | `NVARCHAR(50)`   | NO (PK)  | Social platform name, e.g. `Twitter`, `Facebook`, etc.        |
| `MessageType` | `NVARCHAR(50)`   | NO (PK)  | Message category, e.g. `RandomPost`                           |
| `Template`    | `NVARCHAR(MAX)`  | NO       | Scriban template string used to render the broadcast message  |
| `Description` | `NVARCHAR(500)`  | YES      | Human-readable description of what the template is for        |

Primary key: composite `(Platform, MessageType)`.

## Design Choices

### 1. Composite PK `(Platform, MessageType)` — not a surrogate int
Templates are looked up by exact `(Platform, MessageType)` pair at send time. Using those two
business-key columns as the PK eliminates a redundant surrogate key, makes look-up queries
self-documenting, and enforces at the DB layer that each platform+type combination is unique.

### 2. `NVARCHAR(MAX)` for `Template`
Scriban templates can be arbitrarily long (conditional blocks, loops, variable references).
Consistent with the existing `Message` column on `ScheduledItems`.

### 3. `MessageTemplate` removed from `ScheduledItems`
A per-row template column couples the template definition to each scheduled item, causing
proliferation and inconsistency. The lookup table is the single source of truth; all scheduled
items for a given platform pick up the same template automatically.

### 4. `ImageUrl` stays on `ScheduledItems` (`NVARCHAR(2048)`, nullable)
Image choice is genuinely per-item — it makes sense as a row-level attribute.
2048 characters is the de-facto safe upper limit for a URL, matching existing URL columns in
the codebase.

### 5. Seed data
Four default rows are inserted by the migration, one per platform, for `MessageType = 'RandomPost'`:

| Platform  | Template                                              |
|-----------|-------------------------------------------------------|
| Twitter   | `{{ title }} - {{ url }}`                             |
| Facebook  | `{{ title }}\n\n{{ description }}\n\n{{ url }}`       |
| LinkedIn  | `{{ title }}\n\n{{ description }}\n\n{{ url }}`       |
| Bluesky   | `{{ title }} - {{ url }}`                             |

### 6. Migration approach
The existing migration file `2026-03-17-scheduleditem-add-messagetemplate-imageurl.sql` was
revised in place (no new file needed — it was not yet applied to any environment). The
`ALTER TABLE … ADD MessageTemplate` statement was removed and replaced with the
`CREATE TABLE [dbo].[MessageTemplates]` DDL plus the 4 seed `INSERT` rows.

## Files Changed

| File | Change |
|------|--------|
| `scripts/database/table-create.sql` | Removed `MessageTemplate` from `ScheduledItems`; added `MessageTemplates` CREATE TABLE block |
| `scripts/database/migrations/2026-03-17-scheduleditem-add-messagetemplate-imageurl.sql` | Replaced `ADD MessageTemplate` ALTER TABLE with `CREATE TABLE MessageTemplates` + seed INSERTs; kept `ADD ImageUrl` |


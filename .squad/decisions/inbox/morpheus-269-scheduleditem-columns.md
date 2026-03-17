# Morpheus Decisions: ScheduledItems New Columns (Issue #269)

## Date
2026-03-17

## Summary
Added two nullable columns to the `dbo.ScheduledItems` table to support richer broadcast
scheduling: a Scriban template for the message and an optional image URL.

## Column Definitions

| Column            | Type             | Nullable | Purpose                                                   |
|-------------------|------------------|----------|-----------------------------------------------------------|
| `MessageTemplate` | `NVARCHAR(MAX)`  | YES      | Scriban template string used to render the broadcast message at send time |
| `ImageUrl`        | `NVARCHAR(2048)` | YES      | URL of an image to attach/embed in the broadcast post     |

## Design Choices

### 1. `MessageTemplate` — `NVARCHAR(MAX)`
Scriban templates can be arbitrarily long (they may embed conditional blocks, loops, and variable
references). Using `NVARCHAR(MAX)` is consistent with the existing `Message` column on the same
table and avoids any truncation risk. There is no meaningful upper bound to impose at the DB layer.

### 2. `ImageUrl` — `NVARCHAR(2048)`
2048 characters is the de-facto safe upper limit for a URL (respects IE/legacy limits and matches
common `nvarchar(2048)` conventions already used across the codebase for URL columns). This is more
constrained than `NVARCHAR(MAX)` which is appropriate given URLs are well-bounded in practice.

### 3. Both columns are nullable
Neither field is required for a valid scheduled item — the existing `Message` column continues to
carry the static broadcast text. `MessageTemplate` and `ImageUrl` are opt-in enhancements;
making them `NOT NULL` would break existing rows and force unnecessary defaults.

### 4. Migration approach
A new migration script was placed in `scripts/database/migrations/` following the pattern
established in issue #274 (`2026-03-16-scheduleditem-integrity.sql`). The `table-create.sql`
base schema was also updated so fresh database initializations include the columns.

## Files Changed

| File | Change |
|------|--------|
| `scripts/database/table-create.sql` | Added `MessageTemplate` and `ImageUrl` to the `ScheduledItems` CREATE TABLE block |
| `scripts/database/migrations/2026-03-17-scheduleditem-add-messagetemplate-imageurl.sql` | New migration; two `ALTER TABLE … ADD COLUMN` statements for live databases |

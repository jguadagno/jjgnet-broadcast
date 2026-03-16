# Decision: ScheduledItemType Enum (Issue #274)

**Author**: Trinity (Backend Dev)  
**Date**: 2025-07-11  
**Branch**: `issue-274`  
**Related todos**: `domain-enum`, `data-mapping`, `functions-enum`

## Summary

Added a `ScheduledItemType` enum to replace raw `string ItemTableName` usage in switch dispatching across all 4 Functions.

## Decisions

### 1. `ItemType` is the primary property; `ItemTableName` is computed

`Domain.Models.ScheduledItem` now has:
- `public ScheduledItemType ItemType { get; set; }` — the authoritative, type-safe property
- `public string ItemTableName => ItemType.ToString();` — computed, read-only, kept for backward-compat logging

**Rationale**: Keeps existing log statements (`scheduledItem.ItemTableName`) compiling without change, while making the switch in Functions fully type-safe. The DB column name (`ItemTableName`) is preserved in the EF entity (`Data.Sql.Models.ScheduledItem`) unchanged.

### 2. EF entity (`Data.Sql.Models.ScheduledItem`) unchanged

The SQL entity retains `public string ItemTableName { get; set; }`. The DB schema requires no migration.

### 3. AutoMapper handles string ↔ enum conversion

`BroadcastingProfile` uses `Enum.Parse<ScheduledItemType>` when mapping EF entity → Domain model, and `.ToString()` for the reverse. This is safe because the DB should only contain valid enum names; invalid values will throw at read time (fail-fast).

### 4. `WebMappingProfile` updated

`ScheduledItemViewModel.ItemTableName` (string) → `Domain.ScheduledItem.ItemType` (enum) via `Enum.Parse`, and back via `.ToString()`. The ViewModel itself is unchanged to avoid impacting Razor views.

### 5. All 4 Functions switch on `ScheduledItemType`

Twitter, Facebook, LinkedIn, and Bluesky `ProcessScheduledItemFired.cs` now switch on `scheduledItem.ItemType` using `ScheduledItemType` enum cases. `SourceSystems` constants are no longer used in the switch expressions but are not removed (they may have other usages).

### 6. Test data updated

All test files that set `ItemTableName` on `Domain.Models.ScheduledItem` (read-only after this change) were updated to set `ItemType = ScheduledItemType.SyndicationFeedSources` as a safe default where the specific type doesn't affect test logic.

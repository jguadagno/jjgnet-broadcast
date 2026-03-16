# Tank: Decisions for Issue #274 Test Suite

## Context
Writing unit tests for issue #274 — ScheduledItems Referential Integrity changes.

## Decisions

### 1. No new test project needed
All tests placed in the existing `JosephGuadagno.Broadcasting.Data.Sql.Tests` project. It already had the right dependencies (xUnit v3, Moq, AutoMapper, EF InMemory) and a `ScheduledItemDataStoreTests.cs` to pattern-match against.

### 2. GetOrphanedScheduledItemsAsync tested via Moq (not EF InMemory)
The concrete implementation uses `FromSqlRaw` which is not supported by the EF Core InMemory provider. Rather than spin up a real SQL Server instance, mock-based contract tests against `IScheduledItemDataStore` are used. This verifies the interface contract and return-value propagation without requiring infrastructure.

### 3. Fixed pre-existing test breakage
`ScheduledItemDataStoreTests.cs` had a `CreateScheduledItem` helper and several inline test items using `ItemTableName = "TestTable"` / `"T"`. After issue #274 changed `BroadcastingProfile` to call `Enum.Parse<ScheduledItemType>(source.ItemTableName)`, these values caused `ArgumentException` at runtime. All occurrences were updated to use `"Engagements"` (a valid enum value). This restored 5 pre-broken tests to green.

### 4. Assertion library: xUnit Assert (not FluentAssertions)
The `Data.Sql.Tests` csproj does not reference FluentAssertions. All assertions use the standard xUnit `Assert.*` API to stay consistent with the existing test files.

### 5. Three new test files created
- `ScheduledItemTypeTests.cs` — enum value coverage (D) + domain model computed property (A)
- `ScheduledItemMappingTests.cs` — AutoMapper bidirectional mapping coverage (B)
- `ScheduledItemOrphanTests.cs` — mock-based orphan detection contract tests (C)

## Result
122/122 tests passing in `Data.Sql.Tests`. Committed to `issue-274` branch.

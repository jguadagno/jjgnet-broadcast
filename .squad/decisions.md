
# Morpheus Decisions: Orphan Detection (Issue #274)

## Date
2026-03-16

## Decisions

### 1. SQL Migration file location
Created `scripts/database/migrations/2026-03-16-scheduleditem-integrity.sql`.
The existing pattern places one-off scripts in `scripts/database/`. A `migrations/` subdirectory
was created to distinguish idempotency-sensitive one-time scripts from the base schema.

### 2. Valid ItemTableName values
Valid values enforced by the new CHECK constraint:
- `Engagements`
- `Talks`
- `SyndicationFeedSources`
- `YouTubeSources`

Bad legacy values fixed in the migration:
- `SyndicationFeed` → `SyndicationFeedSources`
- `YouTube` → `YouTubeSources`

### 3. Orphan detection SQL strategy
Used conditional NOT EXISTS per table name rather than dynamic SQL, since the set of valid
table names is fixed and small. This keeps it readable, type-safe, and fast with indexed PKs.

### 4. Return type
`GetOrphanedScheduledItemsAsync()` returns `IEnumerable<Domain.Models.ScheduledItem>` to stay
consistent with the domain layer. EF entity results are mapped via AutoMapper (same pattern as
all other methods in ScheduledItemDataStore).

### 5. Raw SQL approach
Used `FromSqlRaw` on `broadcastingContext.ScheduledItems` because the join condition is
conditional on a string column value — this cannot be expressed cleanly in LINQ without
client-side evaluation. `FromSqlRaw` is the existing EF Core pattern for this scenario.

### 6. Trinity coordination note
Trinity is adding a `ScheduledItemType` enum and renaming `ItemTableName` → `ItemType` on the
Domain model. The orphan detection method uses the EF entity (which still stores the string
`ItemTableName` in the DB) and relies on the existing AutoMapper mapping to produce
`Domain.Models.ScheduledItem`. No changes to the mapping layer are needed from our side.

# Decision: No Raw SQL in ScheduledItemDataStore

**Date:** 2026-03-16
**Source:** PR #280 review comment by @jguadagno
**Applies to:** Morpheus, Trinity (any data store work)

## Rule

Do NOT use FromSqlRaw, ExecuteSqlRaw, or any hardcoded SQL strings in ScheduledItemDataStore (or any DataStore).
Use **Entity Framework Core LINQ queries** instead. When type-based dispatch is needed (e.g. per ScheduledItemType), use the enum directly in LINQ predicates.

## Example (correct approach for orphan detection)

Use EF DbSets with .Where() and .ContainsAsync() / HashSet membership — do not write raw SQL.

# Decision: UI Dropdown Value Fix (Issue #274)

**Date:** 2025-01-16  
**Author:** Sparks (Frontend Developer)

## Decision

Updated `ItemTableName` dropdown option values in Schedule Add/Edit views and the supporting JS switch statement to match the backend's expected table name strings.

## Changes Made

| File | Change |
|------|--------|
| `Views/Schedules/Add.cshtml` | `value="SyndicationFeed"` → `"SyndicationFeedSources"`, `value="YouTube"` → `"YouTubeSources"` |
| `Views/Schedules/Edit.cshtml` | Same as above |
| `wwwroot/js/schedules.edit.js` | Updated `case` strings to match new values |

## Rationale

Display labels are user-facing and remain unchanged ("Syndication Feed", "YouTube"). Only the submitted `value` attributes were corrected to align with what Azure Functions collectors expect when looking up items in table storage.

## Outcome

Build passes (0 errors). Committed on branch `issue-274`.

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

# Decision: Custom Exception Types for Social Managers (Issue #273)

**Date:** 2026-03-16
**Author:** Trinity (Backend Dev)
**Applies to:** Facebook Manager, LinkedIn Manager, Domain

## What was done

Introduced a typed exception hierarchy to replace generic `ApplicationException` and `HttpRequestException` throws in the social media manager classes.

### New Types

| Type | Location | Purpose |
|------|----------|---------|
| `BroadcastingException` | `Domain/Exceptions/` | Abstract base for all broadcasting-related exceptions. Carries optional `ApiErrorCode` and `ApiErrorMessage` properties. |
| `FacebookPostException` | `Managers.Facebook/Exceptions/` | Thrown by `FacebookManager` on API or deserialization failures. |
| `LinkedInPostException` | `Managers.LinkedIn/Exceptions/` | Thrown by `LinkedInManager` on API or deserialization failures. |

## Decisions Made

### 1. Base exception lives in Domain
`BroadcastingException` is placed in the `Domain` project so it can be referenced by any layer (API, Functions, Web) that needs to catch platform-specific errors without coupling to individual manager assemblies.

### 2. Domain reference added to both manager projects
`Managers.Facebook` and `Managers.LinkedIn` did not previously reference `Domain`. References were added via `dotnet add reference` to enable the inheritance chain.

### 3. `ArgumentNullException` throws left unchanged
Parameter validation guards that throw `ArgumentNullException` were intentionally left as-is — those represent programming errors (invalid call-site contract), not API failures.

### 4. All `HttpRequestException` and `ApplicationException` throws in the managers replaced
Every API-failure throw site in both managers was updated to the typed exception. This includes `ExecuteGetAsync`, `CallPostShareUrl`, `GetUploadResponse`, and `UploadImage` in `LinkedInManager`, and both methods in `FacebookManager`.

### 5. `throw;` re-throws left unchanged
Bare `throw;` statements in catch blocks remain as-is — they preserve the original stack trace and are correct.

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

# Twitter/Bluesky Exception Implementation Decisions

## Files Created
- `src/JosephGuadagno.Broadcasting.Managers.Twitter/Exceptions/TwitterPostException.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Bluesky/Exceptions/BlueskyPostException.cs`

## Files Modified
- `src/JosephGuadagno.Broadcasting.Managers.Twitter/TwitterManager.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Bluesky/BlueskyManager.cs`
- `src/JosephGuadagno.Broadcasting.Managers.Bluesky/JosephGuadagno.Broadcasting.Managers.Bluesky.csproj`

## Decisions

### TwitterManager
- `SendTweetAsync` now throws `TwitterPostException` instead of returning `null` on both null-tweet and exception paths
- Added a `catch (TwitterPostException) { throw; }` re-throw guard so the inner TwitterPostException created from a null tweet propagates cleanly through the outer catch

### BlueskyManager
- `Post()` now throws `BlueskyPostException` for both login failure and post failure paths, with HTTP status code and API error message captured via the `apiErrorCode`/`apiErrorMessage` constructor
- `DeletePost()` left unchanged — returns `false` on failure (boolean method, not a post operation)
- `GetEmbeddedExternalRecord()` thumbnail `HttpRequestException` catch left intentionally silent (per existing comment)
- Added `ProjectReference` to `JosephGuadagno.Broadcasting.Domain` in Bluesky csproj (was missing)

### BroadcastingException Wait
- Waited ~2 minutes for the base class to be created by the other Trinity agent before proceeding

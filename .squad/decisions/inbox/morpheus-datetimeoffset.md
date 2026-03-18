# Morpheus: DateTimeOffset Consistency (feature/datetimeoffset-consistency)

**Date:** 2026-03-18
**Branch:** `feature/datetimeoffset-consistency`
**Author:** Morpheus (Data Engineer)

## Summary

Audited all SQL datetime columns and C# model properties for timezone-aware (`DateTimeOffset`) consistency. The SQL schema was already fully `datetimeoffset`-consistent from prior migrations. Two C# model gaps were closed.

---

## SQL Schema Audit

**Result: No SQL changes needed.** Every point-in-time column in the schema already uses `DATETIMEOFFSET`. The schema was migrated to `DATETIMEOFFSET` during the initial table creation work (`2026-01-31-engagement-add-time-columns.sql`, `2026-02-04-move-from-table-storage.sql`).

### Confirmed DATETIMEOFFSET columns (nothing to change)

| Table | Column | Type | Notes |
|-------|--------|------|-------|
| `dbo.Engagements` | `StartDateTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.Engagements` | `EndDateTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.Engagements` | `CreatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.Engagements` | `LastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.Talks` | `StartDateTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.Talks` | `EndDateTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.ScheduledItems` | `SendOnDateTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.ScheduledItems` | `MessageSentOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.Cache` | `ExpiresAtTime` | `datetimeoffset` | ✅ Already correct |
| `dbo.Cache` | `AbsoluteExpiration` | `datetimeoffset` | ✅ Already correct |
| `dbo.FeedChecks` | `LastCheckedFeed` | `datetimeoffset` | ✅ Already correct |
| `dbo.FeedChecks` | `LastItemAddedOrUpdated` | `datetimeoffset` | ✅ Already correct |
| `dbo.FeedChecks` | `LastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.TokenRefreshes` | `Expires` | `datetimeoffset` | ✅ Already correct |
| `dbo.TokenRefreshes` | `LastChecked` | `datetimeoffset` | ✅ Already correct |
| `dbo.TokenRefreshes` | `LastRefreshed` | `datetimeoffset` | ✅ Already correct |
| `dbo.TokenRefreshes` | `LastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.SyndicationFeedSources` | `PublicationDate` | `datetimeoffset` | ✅ Already correct |
| `dbo.SyndicationFeedSources` | `AddedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.SyndicationFeedSources` | `ItemLastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.SyndicationFeedSources` | `LastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.YouTubeSources` | `PublicationDate` | `datetimeoffset` | ✅ Already correct |
| `dbo.YouTubeSources` | `AddedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.YouTubeSources` | `ItemLastUpdatedOn` | `datetimeoffset` | ✅ Already correct |
| `dbo.YouTubeSources` | `LastUpdatedOn` | `datetimeoffset` | ✅ Already correct |

### No DATE-only columns found
No `DATE`-only columns exist in the schema — all temporal columns already carry full timestamp + offset information.

---

## EF Core & Domain Model Audit

All `Data.Sql.Models.*` and `Domain.Models.*` classes that correspond to DB columns already used `DateTimeOffset`. No changes needed there.

---

## C# Model Changes Made

### 1. `Domain.Models.LoadFeedItemsRequest`
**File:** `src/JosephGuadagno.Broadcasting.Domain/Models/LoadFeedItemsRequest.cs`

| Property | Old Type | New Type | Rationale |
|----------|----------|----------|-----------|
| `CheckFrom` | `DateTime` | `DateTimeOffset` | Represents a UTC/timezone-aware checkpoint used for feed filtering. Using `DateTime` was inconsistent with all other temporal Domain model properties. |

### 2. `SpeakingEngagementsReader.Models.Presentation`
**File:** `src/JosephGuadagno.Broadcasting.SpeakingEngagementsReader/Models/Presentation.cs`

| Property | Old Type | New Type | Rationale |
|----------|----------|----------|-----------|
| `PresentationStartDateTime` | `DateTime?` | `DateTimeOffset?` | JSON deserialization model for talk start times. These map to `Talk.StartDateTime` (`DateTimeOffset`) in the domain. Using `DateTime?` caused implicit conversion with potential loss of timezone offset when the source JSON carries ISO 8601 timestamps with offsets. |
| `PresentationEndDateTime` | `DateTime?` | `DateTimeOffset?` | Same rationale as above. |

---

## BroadcastingContext.cs

No changes. `BroadcastingContext.cs` has no explicit `HasColumnType("datetime2")` mappings — all EF Core column type inference relies on the CLR type (`DateTimeOffset`) mapping to SQL `datetimeoffset` automatically.

---

## Test Updates

**File:** `src/JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Tests/ModelsTests.cs`

Updated two `Assert.Equal` calls in `Presentation_Properties_Work` to use `new DateTimeOffset(new DateTime(...))` to match the updated `DateTimeOffset?` property type.

---

## Migration Script

`scripts/database/migrations/2026-03-18-datetimeoffset-consistency.sql` — audit/documentation script. Contains no DML/DDL since no schema changes were needed. Documents the full list of confirmed `datetimeoffset` columns for operational reference.

---

## Columns Left As-Is

All datetime columns were already `datetimeoffset`. No columns were intentionally left as `datetime`/`datetime2`.

Non-temporal columns (e.g., `Name`, `Url`, `ItemTableName`, `Platform`, `MessageType`) are string/int/bit types — no consideration needed.

## Coordination Note for Sparks

The domain models `Engagement.StartDateTime`, `Engagement.EndDateTime`, `Talk.StartDateTime`, `Talk.EndDateTime`, `ScheduledItem.SendOnDateTime`, and `ScheduledItem.MessageSentOn` are all `DateTimeOffset`. Sparks can safely apply timezone-aware display in the UI using `TimeZoneInfo.ConvertTime()` against these values with the `Engagement.TimeZoneId` IANA timezone identifier.

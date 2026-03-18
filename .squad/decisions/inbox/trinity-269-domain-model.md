# Trinity Decisions: MessageTemplate Domain Model (Issue #269) — REVISED

## Date
2026-03-17 (revised — supersedes prior note)

## Summary
**Revised per Morpheus schema change**: `MessageTemplate` column was removed from `ScheduledItems`.
`ImageUrl` stays on `ScheduledItems`. A new dedicated `MessageTemplates` lookup table (composite PK)
holds Scriban templates keyed by `(Platform, MessageType)`.

This note documents the revised C# changes made in commit `e662c56` on branch `issue-269`.

## Files Changed

| File | Change |
|------|--------|
| `src/JosephGuadagno.Broadcasting.Domain/Models/ScheduledItem.cs` | Removed `MessageTemplate` property; `ImageUrl` kept |
| `src/JosephGuadagno.Broadcasting.Data.Sql/Models/ScheduledItem.cs` | Removed `MessageTemplate` property; `ImageUrl` kept |
| `src/JosephGuadagno.Broadcasting.Domain/Models/MessageTemplate.cs` | **New** — Domain model with `Platform`, `MessageType`, `Template`, `Description` |
| `src/JosephGuadagno.Broadcasting.Domain/Interfaces/IMessageTemplateDataStore.cs` | **New** — Interface: `GetAsync(platform, messageType)` + `GetAllAsync()` |
| `src/JosephGuadagno.Broadcasting.Data.Sql/Models/MessageTemplate.cs` | **New** — EF entity (`#nullable disable`, matches DB schema) |
| `src/JosephGuadagno.Broadcasting.Data.Sql/BroadcastingContext.cs` | Added `DbSet<MessageTemplate> MessageTemplates`; configured composite PK `(Platform, MessageType)`, `Template` (no max length = NVARCHAR(MAX)), `Description` (max 500) |
| `src/JosephGuadagno.Broadcasting.Data.Sql/MappingProfiles/BroadcastingProfile.cs` | Added `CreateMap<Models.MessageTemplate, Domain.Models.MessageTemplate>().ReverseMap()` |
| `src/JosephGuadagno.Broadcasting.Data.Sql/MessageTemplateDataStore.cs` | **New** — Implements `IMessageTemplateDataStore` with `BroadcastingContext` + `IMapper` primary constructor pattern |
| `src/JosephGuadagno.Broadcasting.Api/Program.cs` | Added DI registration (see below) |

## DI Registration Added

**File:** `src/JosephGuadagno.Broadcasting.Api/Program.cs`

```csharp
// MessageTemplate
services.TryAddScoped<IMessageTemplateDataStore, MessageTemplateDataStore>();
```

Placed after the `ScheduledItem` block (~line 165). Only API registered — Functions and Web are
out of scope for this task.

## Design Choices

### 1. `IMessageTemplateDataStore` does NOT inherit `IDataStore<T>`
Standard `IDataStore<T>` uses `int primaryKey`. `MessageTemplates` has a composite PK
`(Platform, MessageType)`. A custom interface with `GetAsync(string, string)` and `GetAllAsync()`
matches the actual look-up pattern (read-only lookup by platform+type at send time).

### 2. `AsNoTracking()` in data store
`MessageTemplates` is a read-only lookup at runtime. `AsNoTracking()` avoids unnecessary EF
change-tracking overhead on every send.

### 3. AutoMapper — `.ReverseMap()` sufficient
Both the EF entity and domain model have identical property names and types. No custom `ForMember`
mappings are needed.

### 4. `Template` property — no `.HasMaxLength()` in EF config
`NVARCHAR(MAX)` is the SQL type (per Morpheus decision). EF Core maps an unconstrained `string`
to `NVARCHAR(MAX)` by default; adding a max-length would cause a schema mismatch.

### 5. Build result
`dotnet build` — **Build succeeded, 0 errors**. All warnings are pre-existing nullable reference /
XML doc warnings unrelated to this change.


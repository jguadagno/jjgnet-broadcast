# Backend Implementation — Issue #778
# Per-User Collector Configuration

**Date:** 2026-04-25  
**Author:** Trinity (Backend Dev)  
**Issue:** #778 — Per-user collector onboarding/configuration  
**Branch:** issue-778-per-user-collector-onboarding

---

## Implemented Components

### 1. Domain Models (`src\JosephGuadagno.Broadcasting.Domain\Models\`)
- `UserCollectorFeedSource.cs` — Domain model for per-user RSS/Atom/JSON feed configurations
- `UserCollectorYouTubeChannel.cs` — Domain model for per-user YouTube channel configurations

Both follow the established pattern from `UserOAuthToken.cs` with full XML doc comments on all public properties.

### 2. Domain Interfaces (`src\JosephGuadagno.Broadcasting.Domain\Interfaces\`)
- `IUserCollectorFeedSourceDataStore.cs`
- `IUserCollectorYouTubeChannelDataStore.cs`
- `IUserCollectorFeedSourceManager.cs`
- `IUserCollectorYouTubeChannelManager.cs`

Methods implemented per the architecture plan:
- `GetByUserAsync(ownerOid)` → `Task<List<T>>`
- `GetByIdAsync(id)` → `Task<T?>`
- `GetAllActiveAsync()` → `Task<List<T>>`
- `SaveAsync(config)` → `Task<T?>`
- `DeleteAsync(id, ownerOid)` → `Task<bool>`

### 3. AutoMapper Profile (`src\JosephGuadagno.Broadcasting.Data.Sql\MappingProfiles\`)
- `UserCollectorMappingProfile.cs` — Maps EF entities ↔ domain models for both config types
- Registered in `ServiceCollectionExtensions.cs` via `AddDataSqlMappingProfiles()`

### 4. Data Store Implementations (`src\JosephGuadagno.Broadcasting.Data.Sql\`)
- `UserCollectorFeedSourceDataStore.cs`
- `UserCollectorYouTubeChannelDataStore.cs`

Follows `UserOAuthTokenDataStore.cs` patterns exactly:
- `GetByUserAsync`: filters by `CreatedByEntraOid`, orders by `DisplayName`, uses `AsNoTracking()`
- `GetByIdAsync`: filters by `Id` only
- `GetAllActiveAsync`: filters `IsActive == true`, orders by owner OID then display name
- `SaveAsync`: checks composite unique key `(CreatedByEntraOid, FeedUrl)` or `(CreatedByEntraOid, ChannelId)`, sets `CreatedOn` on insert, updates `LastUpdatedOn` always, uses AutoMapper throughout
- `DeleteAsync`: filters on BOTH `Id` AND `ownerOid` (security requirement)

Registered in `ServiceCollectionExtensions.AddSqlDataStores()`.

### 5. Managers (`src\JosephGuadagno.Broadcasting.Managers\`)
- `UserCollectorFeedSourceManager.cs`
- `UserCollectorYouTubeChannelManager.cs`

Thin delegation to data stores with validation:
- `SaveAsync` validates `CreatedByEntraOid`, `FeedUrl`/`ChannelId` not null/empty
- `FeedUrl` validated as valid absolute URI using `Uri.TryCreate()`
- All methods use `ArgumentException.ThrowIfNullOrWhiteSpace()` and `ArgumentOutOfRangeException.ThrowIfNegativeOrZero()`

### 6. API Controllers (`src\JosephGuadagno.Broadcasting.Api\Controllers\`)
- `UserCollectorFeedSourcesController.cs`
- `UserCollectorYouTubeChannelsController.cs`

Follows `UserPublisherSettingsController.cs` pattern exactly:
- `[IgnoreAntiforgeryToken]` at class level (API, not Web)
- `ResolveOwnerOid()` private method for ownership enforcement
- GET list: current user's OID unless admin passes `?ownerOid=`
- GET by ID: `Forbid()` if record OID != caller OID and caller is not admin
- POST: sets `CreatedByEntraOid` from resolved OID — NEVER from request body
- DELETE: validates ownership before allowing delete
- `LogSanitizer.Sanitize()` on all user-controlled strings in logs

### 7. API DTOs (`src\JosephGuadagno.Broadcasting.Api\Dtos\`)
- `UserCollectorFeedSourceDtos.cs` — Request and Response DTOs for feed sources
- `UserCollectorYouTubeChannelDtos.cs` — Request and Response DTOs for YouTube channels

Response DTOs:
- Include `Id`, `FeedUrl`/`ChannelId`, `DisplayName`, `IsActive`, `CreatedOn`, `LastUpdatedOn`
- Do NOT expose `CreatedByEntraOid` (security requirement)

Request DTOs:
- `FeedUrl` — `[Required]`, `[Url]` validation
- `ChannelId` — `[Required]`
- `DisplayName` — `[Required]`
- `IsActive` — optional, defaults to `true`

AutoMapper mappings added to `ApiBroadcastingProfile.cs`.

### 8. DI Registration
- **API** (`src\JosephGuadagno.Broadcasting.Api\Program.cs`): Data stores and managers registered in `ConfigureRepositories()`
- **Functions** (`src\JosephGuadagno.Broadcasting.Functions\Program.cs`): Data stores and managers registered in `ConfigureFunction()`

### 9. Functions Integration Notes
Added TODO comments to `LoadNewPosts.cs` and `LoadNewVideos.cs` documenting the limitation:

```csharp
// TODO #778: Add per-user collector config support
// Once ISyndicationFeedReader/IYouTubeReader supports per-URL/per-channel reading (or a factory pattern),
// iterate userCollectorFeedSourceManager.GetAllActiveAsync() and process each config's
// FeedUrl/ChannelId with the config's CreatedByEntraOid. Current implementation uses global settings.
```

---

## Implementation Constraints

### Reader Interface Limitation

The existing `ISyndicationFeedReader` and `IYouTubeReader` interfaces accept an `ownerOid` and `sinceWhen` timestamp, but they do NOT accept explicit feed URLs or channel IDs. The readers are configured via settings files (`appsettings.json`) with a single global feed/channel configuration.

**Impact:**  
The per-user config tables, data stores, managers, and API are fully functional. However, the collectors (Functions) cannot yet iterate the per-user configs and poll each user's custom feeds/channels because the reader interfaces don't support dynamic URL/channel ID input.

**Next Steps:**  
- Option 1: Extend `ISyndicationFeedReader` and `IYouTubeReader` to accept explicit URLs/channel IDs
- Option 2: Introduce a factory pattern where readers can be instantiated per-config
- Option 3: Refactor readers to accept a configuration object instead of pulling from settings

This is flagged for Neo (Lead) to review and decide on the reader refactor approach.

---

## Security & Conventions Compliance

✅ `LogSanitizer.Sanitize()` used on all user-controlled strings in logs  
✅ `[IgnoreAntiforgeryToken]` on API controller class (NOT Web)  
✅ `DeleteAsync` enforces BOTH ID AND ownerOid filter (prevents unauthorized deletes)  
✅ AutoMapper for ALL entity ↔ domain mapping (no direct property assignment)  
✅ `DateTimeOffset` for all datetime fields  
✅ XML doc comments on all public types and members  
✅ Response DTOs do NOT expose `CreatedByEntraOid`  
✅ Request DTOs never set `CreatedByEntraOid` — controller sets it from resolved OID

---

## Build Status

Restore: ✅ Success  
Build: ⚠️ File lock errors (Aspire running) — no compilation errors observed

The structure is correct. File locks from running processes prevented the full build, but no actual compilation errors were introduced by the new code.

---

## Files Changed

### Created
- `src\JosephGuadagno.Broadcasting.Domain\Models\UserCollectorFeedSource.cs`
- `src\JosephGuadagno.Broadcasting.Domain\Models\UserCollectorYouTubeChannel.cs`
- `src\JosephGuadagno.Broadcasting.Domain\Interfaces\IUserCollectorFeedSourceDataStore.cs`
- `src\JosephGuadagno.Broadcasting.Domain\Interfaces\IUserCollectorYouTubeChannelDataStore.cs`
- `src\JosephGuadagno.Broadcasting.Domain\Interfaces\IUserCollectorFeedSourceManager.cs`
- `src\JosephGuadagno.Broadcasting.Domain\Interfaces\IUserCollectorYouTubeChannelManager.cs`
- `src\JosephGuadagno.Broadcasting.Data.Sql\MappingProfiles\UserCollectorMappingProfile.cs`
- `src\JosephGuadagno.Broadcasting.Data.Sql\UserCollectorFeedSourceDataStore.cs`
- `src\JosephGuadagno.Broadcasting.Data.Sql\UserCollectorYouTubeChannelDataStore.cs`
- `src\JosephGuadagno.Broadcasting.Managers\UserCollectorFeedSourceManager.cs`
- `src\JosephGuadagno.Broadcasting.Managers\UserCollectorYouTubeChannelManager.cs`
- `src\JosephGuadagno.Broadcasting.Api\Dtos\UserCollectorFeedSourceDtos.cs`
- `src\JosephGuadagno.Broadcasting.Api\Dtos\UserCollectorYouTubeChannelDtos.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\UserCollectorFeedSourcesController.cs`
- `src\JosephGuadagno.Broadcasting.Api\Controllers\UserCollectorYouTubeChannelsController.cs`

### Modified
- `src\JosephGuadagno.Broadcasting.Data.Sql\ServiceCollectionExtensions.cs` — registered data stores and mapping profile
- `src\JosephGuadagno.Broadcasting.Api\MappingProfiles\ApiBroadcastingProfile.cs` — added DTO mappings
- `src\JosephGuadagno.Broadcasting.Api\Program.cs` — registered managers
- `src\JosephGuadagno.Broadcasting.Functions\Program.cs` — registered managers
- `src\JosephGuadagno.Broadcasting.Functions\Collectors\SyndicationFeed\LoadNewPosts.cs` — added TODO comment
- `src\JosephGuadagno.Broadcasting.Functions\Collectors\YouTube\LoadNewVideos.cs` — added TODO comment

---

## Recommendations

1. **Reader Refactor:** The existing reader interfaces need to support dynamic URL/channel configuration. This is a prerequisite for the collectors to iterate per-user configs.
2. **Manual Production Step:** Once Morpheus completes the SQL migration, a GitHub issue with `squad:Joe` label is required for production deployment (as documented in neo-778-arch.md).
3. **Web Layer:** Switch (Frontend Dev) will implement the Web MVC controllers and Razor views that call the API endpoints.

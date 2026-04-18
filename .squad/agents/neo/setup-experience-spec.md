# New User Setup Experience — Feature Spec

**Author:** Neo (Lead)  
**Date:** 2026-04-17  
**Status:** Draft Spec  
**Related Epic:** #609 (Multi-tenancy — per-user content, publishers, and social tokens)  
**Related Issues:** #731 (Per-user publisher settings)

---

## Problem Statement

### Current State
After a new user is approved by an administrator (via the approval gate from issue #731), they immediately enter the main application with **no collectors or publishers configured**. The application is currently designed for a single user (Joseph Guadagno), meaning:

1. **Collectors** (YouTube, SyndicationFeed) use **global app settings** — there's no way for a new user to configure their own RSS feed URL, YouTube channel, or playlist ID
2. **Publishers** (Bluesky, Twitter/X, Facebook, LinkedIn) use **global app settings and Key Vault** — no per-user social media credentials exist
3. A new user who gets approved sees an empty experience with no way to actually broadcast anything

### Why This Matters
For multi-user support (#609), each user needs to:
1. **Bring their own content sources** — their own blog RSS feed, their own YouTube playlist
2. **Bring their own social accounts** — their own Twitter, Bluesky, etc. credentials
3. **Choose which platforms to use** — not all users want to publish to all platforms

Without a setup experience, approved users have no path to actually use the application.

---

## Scope

### In Scope
1. **First-visit detection** — Recognize when an approved user has not completed setup
2. **Collector selection** — UI to enable/configure YouTube and SyndicationFeed collectors
3. **Publisher selection** — UI to enable/configure publishers (Bluesky, Twitter, etc.)
4. **Per-user collector settings** — New database tables and full-stack support
5. **Per-user publisher settings** — Builds on #731 (UserPublisherSettings table)
6. **Setup completion flag** — Track whether a user has completed initial setup
7. **Skip/Later option** — Allow users to skip setup and complete it later

### Out of Scope
1. **OAuth flows for social platforms** — Complex OAuth implementation is separate work
2. **Credential encryption** — #731 will handle secure storage strategy
3. **Collector execution changes** — Functions will be updated separately to respect per-user config
4. **New collector types** — JsonFeed, Speaking Engagements collectors (future work)
5. **Admin view of all users' setup status** — Site Admin tooling is separate

---

## User Flow

### Happy Path: First-Time Approved User

```
1. User signs in with Microsoft Entra ID
   ↓
2. First visit → No ApplicationUsers record → Auto-created with ApprovalStatus='Pending'
   ↓
3. UserApprovalMiddleware redirects to /Account/PendingApproval
   ↓
4. Admin approves user (ApprovalStatus='Approved')
   ↓
5. User's next visit passes approval gate
   ↓
6. NEW: SetupMiddleware detects HasCompletedSetup=false
   ↓
7. NEW: Redirect to /Setup/Welcome
   ↓
8. Welcome page explains the setup wizard
   ↓
9. User clicks "Start Setup" → /Setup/Collectors
   ↓
10. Collectors page shows:
    - YouTube Collector: [Enable] checkbox
      - If enabled: Playlist ID (required), Channel ID (optional), API Key (required)
    - Syndication Feed Collector: [Enable] checkbox  
      - If enabled: Feed URL (required)
   ↓
11. User configures their collectors → clicks "Next"
   ↓
12. /Setup/Publishers page shows:
    - Bluesky: [Enable] checkbox
      - If enabled: Handle, App Password
    - Twitter/X: [Enable] checkbox
      - If enabled: API Key, API Secret, Access Token, Access Token Secret
    - LinkedIn: [Enable] checkbox
      - If enabled: Access Token (OAuth flow link)
    - Facebook: [Enable] checkbox
      - If enabled: Page Access Token
   ↓
13. User configures their publishers → clicks "Next"
   ↓
14. /Setup/Review page shows summary of configured collectors + publishers
   ↓
15. User clicks "Complete Setup"
   ↓
16. HasCompletedSetup=true saved to ApplicationUsers
   ↓
17. Redirect to /Home/Index (normal app experience)
```

### Alternative Flows

**Skip Setup:**
- User clicks "Skip for now" on Welcome page
- `HasCompletedSetup` remains `false`
- User can access the app but sees a banner: "Complete your setup to start broadcasting"
- /Settings page has "Complete Setup" button

**Return Later:**
- User can access /Setup/Collectors and /Setup/Publishers directly from Settings menu
- Partial setup is saved — user doesn't lose progress if they leave mid-wizard

**Edit After Completion:**
- /Settings/Collectors and /Settings/Publishers available after setup complete
- Full CRUD for collector and publisher configurations

---

## UI Design (Web MVC)

### New Controllers

#### `SetupController` (New)
```
/Setup/Welcome        GET  — Welcome page explaining the wizard
/Setup/Collectors     GET  — Collector configuration form
/Setup/Collectors     POST — Save collector settings
/Setup/Publishers     GET  — Publisher configuration form
/Setup/Publishers     POST — Save publisher settings
/Setup/Review         GET  — Summary before completing
/Setup/Complete       POST — Mark setup complete, redirect to home
/Setup/Skip           POST — Skip setup, set flag, redirect to home
```

#### `SettingsController` (New)
```
/Settings             GET  — Main settings page
/Settings/Collectors  GET  — Edit collectors (post-setup)
/Settings/Collectors  POST — Save collector changes
/Settings/Publishers  GET  — Edit publishers (post-setup)
/Settings/Publishers  POST — Save publisher changes
```

### New Views

```
Views/
  Setup/
    Welcome.cshtml        — Hero section, "Start Setup" + "Skip" buttons
    Collectors.cshtml     — Form: YouTube config, SyndicationFeed config
    Publishers.cshtml     — Form: Bluesky, Twitter, LinkedIn, Facebook config
    Review.cshtml         — Read-only summary, "Complete Setup" button
  Settings/
    Index.cshtml          — Settings hub with links to collectors/publishers
    Collectors.cshtml     — Same as Setup/Collectors but for editing
    Publishers.cshtml     — Same as Setup/Publishers but for editing
  Shared/
    _SetupBanner.cshtml   — Partial for "Complete your setup" banner
```

### Navigation Updates

- Add "Settings" to main nav (alongside Engagements, Talks, Schedules)
- Settings dropdown: Collectors | Publishers | Profile
- Show setup banner on all pages if `HasCompletedSetup=false`

### Form Design Notes

**Collector Forms:**
- Enable checkbox at top of each collector section
- Fields disabled until checkbox is checked (progressive disclosure)
- Validation: If enabled, required fields must have values
- Help text explaining where to find YouTube API Key, Playlist ID, etc.

**Publisher Forms:**
- Enable checkbox per platform
- Sensitive fields (passwords, tokens) are:
  - Write-only on initial entry
  - Masked (•••••) after save
  - "Change" link to re-enter
- Test button per platform: "Test Connection" → calls manager to verify credentials

---

## API Changes

### New Endpoints (UserCollectorSettings)

```
GET    /api/users/me/collectors              — Get all collector settings for current user
GET    /api/users/me/collectors/{type}       — Get specific collector (YouTube, SyndicationFeed)
PUT    /api/users/me/collectors/{type}       — Create or update collector setting
DELETE /api/users/me/collectors/{type}       — Disable/delete collector setting
```

### Existing Endpoints (UserPublisherSettings from #731)

```
GET    /api/users/me/publishers              — Get all publisher settings for current user
GET    /api/users/me/publishers/{platformId} — Get specific publisher
PUT    /api/users/me/publishers/{platformId} — Create or update publisher setting
DELETE /api/users/me/publishers/{platformId} — Delete publisher setting
```

### New Endpoint (Setup Status)

```
GET    /api/users/me/setup-status            — Returns { hasCompletedSetup: bool }
POST   /api/users/me/complete-setup          — Sets HasCompletedSetup=true
```

### Authorization

All `/api/users/me/*` endpoints:
- Require authentication (Bearer token)
- Scoped to current user's EntraObjectId (cannot access other users' settings)
- No role requirement (any approved user can manage their own settings)

Site Administrator override (future):
- `GET /api/admin/users/{userId}/collectors` — view any user's collectors
- Pattern established but not required for initial setup experience

---

## Database Changes

### Modify Table: `ApplicationUsers`

Add column to track setup completion:

```sql
ALTER TABLE dbo.ApplicationUsers
ADD HasCompletedSetup BIT NOT NULL
    CONSTRAINT DF_ApplicationUsers_HasCompletedSetup DEFAULT (0);
GO
```

### New Table: `UserCollectorSettings`

```sql
CREATE TABLE dbo.UserCollectorSettings
(
    Id                INT IDENTITY
        CONSTRAINT PK_UserCollectorSettings PRIMARY KEY CLUSTERED,
    CreatedByEntraOid NVARCHAR(36)   NOT NULL,
    CollectorType     NVARCHAR(50)   NOT NULL, -- 'YouTube', 'SyndicationFeed'
    IsEnabled         BIT            NOT NULL DEFAULT 0,
    Settings          NVARCHAR(MAX)  NULL,     -- JSON blob for type-specific config
    CreatedOn         DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE(),
    LastUpdatedOn     DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_UserCollectorSettings_User_Type
        UNIQUE (CreatedByEntraOid, CollectorType),
    CONSTRAINT CK_UserCollectorSettings_CollectorType
        CHECK (CollectorType IN ('YouTube', 'SyndicationFeed'))
)
GO
```

**JSON Settings Schema per Collector Type:**

```json
// YouTube
{
  "playlistId": "PLxxxxxxxxx",
  "channelId": "UCxxxxxxxxx",
  "apiKey": "AIza...",
  "resultSetPageSize": 25
}

// SyndicationFeed
{
  "feedUrl": "https://example.com/feed.xml"
}
```

### Existing Table: `UserPublisherSettings` (from #731)

Already designed in #731:

```sql
CREATE TABLE dbo.UserPublisherSettings
(
    Id                    INT IDENTITY CONSTRAINT PK_UserPublisherSettings PRIMARY KEY CLUSTERED,
    CreatedByEntraOid     NVARCHAR(36)  NOT NULL,
    SocialMediaPlatformId INT           NOT NULL
        REFERENCES dbo.SocialMediaPlatforms(Id),
    IsEnabled             BIT           NOT NULL DEFAULT 0,
    Settings              NVARCHAR(MAX) NULL,  -- JSON blob
    CreatedOn             DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE(),
    LastUpdatedOn         DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_UserPublisherSettings_User_Platform
        UNIQUE (CreatedByEntraOid, SocialMediaPlatformId)
)
```

### Migration Script Location

```
scripts/database/migrations/YYYY-MM-DD-user-setup-experience.sql
```

Script must be idempotent (IF NOT EXISTS checks).

---

## Domain / Manager Changes

### New Domain Models

```csharp
// JosephGuadagno.Broadcasting.Domain.Models

public class UserCollectorSetting
{
    public int Id { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
    public string CollectorType { get; set; } = string.Empty; // Use constants
    public bool IsEnabled { get; set; }
    public string? Settings { get; set; } // JSON blob
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}

// Typed settings classes for deserialization
public class YouTubeCollectorSettings
{
    public string? PlaylistId { get; set; }
    public string? ChannelId { get; set; }
    public string? ApiKey { get; set; }
    public int ResultSetPageSize { get; set; } = 25;
}

public class SyndicationFeedCollectorSettings
{
    public string? FeedUrl { get; set; }
}
```

### New Interfaces

```csharp
// JosephGuadagno.Broadcasting.Domain.Interfaces

public interface IUserCollectorSettingDataStore
{
    Task<IReadOnlyList<UserCollectorSetting>> GetByUserAsync(string ownerOid);
    Task<UserCollectorSetting?> GetByUserAndTypeAsync(string ownerOid, string collectorType);
    Task<OperationResult<UserCollectorSetting>> SaveAsync(UserCollectorSetting setting);
    Task<bool> DeleteAsync(string ownerOid, string collectorType);
}

public interface IUserCollectorSettingManager
{
    Task<IReadOnlyList<UserCollectorSetting>> GetByUserAsync(string ownerOid);
    Task<UserCollectorSetting?> GetByUserAndTypeAsync(string ownerOid, string collectorType);
    Task<OperationResult<UserCollectorSetting>> SaveAsync(UserCollectorSetting setting);
    Task<bool> DeleteAsync(string ownerOid, string collectorType);
    
    // Typed helpers
    Task<YouTubeCollectorSettings?> GetYouTubeSettingsAsync(string ownerOid);
    Task<SyndicationFeedCollectorSettings?> GetSyndicationFeedSettingsAsync(string ownerOid);
}
```

### New Constants

```csharp
// JosephGuadagno.Broadcasting.Domain.Constants

public static class CollectorTypes
{
    public const string YouTube = "YouTube";
    public const string SyndicationFeed = "SyndicationFeed";
    
    public static readonly string[] All = { YouTube, SyndicationFeed };
}
```

### ApplicationUser Changes

Add property to existing model:

```csharp
public class ApplicationUser
{
    // ... existing properties ...
    public bool HasCompletedSetup { get; set; }
}
```

---

## Middleware: Setup Gate

Similar to `UserApprovalMiddleware`, add `UserSetupMiddleware`:

```csharp
public class UserSetupMiddleware(RequestDelegate next, ILogger<UserSetupMiddleware> logger)
{
    private const string SetupPath = "/Setup";
    private const string SettingsPath = "/Settings";
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Pass through if not authenticated or not approved
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }
        
        var approvalStatus = context.User.FindFirst(ApplicationClaimTypes.ApprovalStatus)?.Value;
        if (approvalStatus != ApprovalStatus.Approved.ToString())
        {
            await next(context);
            return;
        }
        
        // Pass through if already on setup/settings pages
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith(SetupPath, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(SettingsPath, StringComparison.OrdinalIgnoreCase) ||
            IsStaticOrWellKnownPath(path))
        {
            await next(context);
            return;
        }
        
        // Check setup completion claim
        var setupCompleteClaim = context.User.FindFirst(ApplicationClaimTypes.HasCompletedSetup);
        if (setupCompleteClaim?.Value != "true")
        {
            // Optional: Only redirect on first visit, not every request
            // Could add a "setup_prompted" session flag
            context.Response.Redirect("/Setup/Welcome");
            return;
        }
        
        await next(context);
    }
}
```

**Important:** Middleware order must be:
1. `UseAuthentication()`
2. `UseUserApprovalGate()` — Checks approval status
3. `UseUserSetupGate()` — Checks setup completion (NEW)
4. `UseAuthorization()`

---

## Dependency on #609 / #731

### Must Complete First (Blockers)

1. **#731 — UserPublisherSettings table** — Required for publisher configuration
2. **#725 — Schema changes for sources** — Adds `CreatedByEntraOid` to YouTubeSources and SyndicationFeedSources tables

### Must Complete Concurrently

3. **EntraClaimsTransformation update** — Add `HasCompletedSetup` claim
4. **ApplicationUserManager.GetOrCreateAsync** — Must set `HasCompletedSetup=false` for new users

### Can Complete After (Not Blockers)

5. **Functions collector updates** — Update `LoadNewVideos` and `LoadNewPosts` to read from `UserCollectorSettings` instead of app settings
6. **Functions publisher updates** — Update publishers to read from `UserPublisherSettings` instead of Key Vault

---

## GitHub Issues to Create

### Core Setup Experience (Sprint 1)

| # | Title | Assignee Hint | Dependencies |
|---|-------|---------------|--------------|
| 1 | **feat(#609): Add HasCompletedSetup column to ApplicationUsers** | Morpheus | None |
| 2 | **feat(#609): Add UserCollectorSettings table** | Morpheus | None |
| 3 | **feat(#609): UserCollectorSetting domain model and interfaces** | Tank | #1, #2 |
| 4 | **feat(#609): UserCollectorSettingDataStore implementation** | Morpheus | #3 |
| 5 | **feat(#609): UserCollectorSettingManager implementation** | Trinity | #4 |
| 6 | **feat(#609): User collector settings API endpoints** | Trinity | #5 |

### Web Setup Wizard (Sprint 2)

| # | Title | Assignee Hint | Dependencies |
|---|-------|---------------|--------------|
| 7 | **feat(#609): SetupController and views** | Switch | #6, #731 |
| 8 | **feat(#609): UserSetupMiddleware** | Switch | #7 |
| 9 | **feat(#609): SettingsController and post-setup editing** | Switch | #7 |
| 10 | **feat(#609): Setup completion banner partial** | Sparks | #8 |

### Integration (Sprint 3)

| # | Title | Assignee Hint | Dependencies |
|---|-------|---------------|--------------|
| 11 | **feat(#609): Update EntraClaimsTransformation with HasCompletedSetup claim** | Ghost | #1 |
| 12 | **feat(#609): Functions — Update collectors to use UserCollectorSettings** | Cypher | #5, #11 |
| 13 | **feat(#609): Functions — Update publishers to use UserPublisherSettings** | Cypher | #731 |

### Testing (Parallel with Sprint 2-3)

| # | Title | Assignee Hint | Dependencies |
|---|-------|---------------|--------------|
| 14 | **test(#609): UserCollectorSettingManager unit tests** | Tank | #5 |
| 15 | **test(#609): UserCollectorSettings API integration tests** | Tank | #6 |
| 16 | **test(#609): SetupController tests** | Tank | #7 |

---

## Open Questions

### 1. **OAuth vs. Direct Credentials for Publishers?** ✅ DECIDED

**Decision:** Start with **direct credential entry** (user pastes their own API keys/tokens). OAuth integration is deferred as a follow-on enhancement.

**Rationale:** Not all providers support OAuth, and direct credentials match the current app-level architecture already in place. Users are expected to manage their own app registrations per platform.

**OAuth:** Deferred — future enhancement after direct credential flow is stable.

### 2. **Forced Setup vs. Optional Setup?** ✅ DECIDED

**Decision:** **Soft enforcement.** Redirect to setup on first visit, but allow skip. Without publishers configured, scheduled items cannot be broadcast — but users CAN still create engagements and talks manually. A visible banner reminds users to complete setup.

**Rationale:** There is meaningful value in creating engagements/talks even without publishers — content can be manually managed. Blocking access entirely would prevent this legitimate use.

### 3. **Credential Storage Security** ✅ DECIDED

**Decision:** JSON in SQL with **Data Protection API encryption** on the `Settings` column (as designed in #731). Key Vault per-user integration is deferred.

**Future issue needed:** Create a GitHub issue to track migrating per-user credentials to Azure Key Vault as a security hardening step after the initial implementation is stable.

### 4. **What Happens If No Collectors Configured?** ✅ DECIDED

**Decision:** **Allow empty collectors.** Users can still manually create engagements and talks without any collectors. Without publishers, nothing gets broadcast — but content creation is still functional. No hard block on setup completion.

### 5. **Setup Wizard State Persistence** ✅ DECIDED

**Decision:** **Save each step immediately** to the database. User doesn't lose work if browser crashes. `HasCompletedSetup=true` is only set after the final step is confirmed.

---

## Appendix: Current Config Locations

> **Note on secrets architecture:** In production, all sensitive values (API keys, passwords, tokens) are loaded from **Azure Key Vault** via environment settings. Locally, these values are stored in **user secrets** (not Key Vault). Any "(app settings)" entries below refer to config-bound settings — sensitive ones are Key Vault-backed in production.

### YouTube Collector (Current)
- `IYouTubeSettings.PlaylistId` — app settings
- `IYouTubeSettings.ChannelId` — app settings
- `IYouTubeSettings.ApiKey` — Key Vault in production / user secrets locally
- `IYouTubeSettings.ResultSetPageSize` — app settings

### Syndication Feed Collector (Current)
- `ISyndicationFeedReaderSettings.FeedUrl` — app settings

### Bluesky Publisher (Current)
- `IBlueskySettings.BlueskyUserName` — app settings
- `IBlueskySettings.BlueskyPassword` — Key Vault in production / user secrets locally

### Twitter Publisher (Current)
- `TwitterContext` configured via app settings
- API Key, API Secret, Access Token, Access Token Secret — Key Vault in production / user secrets locally

### LinkedIn Publisher (Current)
- Access Token stored in Key Vault (`LinkedInController.cs` lines 146-156)
- Token refresh managed separately

### Facebook Publisher (Current)
- Page Access Token — app settings/Key Vault

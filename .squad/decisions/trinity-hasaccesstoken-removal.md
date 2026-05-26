# Decision: HasAccessToken Removal from LinkedIn Settings

**Date:** 2026-05-21  
**Author:** Trinity  
**Branch:** issue-980-publisher-architecture-refactor

## Summary

Removed the `HasAccessToken` property from all LinkedIn settings layers. This property was a flag indicating an access token was stored in Key Vault under the name `publisher-{ownerOid}-linkedin-access-token`. Since LinkedIn OAuth tokens are now stored in `UserOAuthTokens`, this flag is dead code.

## DB Column Status — Action Required

`HasAccessToken` **is a column in the `UserPublisherLinkedInSettings` table**. It exists in:

- `scripts/database/table-create.sql` line 416
- `scripts/database/migrations/2026-05-15-publisher-settings-per-publisher-tables.sql` line 74

The C# code that maps to this column has been removed in this PR. EF Core will ignore unmapped columns at read time, so the app will function correctly without a migration. However, the column is now orphaned in the schema.

**A follow-up SQL migration is needed** to drop the column:

```sql
ALTER TABLE [dbo].[UserPublisherLinkedInSettings]
DROP CONSTRAINT [DF_UserPublisherLinkedInSettings_HasAccessToken];

ALTER TABLE [dbo].[UserPublisherLinkedInSettings]
DROP COLUMN [HasAccessToken];
```

Also update `scripts/database/table-create.sql` and the original migration file to remove the column definition.

This can be tracked as a cleanup task — no functional impact since EF ignores unmapped columns.

## Files Changed

- `src/JosephGuadagno.Broadcasting.Domain/Models/UserPublisherLinkedInSettings.cs` — removed property
- `src/JosephGuadagno.Broadcasting.Domain/Models/LinkedInPublisherSetting.cs` — removed property
- `src/JosephGuadagno.Broadcasting.Data.Sql/Models/UserPublisherLinkedInSettings.cs` — removed property
- `src/JosephGuadagno.Broadcasting.Data.Sql/UserPublisherLinkedInSettingsDataStore.cs` — removed mapping assignment
- `src/JosephGuadagno.Broadcasting.Api/Dtos/LinkedInSettingsDtos.cs` — removed from response DTO
- `src/JosephGuadagno.Broadcasting.Api/Controllers/Publishers/LinkedInSettingsController.cs` — removed `settings.HasAccessToken = true`
- `src/JosephGuadagno.Broadcasting.Web/Models/PublisherPlatformSettingsViewModels.cs` — removed property and updated validation
- `src/JosephGuadagno.Broadcasting.Web/Controllers/PublisherLinkedInSettingsController.cs` — removed from mapping
- `src/JosephGuadagno.Broadcasting.Data.Sql.Tests/UserPublisherLinkedInSettingsDataStoreTests.cs` — removed from test fixtures
- `src/JosephGuadagno.Broadcasting.Web/Views/PublisherLinkedInSettings/Index.cshtml` — removed access token row
- `src/JosephGuadagno.Broadcasting.Web/Views/PublisherLinkedInSettings/Edit.cshtml` — removed hidden input and list item

## Not Changed (Twitter)

`HasAccessToken` and `HasAccessTokenSecret` on `UserPublisherTwitterSettings` were **not touched** — Twitter still uses the Key Vault token storage pattern and those properties are active.

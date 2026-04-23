-- Migration: Add CredentialSetupDocumentationUrl to SocialMediaPlatforms
-- Issue: #812
-- Date: 2026-05-01
-- Description: Adds optional CredentialSetupDocumentationUrl column to
--              dbo.SocialMediaPlatforms so each platform can link to its
--              credential-setup help page in the admin UI.

USE JJGNet;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[SocialMediaPlatforms]')
      AND name = N'CredentialSetupDocumentationUrl'
)
BEGIN
    ALTER TABLE [dbo].[SocialMediaPlatforms]
        ADD [CredentialSetupDocumentationUrl] nvarchar(500) NULL;
END
GO

-- Seed known documentation URLs (idempotent)
UPDATE [dbo].[SocialMediaPlatforms]
SET [CredentialSetupDocumentationUrl] = N'/help/socialMediaPlatforms/twitter'
WHERE [CredentialSetupDocumentationUrl] IS NULL
  AND [Name] = N'Twitter';

UPDATE [dbo].[SocialMediaPlatforms]
SET [CredentialSetupDocumentationUrl] = N'/help/socialMediaPlatforms/bluesky'
WHERE [CredentialSetupDocumentationUrl] IS NULL
  AND [Name] = N'BlueSky';

UPDATE [dbo].[SocialMediaPlatforms]
SET [CredentialSetupDocumentationUrl] = N'/help/socialMediaPlatforms/linkedin'
WHERE [CredentialSetupDocumentationUrl] IS NULL
  AND [Name] = N'LinkedIn';

UPDATE [dbo].[SocialMediaPlatforms]
SET [CredentialSetupDocumentationUrl] = N'/help/socialMediaPlatforms/facebook'
WHERE [CredentialSetupDocumentationUrl] IS NULL
  AND [Name] = N'Facebook';

UPDATE [dbo].[SocialMediaPlatforms]
SET [CredentialSetupDocumentationUrl] = N'/help/socialMediaPlatforms/mastodon'
WHERE [CredentialSetupDocumentationUrl] IS NULL
  AND [Name] = N'Mastodon';
GO

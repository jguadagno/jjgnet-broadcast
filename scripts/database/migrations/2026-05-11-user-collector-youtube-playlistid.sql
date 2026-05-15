-- Migration: Add PlaylistId to UserCollectorYouTubeChannels for per-user configuration
-- Issue: #950 (per-user YouTube playlist ID replaces global setting)
-- Date: 2026-05-11
--
-- Design notes:
--   - PlaylistId was previously a global application setting (IYouTubeSettings.PlaylistId)
--   - It is now per-user: each user's YouTube collector config carries their own playlist reference
--   - Empty string ('') is the default, indicating the user has not configured a specific playlist
--   - This enables per-user content targeting for YouTube video collection

USE JJGNet;
GO

-- Add the PlaylistId column (nullable first to allow for existing rows)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.UserCollectorYouTubeChannels') AND name = 'PlaylistId'
)
BEGIN
    ALTER TABLE dbo.UserCollectorYouTubeChannels
        ADD PlaylistId nvarchar(255) NULL;
END
GO

-- Populate existing rows with empty string (no specific playlist configured)
UPDATE dbo.UserCollectorYouTubeChannels
SET PlaylistId = ''
WHERE PlaylistId IS NULL;
GO

-- Make the column NOT NULL with a default
ALTER TABLE dbo.UserCollectorYouTubeChannels
    ALTER COLUMN PlaylistId nvarchar(255) NOT NULL;
GO

ALTER TABLE dbo.UserCollectorYouTubeChannels
    ADD CONSTRAINT DF_UserCollectorYouTubeChannels_PlaylistId DEFAULT ('') FOR PlaylistId;
GO

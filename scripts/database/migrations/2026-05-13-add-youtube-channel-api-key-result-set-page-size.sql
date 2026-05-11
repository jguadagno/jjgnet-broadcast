-- Migration: Add ApiKey (renamed to ApiKeySecretName) and ResultSetPageSize to UserCollectorYouTubeChannels
-- ApiKeySecretName:   nullable nvarchar(255); stores the Azure Key Vault secret name, not the raw key.
-- ResultSetPageSize:  non-nullable int with default 50; valid range is 1–200.

-- Rename ApiKey to ApiKeySecretName if it already exists (handles environments that ran the old migration)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('UserCollectorYouTubeChannels') AND name = 'ApiKey')
BEGIN
    EXEC sp_rename 'UserCollectorYouTubeChannels.ApiKey', 'ApiKeySecretName', 'COLUMN';
END
GO

-- Add ApiKeySecretName column if not already present
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UserCollectorYouTubeChannels') AND name = 'ApiKeySecretName')
BEGIN
    ALTER TABLE dbo.UserCollectorYouTubeChannels
        ADD ApiKeySecretName nvarchar(255) NULL
END
GO

-- Add ResultSetPageSize column if not already present
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UserCollectorYouTubeChannels') AND name = 'ResultSetPageSize')
BEGIN
    ALTER TABLE dbo.UserCollectorYouTubeChannels
        ADD ResultSetPageSize int NOT NULL DEFAULT 50
END
GO

-- Migration: Add ConferenceHashtag and ConferenceTwitterHandle to Engagements table
-- Issue: #105
-- Date: 2026-03-21

USE JJGNet;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Engagements') AND name = 'ConferenceHashtag'
)
BEGIN
    ALTER TABLE dbo.Engagements
        ADD ConferenceHashtag NVARCHAR(255) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Engagements') AND name = 'ConferenceTwitterHandle'
)
BEGIN
    ALTER TABLE dbo.Engagements
        ADD ConferenceTwitterHandle NVARCHAR(255) NULL;
END
GO

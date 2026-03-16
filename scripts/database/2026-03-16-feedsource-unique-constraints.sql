USE JJGNet;
GO

-- Resize SyndicationFeedSources.FeedIdentifier from NVARCHAR(MAX) to NVARCHAR(450)
-- SQL Server cannot index NVARCHAR(MAX); 450 chars = 900 bytes = max key size for a unique index.
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = 'SyndicationFeedSources'
      AND c.name = 'FeedIdentifier'
      AND c.max_length = -1  -- -1 indicates MAX type
)
BEGIN
    ALTER TABLE dbo.SyndicationFeedSources
        ALTER COLUMN FeedIdentifier NVARCHAR(450) NOT NULL;
END
GO

-- Add unique constraint on SyndicationFeedSources.FeedIdentifier
IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE name = 'UQ_SyndicationFeedSources_FeedIdentifier'
      AND parent_object_id = OBJECT_ID('dbo.SyndicationFeedSources')
)
BEGIN
    ALTER TABLE dbo.SyndicationFeedSources
        ADD CONSTRAINT UQ_SyndicationFeedSources_FeedIdentifier UNIQUE (FeedIdentifier);
END
GO

-- Add unique constraint on YouTubeSources.VideoId (already NVARCHAR(20), no resize needed)
IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE name = 'UQ_YouTubeSources_VideoId'
      AND parent_object_id = OBJECT_ID('dbo.YouTubeSources')
)
BEGIN
    ALTER TABLE dbo.YouTubeSources
        ADD CONSTRAINT UQ_YouTubeSources_VideoId UNIQUE (VideoId);
END
GO

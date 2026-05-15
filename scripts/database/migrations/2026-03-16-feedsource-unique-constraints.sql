USE JJGNet;
GO

-- Resize SyndicationFeedItems.FeedIdentifier from NVARCHAR(MAX) to NVARCHAR(450)
-- SQL Server cannot index NVARCHAR(MAX); 450 chars = 900 bytes = max key size for a unique index.
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = 'SyndicationFeedItems'
      AND c.name = 'FeedIdentifier'
      AND c.max_length = -1  -- -1 indicates MAX type
)
BEGIN
    ALTER TABLE dbo.SyndicationFeedItems
        ALTER COLUMN FeedIdentifier NVARCHAR(450) NOT NULL;
END
GO

-- Add unique constraint on SyndicationFeedItems.FeedIdentifier
IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE name = 'UQ_SyndicationFeedItems_FeedIdentifier'
      AND parent_object_id = OBJECT_ID('dbo.SyndicationFeedItems')
)
BEGIN
    ALTER TABLE dbo.SyndicationFeedItems
        ADD CONSTRAINT UQ_SyndicationFeedItems_FeedIdentifier UNIQUE (FeedIdentifier);
END
GO

-- Add unique constraint on YouTubeItems.VideoId (already NVARCHAR(20), no resize needed)
IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE name = 'UQ_YouTubeItems_VideoId'
      AND parent_object_id = OBJECT_ID('dbo.YouTubeItems')
)
BEGIN
    ALTER TABLE dbo.YouTubeItems
        ADD CONSTRAINT UQ_YouTubeItems_VideoId UNIQUE (VideoId);
END
GO

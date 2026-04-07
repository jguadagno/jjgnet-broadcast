-- Migration: Create SourceTags junction table to normalize delimited Tags columns
-- Issue #323: feat(data): normalize Tags column to junction table
-- SourceType valid values: 'SyndicationFeed', 'YouTube'

CREATE TABLE dbo.SourceTags
(
    Id         INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_SourceTags PRIMARY KEY CLUSTERED,
    SourceId   INT           NOT NULL,
    SourceType NVARCHAR(50)  NOT NULL,
    Tag        NVARCHAR(100) NOT NULL
);
GO

CREATE INDEX IX_SourceTags_Tag ON dbo.SourceTags (Tag);
GO

CREATE INDEX IX_SourceTags_SourceId_SourceType ON dbo.SourceTags (SourceId, SourceType);
GO

-- Migrate existing delimited tag data from SyndicationFeedSources
INSERT INTO dbo.SourceTags (SourceId, SourceType, Tag)
SELECT s.Id, 'SyndicationFeed', LTRIM(RTRIM(value))
FROM dbo.SyndicationFeedSources s
CROSS APPLY STRING_SPLIT(s.Tags, ',')
WHERE s.Tags IS NOT NULL AND s.Tags != '';
GO

-- Migrate existing delimited tag data from YouTubeSources
INSERT INTO dbo.SourceTags (SourceId, SourceType, Tag)
SELECT y.Id, 'YouTube', LTRIM(RTRIM(value))
FROM dbo.YouTubeSources y
CROSS APPLY STRING_SPLIT(y.Tags, ',')
WHERE y.Tags IS NOT NULL AND y.Tags != '';
GO

-- NOTE: The Tags column on SyndicationFeedSources and YouTubeSources is retained
--       for backward compatibility. It will be removed in a future migration once
--       all consumers have been verified to use SourceTags.

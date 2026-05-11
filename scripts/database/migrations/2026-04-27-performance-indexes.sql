-- Migration: 2026-04-27-performance-indexes.sql
-- Adds 15 performance indexes for sort/filter columns introduced in commit 2bfcfb4.
-- Covers Engagements, SyndicationFeedItems, YouTubeItems, ScheduledItems,
-- and SocialMediaPlatforms (Issue #855).
-- Idempotent: safe to run multiple times.

USE JJGNet;
GO

-- ============================================================
-- Engagements
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Engagements_StartDateTime' AND object_id = OBJECT_ID('dbo.Engagements'))
BEGIN
    CREATE INDEX IX_Engagements_StartDateTime ON dbo.Engagements (StartDateTime DESC) INCLUDE (Name, EndDateTime, CreatedByEntraOid);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Engagements_CreatedByEntraOid' AND object_id = OBJECT_ID('dbo.Engagements'))
BEGIN
    CREATE INDEX IX_Engagements_CreatedByEntraOid ON dbo.Engagements (CreatedByEntraOid);
END
GO

-- ============================================================
-- SyndicationFeedItems
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedItems_Title' AND object_id = OBJECT_ID('dbo.SyndicationFeedItems'))
BEGIN
    CREATE INDEX IX_SyndicationFeedItems_Title ON dbo.SyndicationFeedItems (Title);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedItems_Author' AND object_id = OBJECT_ID('dbo.SyndicationFeedItems'))
BEGIN
    CREATE INDEX IX_SyndicationFeedItems_Author ON dbo.SyndicationFeedItems (Author);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedItems_PublicationDate' AND object_id = OBJECT_ID('dbo.SyndicationFeedItems'))
BEGIN
    CREATE INDEX IX_SyndicationFeedItems_PublicationDate ON dbo.SyndicationFeedItems (PublicationDate DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedItems_AddedOn' AND object_id = OBJECT_ID('dbo.SyndicationFeedItems'))
BEGIN
    CREATE INDEX IX_SyndicationFeedItems_AddedOn ON dbo.SyndicationFeedItems (AddedOn DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedItems_CreatedByEntraOid' AND object_id = OBJECT_ID('dbo.SyndicationFeedItems'))
BEGIN
    CREATE INDEX IX_SyndicationFeedItems_CreatedByEntraOid ON dbo.SyndicationFeedItems (CreatedByEntraOid);
END
GO

-- ============================================================
-- YouTubeItems
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeItems_Title' AND object_id = OBJECT_ID('dbo.YouTubeItems'))
BEGIN
    CREATE INDEX IX_YouTubeItems_Title ON dbo.YouTubeItems (Title);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeItems_Author' AND object_id = OBJECT_ID('dbo.YouTubeItems'))
BEGIN
    CREATE INDEX IX_YouTubeItems_Author ON dbo.YouTubeItems (Author);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeItems_PublicationDate' AND object_id = OBJECT_ID('dbo.YouTubeItems'))
BEGIN
    CREATE INDEX IX_YouTubeItems_PublicationDate ON dbo.YouTubeItems (PublicationDate DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeItems_AddedOn' AND object_id = OBJECT_ID('dbo.YouTubeItems'))
BEGIN
    CREATE INDEX IX_YouTubeItems_AddedOn ON dbo.YouTubeItems (AddedOn DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeItems_CreatedByEntraOid' AND object_id = OBJECT_ID('dbo.YouTubeItems'))
BEGIN
    CREATE INDEX IX_YouTubeItems_CreatedByEntraOid ON dbo.YouTubeItems (CreatedByEntraOid);
END
GO

-- ============================================================
-- ScheduledItems
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ScheduledItems_SendOnDateTime' AND object_id = OBJECT_ID('dbo.ScheduledItems'))
BEGIN
    CREATE INDEX IX_ScheduledItems_SendOnDateTime ON dbo.ScheduledItems (SendOnDateTime DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ScheduledItems_CreatedByEntraOid' AND object_id = OBJECT_ID('dbo.ScheduledItems'))
BEGIN
    CREATE INDEX IX_ScheduledItems_CreatedByEntraOid ON dbo.ScheduledItems (CreatedByEntraOid);
END
GO

-- ============================================================
-- SocialMediaPlatforms
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SocialMediaPlatforms_IsActive_Name' AND object_id = OBJECT_ID('dbo.SocialMediaPlatforms'))
BEGIN
    CREATE INDEX IX_SocialMediaPlatforms_IsActive_Name ON dbo.SocialMediaPlatforms (IsActive, Name);
END
GO

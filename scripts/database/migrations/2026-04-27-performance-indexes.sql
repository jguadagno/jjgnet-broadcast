-- Migration: 2026-04-27-performance-indexes.sql
-- Adds 15 performance indexes for sort/filter columns introduced in commit 2bfcfb4.
-- Covers Engagements, SyndicationFeedSources, YouTubeSources, ScheduledItems,
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
-- SyndicationFeedSources
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedSources_Title' AND object_id = OBJECT_ID('dbo.SyndicationFeedSources'))
BEGIN
    CREATE INDEX IX_SyndicationFeedSources_Title ON dbo.SyndicationFeedSources (Title);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedSources_Author' AND object_id = OBJECT_ID('dbo.SyndicationFeedSources'))
BEGIN
    CREATE INDEX IX_SyndicationFeedSources_Author ON dbo.SyndicationFeedSources (Author);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedSources_PublicationDate' AND object_id = OBJECT_ID('dbo.SyndicationFeedSources'))
BEGIN
    CREATE INDEX IX_SyndicationFeedSources_PublicationDate ON dbo.SyndicationFeedSources (PublicationDate DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedSources_AddedOn' AND object_id = OBJECT_ID('dbo.SyndicationFeedSources'))
BEGIN
    CREATE INDEX IX_SyndicationFeedSources_AddedOn ON dbo.SyndicationFeedSources (AddedOn DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedSources_CreatedByEntraOid' AND object_id = OBJECT_ID('dbo.SyndicationFeedSources'))
BEGIN
    CREATE INDEX IX_SyndicationFeedSources_CreatedByEntraOid ON dbo.SyndicationFeedSources (CreatedByEntraOid);
END
GO

-- ============================================================
-- YouTubeSources
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeSources_Title' AND object_id = OBJECT_ID('dbo.YouTubeSources'))
BEGIN
    CREATE INDEX IX_YouTubeSources_Title ON dbo.YouTubeSources (Title);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeSources_Author' AND object_id = OBJECT_ID('dbo.YouTubeSources'))
BEGIN
    CREATE INDEX IX_YouTubeSources_Author ON dbo.YouTubeSources (Author);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeSources_PublicationDate' AND object_id = OBJECT_ID('dbo.YouTubeSources'))
BEGIN
    CREATE INDEX IX_YouTubeSources_PublicationDate ON dbo.YouTubeSources (PublicationDate DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeSources_AddedOn' AND object_id = OBJECT_ID('dbo.YouTubeSources'))
BEGIN
    CREATE INDEX IX_YouTubeSources_AddedOn ON dbo.YouTubeSources (AddedOn DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeSources_CreatedByEntraOid' AND object_id = OBJECT_ID('dbo.YouTubeSources'))
BEGIN
    CREATE INDEX IX_YouTubeSources_CreatedByEntraOid ON dbo.YouTubeSources (CreatedByEntraOid);
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

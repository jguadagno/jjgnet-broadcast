-- Migration: Enforce NOT NULL on CreatedByEntraOid ownership columns
-- Issue: #726
-- Date: 2026-04-17
-- Description: After all records have been backfilled (migration 2026-04-17-backfill-owner-oid.sql),
--              tighten the constraint to NOT NULL on both source tables.

USE JJGNet;
GO

-- ============================================================
-- SyndicationFeedItems: NULL → NOT NULL
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[SyndicationFeedItems]')
      AND name = 'CreatedByEntraOid'
      AND is_nullable = 1
)
BEGIN
    PRINT 'Altering SyndicationFeedItems.CreatedByEntraOid to NOT NULL';
    ALTER TABLE [dbo].[SyndicationFeedItems]
        ALTER COLUMN [CreatedByEntraOid] NVARCHAR(36) NOT NULL;
END
ELSE
BEGIN
    PRINT 'SyndicationFeedItems.CreatedByEntraOid is already NOT NULL or does not exist';
END
GO

-- ============================================================
-- YouTubeItems: NULL → NOT NULL
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[YouTubeItems]')
      AND name = 'CreatedByEntraOid'
      AND is_nullable = 1
)
BEGIN
    PRINT 'Altering YouTubeItems.CreatedByEntraOid to NOT NULL';
    ALTER TABLE [dbo].[YouTubeItems]
        ALTER COLUMN [CreatedByEntraOid] NVARCHAR(36) NOT NULL;
END
ELSE
BEGIN
    PRINT 'YouTubeItems.CreatedByEntraOid is already NOT NULL or does not exist';
END
GO

PRINT 'Migration completed: CreatedByEntraOid NOT NULL (#726)';
GO

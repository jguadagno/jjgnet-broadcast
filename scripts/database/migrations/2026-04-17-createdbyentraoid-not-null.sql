-- Migration: Enforce NOT NULL on CreatedByEntraOid ownership columns
-- Issue: #726
-- Date: 2026-04-17
-- Description: After all records have been backfilled (migration 2026-04-17-backfill-owner-oid.sql),
--              tighten the constraint to NOT NULL on both source tables.

USE JJGNet;
GO

-- ============================================================
-- SyndicationFeedSources: NULL → NOT NULL
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[SyndicationFeedSources]')
      AND name = 'CreatedByEntraOid'
      AND is_nullable = 1
)
BEGIN
    PRINT 'Altering SyndicationFeedSources.CreatedByEntraOid to NOT NULL';
    ALTER TABLE [dbo].[SyndicationFeedSources]
        ALTER COLUMN [CreatedByEntraOid] NVARCHAR(36) NOT NULL;
END
ELSE
BEGIN
    PRINT 'SyndicationFeedSources.CreatedByEntraOid is already NOT NULL or does not exist';
END
GO

-- ============================================================
-- YouTubeSources: NULL → NOT NULL
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[YouTubeSources]')
      AND name = 'CreatedByEntraOid'
      AND is_nullable = 1
)
BEGIN
    PRINT 'Altering YouTubeSources.CreatedByEntraOid to NOT NULL';
    ALTER TABLE [dbo].[YouTubeSources]
        ALTER COLUMN [CreatedByEntraOid] NVARCHAR(36) NOT NULL;
END
ELSE
BEGIN
    PRINT 'YouTubeSources.CreatedByEntraOid is already NOT NULL or does not exist';
END
GO

PRINT 'Migration completed: CreatedByEntraOid NOT NULL (#726)';
GO

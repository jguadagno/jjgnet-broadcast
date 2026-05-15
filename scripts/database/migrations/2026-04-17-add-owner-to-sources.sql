-- Migration: Add CreatedByEntraOid ownership columns to source tables
-- Issue: #725
-- Date: 2026-04-17
-- Description: Extends RBAC ownership tracking to SyndicationFeedItems and YouTubeItems
--              to support ownership-based delete rules: Contributors can delete only their
--              own content, Administrators can delete any content.
--              Column is nullable to handle existing records gracefully.

USE JJGNet;
GO

-- ============================================================
-- Add CreatedByEntraOid to SyndicationFeedItems
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[SyndicationFeedItems]')
      AND name = 'CreatedByEntraOid')
BEGIN
    ALTER TABLE [dbo].[SyndicationFeedItems]
        ADD [CreatedByEntraOid] NVARCHAR(36) NULL;
END
GO

-- ============================================================
-- Add CreatedByEntraOid to YouTubeItems
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[YouTubeItems]')
      AND name = 'CreatedByEntraOid')
BEGIN
    ALTER TABLE [dbo].[YouTubeItems]
        ADD [CreatedByEntraOid] NVARCHAR(36) NULL;
END
GO

-- ============================================================
-- NOTES ON NULLABLE COLUMN DECISION
-- ============================================================
-- CreatedByEntraOid is nullable for backward compatibility with existing data.
-- New content created after deployment will capture the creator's
-- Entra Object ID from the authenticated user's oid claim.
-- 
-- Existing records without ownership:
-- - Can be deleted by Administrators
-- - Cannot be deleted by Contributors (no ownership = not their content)
-- 
-- Issue #726 provides a backfill migration to assign ownership to existing records.
-- ============================================================

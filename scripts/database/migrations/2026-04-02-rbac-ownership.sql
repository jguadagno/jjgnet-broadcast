-- Migration: Add CreatedByEntraOid ownership columns to content tables
-- Issue: #607
-- Date: 2026-04-02
-- Description: Phase 2 of RBAC implementation. Adds ownership tracking to content tables
--              to support ownership-based delete rules: Contributors can delete only their
--              own content, Administrators can delete any content.
--              Column is nullable to handle existing records gracefully.

USE JJGNet;
GO

-- ============================================================
-- Add CreatedByEntraOid to Engagements
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Engagements]')
      AND name = 'CreatedByEntraOid')
BEGIN
    ALTER TABLE [dbo].[Engagements]
        ADD [CreatedByEntraOid] NVARCHAR(36) NULL;
END
GO

-- ============================================================
-- Add CreatedByEntraOid to Talks
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Talks]')
      AND name = 'CreatedByEntraOid')
BEGIN
    ALTER TABLE [dbo].[Talks]
        ADD [CreatedByEntraOid] NVARCHAR(36) NULL;
END
GO

-- ============================================================
-- Add CreatedByEntraOid to ScheduledItems
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ScheduledItems]')
      AND name = 'CreatedByEntraOid')
BEGIN
    ALTER TABLE [dbo].[ScheduledItems]
        ADD [CreatedByEntraOid] NVARCHAR(36) NULL;
END
GO

-- ============================================================
-- Add CreatedByEntraOid to MessageTemplates
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[MessageTemplates]')
      AND name = 'CreatedByEntraOid')
BEGIN
    ALTER TABLE [dbo].[MessageTemplates]
        ADD [CreatedByEntraOid] NVARCHAR(36) NULL;
END
GO

-- ============================================================
-- NOTES ON NULLABLE COLUMN DECISION
-- ============================================================
-- CreatedByEntraOid is nullable for backward compatibility with existing data.
-- New content created after Phase 2 deployment will capture the creator's
-- Entra Object ID from the authenticated user's oid claim.
-- 
-- Existing records without ownership:
-- - Can be deleted by Administrators
-- - Cannot be deleted by Contributors (no ownership = not their content)
-- 
-- A future Phase 2.5 migration could backfill ownership for existing records
-- if historical audit logs or creation metadata are available.
-- ============================================================

-- Migration: Backfill CreatedByEntraOid for all existing records
-- Issue: #726
-- Date: 2026-04-17
-- Description: Sets CreatedByEntraOid on all existing rows with NULL values.
--              All existing content was created by Joseph Guadagno (the only current user).
--              This migration is idempotent - only NULL rows are updated, safe to re-run.
--
-- REQUIRED ACTION BEFORE RUNNING:
--   Replace 'PLACEHOLDER_OID' below with Joseph Guadagno's actual Entra Object ID (oid claim).
--   You can find this value in Azure Portal > Entra ID > Users > Joseph Guadagno > Object ID
--   or by inspecting the oid claim from an authenticated session.

USE JJGNet;
GO

-- ============================================================
-- Configuration - UPDATE THIS VALUE BEFORE RUNNING
-- ============================================================
DECLARE @OwnerOid NVARCHAR(36) = 'PLACEHOLDER_OID';

-- Validate that a real OID was provided
IF @OwnerOid = 'PLACEHOLDER_OID'
BEGIN
    RAISERROR('ERROR: @OwnerOid must be updated with the actual Entra Object ID before running this script.', 16, 1);
    RETURN;
END
GO

-- Re-declare for each batch
DECLARE @OwnerOid NVARCHAR(36) = 'PLACEHOLDER_OID';

-- ============================================================
-- Backfill Engagements
-- ============================================================
UPDATE [dbo].[Engagements]
SET [CreatedByEntraOid] = @OwnerOid
WHERE [CreatedByEntraOid] IS NULL;

PRINT CONCAT('Updated ', @@ROWCOUNT, ' Engagements records');
GO

DECLARE @OwnerOid NVARCHAR(36) = 'PLACEHOLDER_OID';

-- ============================================================
-- Backfill Talks
-- ============================================================
UPDATE [dbo].[Talks]
SET [CreatedByEntraOid] = @OwnerOid
WHERE [CreatedByEntraOid] IS NULL;

PRINT CONCAT('Updated ', @@ROWCOUNT, ' Talks records');
GO

DECLARE @OwnerOid NVARCHAR(36) = 'PLACEHOLDER_OID';

-- ============================================================
-- Backfill ScheduledItems
-- ============================================================
UPDATE [dbo].[ScheduledItems]
SET [CreatedByEntraOid] = @OwnerOid
WHERE [CreatedByEntraOid] IS NULL;

PRINT CONCAT('Updated ', @@ROWCOUNT, ' ScheduledItems records');
GO

DECLARE @OwnerOid NVARCHAR(36) = 'PLACEHOLDER_OID';

-- ============================================================
-- Backfill MessageTemplates
-- ============================================================
UPDATE [dbo].[MessageTemplates]
SET [CreatedByEntraOid] = @OwnerOid
WHERE [CreatedByEntraOid] IS NULL;

PRINT CONCAT('Updated ', @@ROWCOUNT, ' MessageTemplates records');
GO

DECLARE @OwnerOid NVARCHAR(36) = 'PLACEHOLDER_OID';

-- ============================================================
-- Backfill SyndicationFeedSources
-- ============================================================
UPDATE [dbo].[SyndicationFeedSources]
SET [CreatedByEntraOid] = @OwnerOid
WHERE [CreatedByEntraOid] IS NULL;

PRINT CONCAT('Updated ', @@ROWCOUNT, ' SyndicationFeedSources records');
GO

DECLARE @OwnerOid NVARCHAR(36) = 'PLACEHOLDER_OID';

-- ============================================================
-- Backfill YouTubeSources
-- ============================================================
UPDATE [dbo].[YouTubeSources]
SET [CreatedByEntraOid] = @OwnerOid
WHERE [CreatedByEntraOid] IS NULL;

PRINT CONCAT('Updated ', @@ROWCOUNT, ' YouTubeSources records');
GO

-- ============================================================
-- Summary
-- ============================================================
PRINT 'Backfill complete. All NULL CreatedByEntraOid values have been updated.';
GO

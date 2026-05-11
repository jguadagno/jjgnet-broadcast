-- Migration: Add EntraOId to FeedChecks for per-user separation
-- Issue: #950 (sanity check / user separation)
-- Date: 2026-05-11
--
-- Design notes:
--   - Empty string ('') is the EntraOId value for system-level timer-based feed checks
--     (LoadNewSpeakingEngagements, ScheduledItems) that have no per-user context.
--   - User-specific collectors (LoadNewPosts, LoadNewVideos) will use the owner OID
--     resolved from their source records.
--   - Existing rows are migrated with EntraOId = '' (system default).
--
use JJGNet
go

-- Step 1: Add the EntraOId column (nullable first to allow populating existing rows)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.FeedChecks') AND name = 'EntraOId'
)
BEGIN
    ALTER TABLE dbo.FeedChecks
        ADD EntraOId nvarchar(36) NULL;
END
go

-- Step 2: Populate existing rows with empty string (system default)
UPDATE dbo.FeedChecks
SET EntraOId = 'ce03be17-54af-4fd7-9762-7a367dcdc0df'
WHERE EntraOId IS NULL;
go

-- Step 3: Make the column NOT NULL with a default
ALTER TABLE dbo.FeedChecks
    ALTER COLUMN EntraOId nvarchar(36) NOT NULL;
go

ALTER TABLE dbo.FeedChecks
    ADD CONSTRAINT DF_FeedChecks_EntraOId DEFAULT ('') FOR EntraOId;
go

-- Step 4: Drop the old unique constraint on Name alone
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'FeedChecks_Unique_Name'
      AND object_id = OBJECT_ID('dbo.FeedChecks')
)
BEGIN
    ALTER TABLE dbo.FeedChecks
        DROP CONSTRAINT FeedChecks_Unique_Name;
END
go

-- Step 5: Add new composite unique constraint on (Name, EntraOId)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_FeedChecks_Name_EntraOId'
      AND object_id = OBJECT_ID('dbo.FeedChecks')
)
BEGIN
    ALTER TABLE dbo.FeedChecks
        ADD CONSTRAINT UQ_FeedChecks_Name_EntraOId UNIQUE (Name, EntraOId);
END
go

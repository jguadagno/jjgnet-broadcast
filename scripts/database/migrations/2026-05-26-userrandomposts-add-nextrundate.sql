-- Migration: 2026-05-26 — Add NextRunDateUtc to UserRandomPostSettings (Issue #995 NextRunDate efficiency fix)
-- Adds NextRunDateUtc column and a filtered index so the RandomPosts Azure Function
-- can query only due rows in SQL instead of loading all active rows and parsing
-- CRON expressions in C# to decide which ones fired in the last minute.

-- ============================================================
-- Add NextRunDateUtc column
-- NULL = never run yet; these rows are always included by GetAllDueAsync.
-- ============================================================
IF COL_LENGTH('dbo.UserRandomPostSettings', 'NextRunDateUtc') IS NULL
BEGIN
    ALTER TABLE [dbo].[UserRandomPostSettings]
    ADD [NextRunDateUtc] DATETIMEOFFSET NULL;
    PRINT 'Added column NextRunDateUtc to UserRandomPostSettings';
END
ELSE
BEGIN
    PRINT 'Column NextRunDateUtc already exists on UserRandomPostSettings — skipped';
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserRandomPostSettings' AND COLUMN_NAME = 'CronParseFailureCount')
BEGIN
    ALTER TABLE [dbo].[UserRandomPostSettings] ADD [CronParseFailureCount] INT NOT NULL DEFAULT 0;
    PRINT 'Added CronParseFailureCount column to UserRandomPostSettings';
END
ELSE
BEGIN
    PRINT 'CronParseFailureCount column already exists — skipped';
END
GO

-- ============================================================
-- Filtered index to accelerate GetAllDueAsync
-- Covers: IsActive = 1 AND (NextRunDateUtc IS NULL OR NextRunDateUtc <= @utcNow)
-- ============================================================
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UserRandomPostSettings_IsActive_NextRunDateUtc'
      AND object_id = OBJECT_ID('dbo.UserRandomPostSettings')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserRandomPostSettings_IsActive_NextRunDateUtc
        ON [dbo].[UserRandomPostSettings] ([IsActive], [NextRunDateUtc])
        WHERE [IsActive] = 1;
    PRINT 'Created index IX_UserRandomPostSettings_IsActive_NextRunDateUtc on UserRandomPostSettings';
END
ELSE
BEGIN
    PRINT 'Index IX_UserRandomPostSettings_IsActive_NextRunDateUtc already exists — skipped';
END
GO

-- Migration: 2026-03-16-fix-clustered-indexes.sql
-- Fixes Engagements, Talks, and ScheduledItems tables which had NONCLUSTERED PKs
-- on heap tables. Drops the nonclustered PK and re-creates it as CLUSTERED.
-- Also adds missing indexes for the hot-path scheduled items query and the
-- Talks -> Engagements FK (addresses #295, #296, #299).
-- Idempotent: safe to run multiple times.

USE JJGNet;
GO

-- ============================================================
-- Engagements
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'Engagements_pk'
      AND object_id = OBJECT_ID('dbo.Engagements')
      AND type_desc = 'NONCLUSTERED'
)
BEGIN
    ALTER TABLE dbo.Engagements DROP CONSTRAINT Engagements_pk;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'Engagements_pk'
      AND object_id = OBJECT_ID('dbo.Engagements')
      AND type_desc = 'CLUSTERED'
)
BEGIN
    ALTER TABLE dbo.Engagements ADD CONSTRAINT Engagements_pk PRIMARY KEY CLUSTERED (Id ASC);
END
GO

-- ============================================================
-- Talks
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'Talks_pk'
      AND object_id = OBJECT_ID('dbo.Talks')
      AND type_desc = 'NONCLUSTERED'
)
BEGIN
    ALTER TABLE dbo.Talks DROP CONSTRAINT Talks_pk;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'Talks_pk'
      AND object_id = OBJECT_ID('dbo.Talks')
      AND type_desc = 'CLUSTERED'
)
BEGIN
    ALTER TABLE dbo.Talks ADD CONSTRAINT Talks_pk PRIMARY KEY CLUSTERED (Id ASC);
END
GO

-- ============================================================
-- ScheduledItems
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ScheduledItems_pk'
      AND object_id = OBJECT_ID('dbo.ScheduledItems')
      AND type_desc = 'NONCLUSTERED'
)
BEGIN
    ALTER TABLE dbo.ScheduledItems DROP CONSTRAINT ScheduledItems_pk;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'ScheduledItems_pk'
      AND object_id = OBJECT_ID('dbo.ScheduledItems')
      AND type_desc = 'CLUSTERED'
)
BEGIN
    ALTER TABLE dbo.ScheduledItems ADD CONSTRAINT ScheduledItems_pk PRIMARY KEY CLUSTERED (Id ASC);
END
GO

-- ============================================================
-- Missing index: hot-path pending scheduled items query (#296)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ScheduledItems_Pending'
      AND object_id = OBJECT_ID('dbo.ScheduledItems')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ScheduledItems_Pending
        ON dbo.ScheduledItems (MessageSent ASC, SendOnDateTime ASC)
        INCLUDE (ItemTableName, ItemPrimaryKey, Message);
END
GO

-- ============================================================
-- Missing index: Talks -> Engagements FK column (#299)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Talks_EngagementId'
      AND object_id = OBJECT_ID('dbo.Talks')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Talks_EngagementId
        ON dbo.Talks (EngagementId ASC);
END
GO

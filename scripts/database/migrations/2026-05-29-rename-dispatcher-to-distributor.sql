-- ============================================================
-- Migration: 2026-05-29 — Rename UserEventDispatcherMappings → UserEventDistributorMappings
-- Terminology cleanup: "Dispatcher" → "Distributor" for the routing layer.
-- Idempotent: each step is guarded by an IF EXISTS check.
-- ============================================================

-- ============================================================
-- 1. Rename the table
-- ============================================================
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserEventDispatcherMappings')
    EXEC sp_rename 'dbo.UserEventDispatcherMappings', 'UserEventDistributorMappings';
GO

-- ============================================================
-- 2. Rename default constraints
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserEventDispatcherMapping_IsActive')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.DF_UserEventDispatcherMapping_IsActive', 'DF_UserEventDistributorMapping_IsActive', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserEventDispatcherMapping_CreatedOn')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.DF_UserEventDispatcherMapping_CreatedOn', 'DF_UserEventDistributorMapping_CreatedOn', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserEventDispatcherMapping_LastUpdatedOn')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.DF_UserEventDispatcherMapping_LastUpdatedOn', 'DF_UserEventDistributorMapping_LastUpdatedOn', 'OBJECT';
GO

-- ============================================================
-- 3. Rename primary key constraint
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'PK_UserEventDispatcherMapping')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.PK_UserEventDispatcherMapping', 'PK_UserEventDistributorMapping', 'OBJECT';
GO

-- ============================================================
-- 4. Rename foreign key constraint
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'FK_UserEventDispatcherMapping_SocialMediaPlatforms')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.FK_UserEventDispatcherMapping_SocialMediaPlatforms', 'FK_UserEventDistributorMapping_SocialMediaPlatforms', 'OBJECT';
GO

-- ============================================================
-- 5. Rename unique constraint
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_UserEventDispatcherMapping_Owner_Event_Platform')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.UQ_UserEventDispatcherMapping_Owner_Event_Platform', 'UQ_UserEventDistributorMapping_Owner_Event_Platform', 'OBJECT';
GO

-- ============================================================
-- 6. Rename check constraint
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'CK_UserEventDispatcherMapping_EventType')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.CK_UserEventDispatcherMapping_EventType', 'CK_UserEventDistributorMapping_EventType', 'OBJECT';
GO

-- ============================================================
-- 7. Rename index
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserEventDispatcherMapping_Active' AND object_id = OBJECT_ID('dbo.UserEventDistributorMappings'))
    EXEC sp_rename 'dbo.UserEventDistributorMappings.IX_UserEventDispatcherMapping_Active', 'IX_UserEventDistributorMapping_Active', 'INDEX';
GO

-- ============================================================
-- 8. Update FeedChecks seed row
-- ============================================================
UPDATE dbo.FeedChecks SET Name = 'DistributorsScheduledItems' WHERE Name = 'DispatchersScheduledItems';
GO

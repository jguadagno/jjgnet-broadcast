-- ============================================================
-- Migration: 2026-05-29 — Rename routing-layer table to UserEventDistributorMappings
-- Terminology cleanup: canonical flow is Collector → Distributor → Publisher.
-- Handles two possible starting states:
--   A) UserEventDispatcherMappings  — DB went through the intermediate dispatcher rename
--   B) UserEventPublisherMapping    — DB never had the dispatcher rename applied
-- Idempotent: each step is guarded by an IF EXISTS check.
-- ============================================================

-- ============================================================
-- PATH A: UserEventDispatcherMappings → UserEventDistributorMappings
-- (DB previously renamed from UserEventPublisherMapping to UserEventDispatcherMappings)
-- ============================================================

-- A.1 Rename the table
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserEventDispatcherMappings')
    EXEC sp_rename 'dbo.UserEventDispatcherMappings', 'UserEventDistributorMappings';
GO

-- A.2 Rename default constraints
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserEventDispatcherMapping_IsActive')
    EXEC sp_rename 'DF_UserEventDispatcherMapping_IsActive', 'DF_UserEventDistributorMapping_IsActive', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserEventDispatcherMapping_CreatedOn')
    EXEC sp_rename 'DF_UserEventDispatcherMapping_CreatedOn', 'DF_UserEventDistributorMapping_CreatedOn', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserEventDispatcherMapping_LastUpdatedOn')
    EXEC sp_rename 'DF_UserEventDispatcherMapping_LastUpdatedOn', 'DF_UserEventDistributorMapping_LastUpdatedOn', 'OBJECT';
GO

-- A.3 Rename primary key constraint
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'PK_UserEventDispatcherMapping')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.PK_UserEventDispatcherMapping', 'PK_UserEventDistributorMapping', 'OBJECT';
GO

-- A.4 Rename foreign key constraint
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'FK_UserEventDispatcherMapping_SocialMediaPlatforms')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.FK_UserEventDispatcherMapping_SocialMediaPlatforms', 'FK_UserEventDistributorMapping_SocialMediaPlatforms', 'OBJECT';
GO

-- A.5 Rename unique constraint
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_UserEventDispatcherMapping_Owner_Event_Platform')
    EXEC sp_rename 'UQ_UserEventDispatcherMapping_Owner_Event_Platform', 'UQ_UserEventDistributorMapping_Owner_Event_Platform', 'OBJECT';
GO

-- A.6 Rename check constraint
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'CK_UserEventDispatcherMapping_EventType')
    EXEC sp_rename 'CK_UserEventDispatcherMapping_EventType', 'CK_UserEventDistributorMapping_EventType', 'OBJECT';
GO

-- A.7 Rename index
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserEventDispatcherMapping_Active' AND object_id = OBJECT_ID('dbo.UserEventDistributorMappings'))
    EXEC sp_rename 'IX_UserEventDispatcherMapping_Active', 'IX_UserEventDistributorMapping_Active', 'INDEX';
GO

-- A.8 Update FeedChecks seed row
UPDATE dbo.FeedChecks SET Name = 'DistributorsScheduledItems' WHERE Name = 'DispatchersScheduledItems';
GO

-- ============================================================
-- PATH B: UserEventPublisherMapping → UserEventDistributorMappings
-- (DB still has the original Publisher name — dispatcher step was never applied)
-- ============================================================

-- B.1 Rename the table
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserEventPublisherMapping')
    EXEC sp_rename 'dbo.UserEventPublisherMapping', 'UserEventDistributorMappings';
GO

-- B.2 Rename default constraints
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserEventPublisherMapping_IsActive')
    EXEC sp_rename 'DF_UserEventPublisherMapping_IsActive', 'DF_UserEventDistributorMapping_IsActive', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserEventPublisherMapping_CreatedOn')
    EXEC sp_rename 'DF_UserEventPublisherMapping_CreatedOn', 'DF_UserEventDistributorMapping_CreatedOn', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserEventPublisherMapping_LastUpdatedOn')
    EXEC sp_rename 'DF_UserEventPublisherMapping_LastUpdatedOn', 'DF_UserEventDistributorMapping_LastUpdatedOn', 'OBJECT';
GO

-- B.3 Rename primary key constraint
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'PK_UserEventPublisherMapping')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.PK_UserEventPublisherMapping', 'PK_UserEventDistributorMapping', 'OBJECT';
GO

-- B.4 Rename foreign key constraint
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'FK_UserEventPublisherMapping_SocialMediaPlatforms')
    EXEC sp_rename 'dbo.UserEventDistributorMappings.FK_UserEventPublisherMapping_SocialMediaPlatforms', 'FK_UserEventDistributorMapping_SocialMediaPlatforms', 'OBJECT';
GO

-- B.5 Rename unique constraint
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_UserEventPublisherMapping_Owner_Event_Platform')
    EXEC sp_rename 'UQ_UserEventPublisherMapping_Owner_Event_Platform', 'UQ_UserEventDistributorMapping_Owner_Event_Platform', 'OBJECT';
GO

-- B.6 Rename check constraint
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'CK_UserEventPublisherMapping_EventType')
    EXEC sp_rename 'CK_UserEventPublisherMapping_EventType', 'CK_UserEventDistributorMapping_EventType', 'OBJECT';
GO

-- B.7 Rename index
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserEventPublisherMapping_Active' AND object_id = OBJECT_ID('dbo.UserEventDistributorMappings'))
    EXEC sp_rename 'IX_UserEventPublisherMapping_Active', 'IX_UserEventDistributorMapping_Active', 'INDEX';
GO

-- B.8 Update FeedChecks seed row
UPDATE dbo.FeedChecks SET Name = 'DistributorsScheduledItems' WHERE Name = 'PublishersScheduledItems';
GO

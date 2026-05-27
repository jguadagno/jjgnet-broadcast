-- Rename table
EXEC sp_rename 'dbo.UserEventPublisherMappings', 'UserEventDispatcherMappings';
GO

-- Rename default constraints
EXEC sp_rename 'dbo.UserEventDispatcherMappings.DF_UserEventPublisherMapping_IsActive', 'DF_UserEventDispatcherMapping_IsActive', 'OBJECT';
GO

EXEC sp_rename 'dbo.UserEventDispatcherMappings.DF_UserEventPublisherMapping_CreatedOn', 'DF_UserEventDispatcherMapping_CreatedOn', 'OBJECT';
GO

EXEC sp_rename 'dbo.UserEventDispatcherMappings.DF_UserEventPublisherMapping_LastUpdatedOn', 'DF_UserEventDispatcherMapping_LastUpdatedOn', 'OBJECT';
GO

-- Rename primary key constraint
EXEC sp_rename 'dbo.UserEventDispatcherMappings.PK_UserEventPublisherMapping', 'PK_UserEventDispatcherMapping', 'OBJECT';
GO

-- Rename unique constraint
EXEC sp_rename 'dbo.UserEventDispatcherMappings.UQ_UserEventPublisherMapping_Owner_Event_Platform', 'UQ_UserEventDispatcherMapping_Owner_Event_Platform', 'OBJECT';
GO

-- Rename FK
EXEC sp_rename 'dbo.UserEventDispatcherMappings.FK_UserEventPublisherMapping_SocialMediaPlatforms', 'FK_UserEventDispatcherMapping_SocialMediaPlatforms', 'OBJECT';
GO

-- Rename check constraint
EXEC sp_rename 'dbo.UserEventDispatcherMappings.CK_UserEventPublisherMapping_EventType', 'CK_UserEventDispatcherMapping_EventType', 'OBJECT';
GO

-- Rename index
EXEC sp_rename 'dbo.UserEventDispatcherMappings.IX_UserEventPublisherMapping_Active', 'IX_UserEventDispatcherMapping_Active', 'INDEX';
GO

-- Data migration: update FeedChecks row
UPDATE dbo.FeedChecks SET Name = 'DispatchersScheduledItems' WHERE Name = 'PublishersScheduledItems';
GO

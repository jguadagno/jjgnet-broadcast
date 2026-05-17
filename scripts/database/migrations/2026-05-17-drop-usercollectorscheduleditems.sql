use JJGNet
go

-- Removes the UserCollectorScheduledItems table and its associated index.
-- This table was introduced in 2026-05-11-user-collector-speaking-scheduled.sql
-- but the feature was removed: scheduled items are manually entered by users
-- and do not require per-user collector settings.

if exists (select 1 from sys.indexes where name = 'IX_UserCollectorScheduledItems_Owner'
    and object_id = object_id('dbo.UserCollectorScheduledItems'))
begin
    drop index IX_UserCollectorScheduledItems_Owner on dbo.UserCollectorScheduledItems
end
go

if exists (select 1 from sys.tables where name = 'UserCollectorScheduledItems' and schema_id = schema_id('dbo'))
begin
    drop table dbo.UserCollectorScheduledItems
end
go

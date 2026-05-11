-- Migration: Add ApiKey and ResultSetPageSize to UserCollectorYouTubeChannels
-- ApiKey:            nullable nvarchar(255); user supplies their own Google API key.
-- ResultSetPageSize: non-nullable int with default 50; valid range is 1–200.

-- Add ApiKey column if not already present
if not exists (select 1 from sys.columns where object_id = object_id('dbo.UserCollectorYouTubeChannels') and name = 'ApiKey')
begin
    alter table dbo.UserCollectorYouTubeChannels
        add ApiKey nvarchar(255) null
end
go

-- Add ResultSetPageSize column if not already present
if not exists (select 1 from sys.columns where object_id = object_id('dbo.UserCollectorYouTubeChannels') and name = 'ResultSetPageSize')
begin
    alter table dbo.UserCollectorYouTubeChannels
        add ResultSetPageSize int not null default 50
end
go

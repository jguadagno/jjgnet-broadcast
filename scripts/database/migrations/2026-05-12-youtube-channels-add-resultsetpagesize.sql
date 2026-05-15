-- Add ResultSetPageSize to UserCollectorYouTubeChannels.
if not exists (select 1 from sys.columns
    where object_id = object_id('dbo.UserCollectorYouTubeChannels')
    and name = 'ResultSetPageSize')
begin
    alter table dbo.UserCollectorYouTubeChannels
        add ResultSetPageSize int not null default 50
end
go

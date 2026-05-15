-- Drop ApiKeySecretName from UserCollectorYouTubeChannels.
-- The secret name is now derived at runtime from EntraOId + ChannelId.
if exists (select 1 from sys.columns
    where object_id = object_id('dbo.UserCollectorYouTubeChannels')
    and name = 'ApiKeySecretName')
begin
    alter table dbo.UserCollectorYouTubeChannels
        drop column ApiKeySecretName
end
go

use JJGNet
go

-- Per-user RSS/Atom/JSON feed collector configurations.
-- Each row represents a single feed URL a user wants polled.
if not exists (select 1 from sys.tables where name = 'UserCollectorFeedSources' and schema_id = schema_id('dbo'))
begin
    create table dbo.UserCollectorFeedSources
    (
        Id                int identity
            constraint PK_UserCollectorFeedSources
                primary key clustered,
        CreatedByEntraOid nvarchar(36)    not null,
        FeedUrl           nvarchar(2048)  not null,
        DisplayName       nvarchar(255)   not null,
        IsActive          bit             not null
            constraint DF_UserCollectorFeedSources_IsActive
                default (1),
        CreatedOn         datetimeoffset  not null
            constraint DF_UserCollectorFeedSources_CreatedOn
                default (sysdatetimeoffset()),
        LastUpdatedOn     datetimeoffset  not null
            constraint DF_UserCollectorFeedSources_LastUpdatedOn
                default (sysdatetimeoffset()),
        constraint UQ_UserCollectorFeedSources_Owner_FeedUrl
            unique (CreatedByEntraOid, FeedUrl)
    )
end
go

if not exists (select 1 from sys.indexes where name = 'IX_UserCollectorFeedSources_Owner' and object_id = object_id('dbo.UserCollectorFeedSources'))
begin
    create nonclustered index IX_UserCollectorFeedSources_Owner
        on dbo.UserCollectorFeedSources (CreatedByEntraOid)
end
go

-- Per-user YouTube channel collector configurations.
-- Each row represents a YouTube channel ID a user wants polled.
if not exists (select 1 from sys.tables where name = 'UserCollectorYouTubeChannels' and schema_id = schema_id('dbo'))
begin
    create table dbo.UserCollectorYouTubeChannels
    (
        Id                int identity
            constraint PK_UserCollectorYouTubeChannels
                primary key clustered,
        CreatedByEntraOid nvarchar(36)   not null,
        ChannelId         nvarchar(50)   not null,
        DisplayName       nvarchar(255)  not null,
        IsActive          bit            not null
            constraint DF_UserCollectorYouTubeChannels_IsActive
                default (1),
        CreatedOn         datetimeoffset not null
            constraint DF_UserCollectorYouTubeChannels_CreatedOn
                default (sysdatetimeoffset()),
        LastUpdatedOn     datetimeoffset not null
            constraint DF_UserCollectorYouTubeChannels_LastUpdatedOn
                default (sysdatetimeoffset()),
        constraint UQ_UserCollectorYouTubeChannels_Owner_Channel
            unique (CreatedByEntraOid, ChannelId)
    )
end
go

if not exists (select 1 from sys.indexes where name = 'IX_UserCollectorYouTubeChannels_Owner' and object_id = object_id('dbo.UserCollectorYouTubeChannels'))
begin
    create nonclustered index IX_UserCollectorYouTubeChannels_Owner
        on dbo.UserCollectorYouTubeChannels (CreatedByEntraOid)
end
go

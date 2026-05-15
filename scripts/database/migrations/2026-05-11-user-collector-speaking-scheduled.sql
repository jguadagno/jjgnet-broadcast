use JJGNet
go

-- Per-user speaking engagements file URL collector configurations.
-- Each row represents a JSON file URL a user wants polled for speaking engagements.
if not exists (select 1 from sys.tables where name = 'UserCollectorSpeakingEngagements' and schema_id = schema_id('dbo'))
begin
    create table dbo.UserCollectorSpeakingEngagements
    (
        Id                        int identity
            constraint PK_UserCollectorSpeakingEngagements
                primary key clustered,
        CreatedByEntraOid         nvarchar(36)   not null,
        SpeakingEngagementsFile   nvarchar(2048) not null,
        DisplayName               nvarchar(255)  not null,
        IsActive                  bit            not null
            constraint DF_UserCollectorSpeakingEngagements_IsActive
                default (1),
        CreatedOn                 datetimeoffset not null
            constraint DF_UserCollectorSpeakingEngagements_CreatedOn
                default (sysdatetimeoffset()),
        LastUpdatedOn             datetimeoffset not null
            constraint DF_UserCollectorSpeakingEngagements_LastUpdatedOn
                default (sysdatetimeoffset()),
        constraint UQ_UserCollectorSpeakingEngagements_Owner_File
            unique (CreatedByEntraOid, SpeakingEngagementsFile)
    )
end
go

if not exists (select 1 from sys.indexes where name = 'IX_UserCollectorSpeakingEngagements_Owner' and object_id = object_id('dbo.UserCollectorSpeakingEngagements'))
begin
    create nonclustered index IX_UserCollectorSpeakingEngagements_Owner
        on dbo.UserCollectorSpeakingEngagements (CreatedByEntraOid)
end
go

-- Per-user scheduled items publisher configurations.
-- Each user has at most one scheduled items config (unique on CreatedByEntraOid).
if not exists (select 1 from sys.tables where name = 'UserCollectorScheduledItems' and schema_id = schema_id('dbo'))
begin
    create table dbo.UserCollectorScheduledItems
    (
        Id                int identity
            constraint PK_UserCollectorScheduledItems
                primary key clustered,
        CreatedByEntraOid nvarchar(36)   not null,
        DisplayName       nvarchar(255)  not null,
        IsActive          bit            not null
            constraint DF_UserCollectorScheduledItems_IsActive
                default (1),
        CreatedOn         datetimeoffset not null
            constraint DF_UserCollectorScheduledItems_CreatedOn
                default (sysdatetimeoffset()),
        LastUpdatedOn     datetimeoffset not null
            constraint DF_UserCollectorScheduledItems_LastUpdatedOn
                default (sysdatetimeoffset()),
        constraint UQ_UserCollectorScheduledItems_Owner
            unique (CreatedByEntraOid)
    )
end
go

if not exists (select 1 from sys.indexes where name = 'IX_UserCollectorScheduledItems_Owner' and object_id = object_id('dbo.UserCollectorScheduledItems'))
begin
    create nonclustered index IX_UserCollectorScheduledItems_Owner
        on dbo.UserCollectorScheduledItems (CreatedByEntraOid)
end
go

use JJGNet
go

create table Engagements
(
    Id            int identity
        constraint Engagements_pk
        primary key nonclustered,
    Name          nvarchar(max)              not null,
    Url           nvarchar(max),
    StartDateTime datetimeoffset             not null,
    EndDateTime   datetimeoffset             not null,
    Comments      nvarchar(max),
    TimeZoneId    nvarchar(50) default 'America/Phoenix' not null
)
go

create table dbo.Talks
(
    Id                   int identity
        constraint Talks_pk
            primary key nonclustered,
    EngagementId         int
        constraint Talks_Engagements_Id
            references Engagements,
    Name                 nvarchar(max)  not null,
    UrlForConferenceTalk nvarchar(max),
    UrlForTalk           nvarchar(max),
    StartDateTime        datetimeoffset not null,
    EndDateTime          datetimeoffset not null,
    TalkLocation         nvarchar(max),
    Comments             nvarchar(max)
)
go

create table dbo.ScheduledItems
(
    Id               int identity
        constraint ScheduledItems_pk
            primary key nonclustered,
    ItemTableName    varchar(255)   not null,
    ItemPrimaryKey   varchar(255)   not null,
    Message          nvarchar(max),
    SendOnDateTime   datetimeoffset not null,
    MessageSent      bit default 0  not null,
    MessageSentOn    datetimeoffset,
    ItemSecondaryKey varchar(255)
)
go

create index ScheduledItems_MessageSentOn_index
    on ScheduledItems (MessageSentOn)
go

create table Cache
(
    Id                         nvarchar(449)  not null
        primary key,
    Value                      varbinary(max) not null,
    ExpiresAtTime              datetimeoffset not null,
    SlidingExpirationInSeconds bigint,
    AbsoluteExpiration         datetimeoffset
)
go

create index Index_ExpiresAtTime
    on Cache (ExpiresAtTime)
go



use JJGNet
go

create table dbo.Engagements
(
    Id            int identity
        constraint Engagements_pk
        primary key nonclustered,
    Name          nvarchar(max)              not null,
    Url           nvarchar(max),
    StartDateTime datetimeoffset             not null,
    EndDateTime   datetimeoffset             not null,
    Comments      nvarchar(max),
    TimeZoneId    nvarchar(50) default 'America/Phoenix' not null,
    CreatedOn datetimeoffset default getutcdate() NOT NULL,
    LastUpdatedOn datetimeoffset default getutcdate() NOT NULL
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
    ItemPrimaryKey   int   not null,
    Message          nvarchar(max),
    SendOnDateTime   datetimeoffset not null,
    MessageSent      bit default 0  not null,
    MessageSentOn    datetimeoffset
)
go

create index ScheduledItems_MessageSentOn_index
    on ScheduledItems (MessageSentOn)
go

create table dbo.Cache
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

-- Create the Feed Checks table
create table dbo.FeedChecks
(
    Id                     int identity
        constraint FeedChecks_pk_Id
            primary key,
    Name                   nvarchar(255)                       not null
        constraint FeedChecks_Unique_Name
            unique,
    LastCheckedFeed        datetimeoffset default getutcdate() not null,
    LastItemAddedOrUpdated datetimeoffset default GETUTCDATE() not null,
    LastUpdatedOn          datetimeoffset default getutcdate() not null
)
go

-- Create the TokenRefresh table
create table dbo.TokenRefreshes
(
    Id            int identity
        constraint TokenRefresh_pk_Id
            primary key,
    Name          nvarchar(255)                       not null
        constraint ToeknRefresh_Unique_Name
            unique,
    Expires       datetimeoffset default getutcdate() not null,
    LastChecked   datetimeoffset default GETUTCDATE() not null,
    LastRefreshed datetimeoffset default GETUTCDATE() not null,
    LastUpdatedOn datetimeoffset default getutcdate() not null
)
go

-- Create the SyndicationFeedSource table
create table dbo.SyndicationFeedSources
(
    Id                int identity
        constraint SyndicationFeedSource_pk_Id
            primary key,
    FeedIdentifier    nvarchar(max)                       not null,
    Author            nvarchar(255)                       not null,
    Title             nvarchar(512)                       not null,
    ShortenedUrl      nvarchar(255),
    Tags              nvarchar(max),
    Url               nvarchar(max)                       not null,
    PublicationDate   datetimeoffset default getutcdate() not null,
    AddedOn           datetimeoffset default getutcdate() not null,
    ItemLastUpdatedOn datetimeoffset default getutcdate(),
    LastUpdatedOn     datetimeoffset default getutcdate() not null
)
GO

-- Create the YouTubeSource table
create table dbo.YouTubeSources
(
    Id                int identity
        constraint YouTubeSource_pk_Id
            primary key,
    VideoId           nvarchar(20)                        not null,
    Author            nvarchar(255)                       not null,
    Title             nvarchar(512)                       not null,
    ShortenedUrl      nvarchar(255),
    Tags              nvarchar(max),
    Url               nvarchar(max)                       not null,
    PublicationDate   datetimeoffset default getutcdate() not null,
    AddedOn           datetimeoffset default getutcdate() not null,
    ItemLastUpdatedOn datetimeoffset default getutcdate(),
    LastUpdatedOn     datetimeoffset default getutcdate() not null
)
go

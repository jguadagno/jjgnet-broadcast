use JJGNet
go

create table dbo.Engagements
(
    Id            int identity
        constraint Engagements_pk
        primary key clustered,
    Name          nvarchar(500)              not null,
    Url           nvarchar(2048),
    StartDateTime datetimeoffset             not null,
    EndDateTime   datetimeoffset             not null,
    Comments      nvarchar(max),
    TimeZoneId    nvarchar(50) default 'America/Phoenix' not null,
    CreatedOn datetimeoffset default getutcdate() NOT NULL,
    LastUpdatedOn datetimeoffset default getutcdate() NOT NULL,
    CreatedByEntraOid nvarchar(36) null
)
go

create table dbo.Talks
(
    Id                   int identity
        constraint Talks_pk
            primary key clustered,
    EngagementId         int
        constraint Talks_Engagements_Id
            references Engagements,
    Name                 nvarchar(500)  not null,
    UrlForConferenceTalk nvarchar(max),
    UrlForTalk           nvarchar(max),
    StartDateTime        datetimeoffset not null,
    EndDateTime          datetimeoffset not null,
    TalkLocation         nvarchar(500),
    Comments             nvarchar(max),
    CreatedByEntraOid    nvarchar(36)   null
)
go

create table dbo.ScheduledItems
(
    Id               int identity
        constraint ScheduledItems_pk
            primary key clustered,
    -- Valid values: 'Engagements', 'Talks', 'SyndicationFeedSources', 'YouTubeSources'
    -- CHECK constraint CK_ScheduledItems_ItemTableName is added via migration 2026-03-16-scheduleditem-integrity.sql
    ItemTableName    varchar(255)   not null,
    ItemPrimaryKey   int   not null,
    Message          nvarchar(max),
    SendOnDateTime   datetimeoffset not null,
    MessageSent      bit default 0  not null,
    MessageSentOn    datetimeoffset,
    ImageUrl         nvarchar(2048),
    SocialMediaPlatformId int null,
    MessageType      nvarchar(50)   null,
    CreatedByEntraOid nvarchar(36)  null
)
go

create index ScheduledItems_MessageSentOn_index
    on ScheduledItems (MessageSentOn)
go

create nonclustered index IX_ScheduledItems_Pending
    on ScheduledItems (MessageSent asc, SendOnDateTime asc)
    include (ItemTableName, ItemPrimaryKey, Message)
go

create nonclustered index IX_Talks_EngagementId
    on Talks (EngagementId)
go

create table dbo.TokenCache
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
    on TokenCache (ExpiresAtTime)
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

-- Create the SourceTags junction table (Issue #323)
-- SourceType valid values: 'SyndicationFeed', 'YouTube'
create table dbo.SourceTags
(
    Id         int identity
        constraint PK_SourceTags
            primary key clustered,
    SourceId   int           not null,
    SourceType nvarchar(50)  not null,
    Tag        nvarchar(100) not null
)
go

create index IX_SourceTags_Tag on dbo.SourceTags (Tag)
go

create index IX_SourceTags_SourceId_SourceType on dbo.SourceTags (SourceId, SourceType)
go

-- Create the MessageTemplates table
create table dbo.MessageTemplates
(
    SocialMediaPlatformId int not null,
    MessageType   nvarchar(50)  not null,
    Template      nvarchar(max) not null,
    Description   nvarchar(500) null,
    CreatedByEntraOid nvarchar(36) null,
    constraint PK_MessageTemplates primary key (SocialMediaPlatformId, MessageType)
)
go

-- Create the RBAC tables (Issue #602)
create table dbo.Roles
(
    Id          int identity
        constraint PK_Roles
            primary key clustered,
    Name        nvarchar(50)  not null
        constraint UQ_Roles_Name
            unique,
    Description nvarchar(200) null
)
go

-- NOTE: EntraObjectId stores the Entra ID oid claim — a stable per-user GUID that never changes.
--       ApprovalStatus valid values: 'Pending', 'Approved', 'Rejected'
create table dbo.ApplicationUsers
(
    Id             int identity
        constraint PK_ApplicationUsers
            primary key clustered,
    EntraObjectId  nvarchar(36)  not null
        constraint UQ_ApplicationUsers_EntraObjectId
            unique,
    DisplayName    nvarchar(200) null,
    Email          nvarchar(200) null,
    ApprovalStatus nvarchar(20)  not null
        constraint DF_ApplicationUsers_ApprovalStatus
            default ('Pending')
        constraint CK_ApplicationUsers_ApprovalStatus
            check (ApprovalStatus in ('Pending', 'Approved', 'Rejected')),
    ApprovalNotes  nvarchar(500) null,
    CreatedAt      datetimeoffset not null
        constraint DF_ApplicationUsers_CreatedAt
            default (getutcdate()),
    UpdatedAt      datetimeoffset not null
        constraint DF_ApplicationUsers_UpdatedAt
            default (getutcdate())
)
go

create table dbo.UserRoles
(
    UserId int not null
        constraint FK_UserRoles_Users
            references ApplicationUsers,
    RoleId int not null
        constraint FK_UserRoles_Roles
            references Roles,
    constraint PK_UserRoles
        primary key clustered (UserId asc, RoleId asc)
)
go

-- Action valid values: 'Registered', 'Approved', 'Rejected', 'RoleAssigned', 'RoleRemoved'
-- AdminUserId is NULL for system-generated entries
create table dbo.UserApprovalLog
(
    Id          int identity
        constraint PK_UserApprovalLog
            primary key clustered,
    UserId      int           not null
        constraint FK_UserApprovalLog_User
            references ApplicationUsers,
    AdminUserId int           null
        constraint FK_UserApprovalLog_Admin
            references ApplicationUsers,
    Action      nvarchar(20)  not null
        constraint CK_UserApprovalLog_Action
            check (Action in ('Registered', 'Approved', 'Rejected', 'RoleAssigned', 'RoleRemoved')),
    Notes       nvarchar(500) null,
    CreatedAt   datetimeoffset not null
        constraint DF_UserApprovalLog_CreatedAt
            default (getutcdate())
)
go

-- Create the EmailTemplates table (Issue #615)
create table dbo.EmailTemplates
(
    Id          int identity
        constraint PK_EmailTemplates
            primary key clustered,
    Name        nvarchar(100)  not null
        constraint UQ_EmailTemplates_Name
            unique,
    Subject     nvarchar(500)  not null,
    Body        nvarchar(max)  not null,
    CreatedDate datetimeoffset not null
        constraint DF_EmailTemplates_CreatedDate
            default (SYSDATETIMEOFFSET()),
    UpdatedDate datetimeoffset not null
        constraint DF_EmailTemplates_UpdatedDate
            default (SYSDATETIMEOFFSET())
)
go

-- Create the SocialMediaPlatforms table (Epic #667)
create table dbo.SocialMediaPlatforms
(
    Id       int identity
        constraint PK_SocialMediaPlatforms
            primary key clustered,
    Name     nvarchar(100) not null
        constraint UQ_SocialMediaPlatforms_Name
            unique,
    Url      nvarchar(500) null,
    Icon     nvarchar(100) null,
    IsActive bit default 1 not null
)
go

-- Create the EngagementSocialMediaPlatforms junction table (Epic #667)
create table dbo.EngagementSocialMediaPlatforms
(
    EngagementId           int           not null
        constraint FK_EngagementSocialMediaPlatforms_Engagements
            references dbo.Engagements,
    SocialMediaPlatformId  int           not null
        constraint FK_EngagementSocialMediaPlatforms_SocialMediaPlatforms
            references dbo.SocialMediaPlatforms,
    Handle                 nvarchar(200) null,
    constraint PK_EngagementSocialMediaPlatforms
        primary key clustered (EngagementId, SocialMediaPlatformId)
)
go

create nonclustered index IX_EngagementSocialMediaPlatforms_SocialMediaPlatformId
    on dbo.EngagementSocialMediaPlatforms (SocialMediaPlatformId)
go

-- Add FK constraint from ScheduledItems to SocialMediaPlatforms (Epic #667)
ALTER TABLE dbo.ScheduledItems
    ADD CONSTRAINT FK_ScheduledItems_SocialMediaPlatforms
        FOREIGN KEY (SocialMediaPlatformId)
        REFERENCES dbo.SocialMediaPlatforms(Id)
go

-- Add FK constraint from MessageTemplates to SocialMediaPlatforms (Epic #667)
ALTER TABLE dbo.MessageTemplates
    ADD CONSTRAINT FK_MessageTemplates_SocialMediaPlatforms
        FOREIGN KEY (SocialMediaPlatformId)
        REFERENCES dbo.SocialMediaPlatforms(Id)
go


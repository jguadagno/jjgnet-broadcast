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
    -- Valid values: 'Engagements', 'Talks', 'SyndicationFeedItems', 'YouTubeItems'
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
    Name                   nvarchar(255)                       not null,
    EntraOId               nvarchar(36)   not null
        constraint DF_FeedChecks_EntraOId
            default (''),
    LastCheckedFeed        datetimeoffset default getutcdate() not null,
    LastItemAddedOrUpdated datetimeoffset default GETUTCDATE() not null,
    LastUpdatedOn          datetimeoffset default getutcdate() not null,
    constraint UQ_FeedChecks_Name_EntraOId
        unique (Name, EntraOId)
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

-- Create the SyndicationFeedItem table
create table dbo.SyndicationFeedItems
(
    Id                int identity
        constraint SyndicationFeedItem_pk_Id
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
    LastUpdatedOn     datetimeoffset default getutcdate() not null,
    CreatedByEntraOid nvarchar(36)                        not null
)
GO

-- Create the YouTubeItem table
create table dbo.YouTubeItems
(
    Id                int identity
        constraint YouTubeItem_pk_Id
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
    LastUpdatedOn     datetimeoffset default getutcdate() not null,
    CreatedByEntraOid nvarchar(36)                        not null
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
    CreatedByEntraOid nvarchar(36) not null default '',
    constraint PK_MessageTemplates primary key (SocialMediaPlatformId, MessageType, CreatedByEntraOid)
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
create table dbo.UserApprovalLogs
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
    IsActive bit default 1 not null,
    CredentialSetupDocumentationUrl nvarchar(500) null
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

-- Create the UserPlatformSettings table (Issue #731)
create table dbo.UserPlatformSettings
(
    Id                    int identity
        constraint PK_UserPlatformSettings
            primary key clustered,
    CreatedByEntraOid     nvarchar(36)   not null,
    SocialMediaPlatformId int            not null
        constraint FK_UserPlatformSettings_SocialMediaPlatforms
            references dbo.SocialMediaPlatforms,
    IsEnabled             bit default 0  not null,
    Settings              nvarchar(max)  null,
    CreatedOn             datetimeoffset not null
        constraint DF_UserPlatformSettings_CreatedOn
            default (getutcdate()),
    LastUpdatedOn         datetimeoffset not null
        constraint DF_UserPlatformSettings_LastUpdatedOn
            default (getutcdate()),
    constraint UQ_UserPlatformSettings_User_Platform
        unique (CreatedByEntraOid, SocialMediaPlatformId)
)
go

-- ============================================================
-- UserPlatformBlueskySettings (Issue #958 — Phase 1)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPlatformBlueskySettings')
BEGIN
    CREATE TABLE [dbo].[UserPlatformBlueskySettings]
    (
        [Id]                 INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid]  NVARCHAR(36)        NOT NULL,
        [IsEnabled]          BIT                 NOT NULL CONSTRAINT DF_UserPlatformBlueskySettings_IsEnabled DEFAULT (0),
        [UserName]           NVARCHAR(255)       NULL,
        [HasAppPassword]     BIT                 NOT NULL CONSTRAINT DF_UserPlatformBlueskySettings_HasAppPassword DEFAULT (0),
        [CreatedOn]          DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPlatformBlueskySettings_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]      DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPlatformBlueskySettings_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserPlatformBlueskySettings PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT UQ_UserPlatformBlueskySettings_Owner UNIQUE ([CreatedByEntraOid])
    );

    PRINT 'Created table UserPlatformBlueskySettings';
END
ELSE
BEGIN
    PRINT 'Table UserPlatformBlueskySettings already exists — skipped';
END
GO

-- ============================================================
-- UserPlatformTwitterSettings (Issue #958 — Phase 1)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPlatformTwitterSettings')
BEGIN
    CREATE TABLE [dbo].[UserPlatformTwitterSettings]
    (
        [Id]                   INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid]    NVARCHAR(36)        NOT NULL,
        [IsEnabled]            BIT                 NOT NULL CONSTRAINT DF_UserPlatformTwitterSettings_IsEnabled DEFAULT (0),
        [HasConsumerKey]       BIT                 NOT NULL CONSTRAINT DF_UserPlatformTwitterSettings_HasConsumerKey DEFAULT (0),
        [HasConsumerSecret]    BIT                 NOT NULL CONSTRAINT DF_UserPlatformTwitterSettings_HasConsumerSecret DEFAULT (0),
        [HasAccessToken]       BIT                 NOT NULL CONSTRAINT DF_UserPlatformTwitterSettings_HasAccessToken DEFAULT (0),
        [HasAccessTokenSecret] BIT                 NOT NULL CONSTRAINT DF_UserPlatformTwitterSettings_HasAccessTokenSecret DEFAULT (0),
        [CreatedOn]            DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPlatformTwitterSettings_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]        DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPlatformTwitterSettings_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserPlatformTwitterSettings PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT UQ_UserPlatformTwitterSettings_Owner UNIQUE ([CreatedByEntraOid])
    );

    PRINT 'Created table UserPlatformTwitterSettings';
END
ELSE
BEGIN
    PRINT 'Table UserPlatformTwitterSettings already exists — skipped';
END
GO

-- ============================================================
-- UserPlatformLinkedInSettings (Issue #958 — Phase 1)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPlatformLinkedInSettings')
BEGIN
    CREATE TABLE [dbo].[UserPlatformLinkedInSettings]
    (
        [Id]               INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid] NVARCHAR(36)       NOT NULL,
        [IsEnabled]        BIT                 NOT NULL CONSTRAINT DF_UserPlatformLinkedInSettings_IsEnabled DEFAULT (0),
        [AuthorId]         NVARCHAR(255)       NULL,
        [ClientId]         NVARCHAR(255)       NULL,
        [HasClientSecret]  BIT                 NOT NULL CONSTRAINT DF_UserPlatformLinkedInSettings_HasClientSecret DEFAULT (0),
        [CreatedOn]        DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPlatformLinkedInSettings_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]    DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPlatformLinkedInSettings_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserPlatformLinkedInSettings PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT UQ_UserPlatformLinkedInSettings_Owner UNIQUE ([CreatedByEntraOid])
    );

    PRINT 'Created table UserPlatformLinkedInSettings';
END
ELSE
BEGIN
    PRINT 'Table UserPlatformLinkedInSettings already exists — skipped';
END
GO

-- ============================================================
-- UserPlatformFacebookSettings (Issue #958 — Phase 1)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPlatformFacebookSettings')
BEGIN
    CREATE TABLE [dbo].[UserPlatformFacebookSettings]
    (
        [Id]                       INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid]        NVARCHAR(36)        NOT NULL,
        [IsEnabled]                BIT                 NOT NULL CONSTRAINT DF_UserPlatformFacebookSettings_IsEnabled DEFAULT (0),
        [PageId]                   NVARCHAR(255)       NULL,
        [AppId]                    NVARCHAR(255)       NULL,
        [HasPageAccessToken]       BIT                 NOT NULL CONSTRAINT DF_UserPlatformFacebookSettings_HasPageAccessToken DEFAULT (0),
        [HasAppSecret]             BIT                 NOT NULL CONSTRAINT DF_UserPlatformFacebookSettings_HasAppSecret DEFAULT (0),
        [HasClientToken]           BIT                 NOT NULL CONSTRAINT DF_UserPlatformFacebookSettings_HasClientToken DEFAULT (0),
        [HasShortLivedAccessToken] BIT                 NOT NULL CONSTRAINT DF_UserPlatformFacebookSettings_HasShortLivedAccessToken DEFAULT (0),
        [HasLongLivedAccessToken]  BIT                 NOT NULL CONSTRAINT DF_UserPlatformFacebookSettings_HasLongLivedAccessToken DEFAULT (0),
        [CreatedOn]                DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPlatformFacebookSettings_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]            DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPlatformFacebookSettings_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserPlatformFacebookSettings PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT UQ_UserPlatformFacebookSettings_Owner UNIQUE ([CreatedByEntraOid])
    );

    PRINT 'Created table UserPlatformFacebookSettings';
END
ELSE
BEGIN
    PRINT 'Table UserPlatformFacebookSettings already exists — skipped';
END
GO

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



-- Per-user OAuth tokens for LinkedIn (and future platforms). Added by issue #777.
-- Access tokens are stored as nvarchar(max) — SQL Server TDE provides OS-level at-rest encryption.
-- Follow-up: evaluate SQL Server Always Encrypted for AccessToken / RefreshToken columns if compliance requirements arise.
create table dbo.UserOAuthTokens
(
    Id                    int identity
        constraint PK_UserOAuthTokens
            primary key clustered,
    CreatedByEntraOid     nvarchar(36)   not null,
    SocialMediaPlatformId int            not null
        constraint FK_UserOAuthTokens_SocialMediaPlatforms
            references dbo.SocialMediaPlatforms,
    AccessToken           nvarchar(max)  not null,
    RefreshToken          nvarchar(max)  null,
    AccessTokenExpiresAt  datetimeoffset not null,
    RefreshTokenExpiresAt datetimeoffset null,
    CreatedOn             datetimeoffset not null
        constraint DF_UserOAuthTokens_CreatedOn
            default (getutcdate()),
    LastUpdatedOn         datetimeoffset not null
        constraint DF_UserOAuthTokens_LastUpdatedOn
            default (getutcdate()),
    LastNotifiedAt        datetimeoffset null,
    constraint UQ_UserOAuthTokens_User_Platform
        unique (CreatedByEntraOid, SocialMediaPlatformId)
)
go

create nonclustered index IX_UserOAuthTokens_AccessTokenExpiresAt
    on dbo.UserOAuthTokens (AccessTokenExpiresAt)
go

-- Performance indexes (Issue #855)

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Engagements_StartDateTime' AND object_id = OBJECT_ID('dbo.Engagements'))
    CREATE INDEX IX_Engagements_StartDateTime ON dbo.Engagements (StartDateTime DESC) INCLUDE (Name, EndDateTime, CreatedByEntraOid);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Engagements_CreatedByEntraOid' AND object_id = OBJECT_ID('dbo.Engagements'))
    CREATE INDEX IX_Engagements_CreatedByEntraOid ON dbo.Engagements (CreatedByEntraOid);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedItems_Title' AND object_id = OBJECT_ID('dbo.SyndicationFeedItems'))
    CREATE INDEX IX_SyndicationFeedItems_Title ON dbo.SyndicationFeedItems (Title);
GO

-- Per-user speaking engagements file URL collector configurations.
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

-- Per-user RSS/Atom/JSON feed collector configurations.
-- Each row represents a single feed URL a user wants polled.
if not exists (select 1 from sys.tables where name = 'UserCollectorFeedSources' and schema_id = schema_id('dbo'))
begin
    create table dbo.UserCollectorFeedSources
    (
        Id                int identity
            constraint PK_UserCollectorFeedSources
                primary key clustered,
        CreatedByEntraOid nvarchar(36)   not null,
        FeedUrl           nvarchar(2048) not null,
        DisplayName       nvarchar(255)  not null,
        IsActive          bit            not null
            constraint DF_UserCollectorFeedSources_IsActive
                default (1),
        CreatedOn         datetimeoffset not null
            constraint DF_UserCollectorFeedSources_CreatedOn
                default (sysdatetimeoffset()),
        LastUpdatedOn     datetimeoffset not null
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
        PlaylistId        nvarchar(255)  not null default '',
        ResultSetPageSize int            not null default 50,
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedItems_Author' AND object_id = OBJECT_ID('dbo.SyndicationFeedItems'))
    CREATE INDEX IX_SyndicationFeedItems_Author ON dbo.SyndicationFeedItems (Author);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedItems_PublicationDate' AND object_id = OBJECT_ID('dbo.SyndicationFeedItems'))
    CREATE INDEX IX_SyndicationFeedItems_PublicationDate ON dbo.SyndicationFeedItems (PublicationDate DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedItems_AddedOn' AND object_id = OBJECT_ID('dbo.SyndicationFeedItems'))
    CREATE INDEX IX_SyndicationFeedItems_AddedOn ON dbo.SyndicationFeedItems (AddedOn DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SyndicationFeedItems_CreatedByEntraOid' AND object_id = OBJECT_ID('dbo.SyndicationFeedItems'))
    CREATE INDEX IX_SyndicationFeedItems_CreatedByEntraOid ON dbo.SyndicationFeedItems (CreatedByEntraOid);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeItems_Title' AND object_id = OBJECT_ID('dbo.YouTubeItems'))
    CREATE INDEX IX_YouTubeItems_Title ON dbo.YouTubeItems (Title);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeItems_Author' AND object_id = OBJECT_ID('dbo.YouTubeItems'))
    CREATE INDEX IX_YouTubeItems_Author ON dbo.YouTubeItems (Author);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeItems_PublicationDate' AND object_id = OBJECT_ID('dbo.YouTubeItems'))
    CREATE INDEX IX_YouTubeItems_PublicationDate ON dbo.YouTubeItems (PublicationDate DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeItems_AddedOn' AND object_id = OBJECT_ID('dbo.YouTubeItems'))
    CREATE INDEX IX_YouTubeItems_AddedOn ON dbo.YouTubeItems (AddedOn DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_YouTubeItems_CreatedByEntraOid' AND object_id = OBJECT_ID('dbo.YouTubeItems'))
    CREATE INDEX IX_YouTubeItems_CreatedByEntraOid ON dbo.YouTubeItems (CreatedByEntraOid);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ScheduledItems_SendOnDateTime' AND object_id = OBJECT_ID('dbo.ScheduledItems'))
    CREATE INDEX IX_ScheduledItems_SendOnDateTime ON dbo.ScheduledItems (SendOnDateTime DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ScheduledItems_CreatedByEntraOid' AND object_id = OBJECT_ID('dbo.ScheduledItems'))
    CREATE INDEX IX_ScheduledItems_CreatedByEntraOid ON dbo.ScheduledItems (CreatedByEntraOid);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SocialMediaPlatforms_IsActive_Name' AND object_id = OBJECT_ID('dbo.SocialMediaPlatforms'))
    CREATE INDEX IX_SocialMediaPlatforms_IsActive_Name ON dbo.SocialMediaPlatforms (IsActive, Name);
GO

-- ============================================================
-- UserRandomPostSettings (Issue #995)
-- Per-user random post scheduling + content filtering
-- Each row = one schedule: (user, publisher, cron, content-filter settings)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserRandomPostSettings')
BEGIN
    CREATE TABLE [dbo].[UserRandomPostSettings]
    (
        [Id]                    INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid]     NVARCHAR(36)        NOT NULL,
        [SocialMediaPlatformId] INT                 NOT NULL,
        [CronExpression]        NVARCHAR(100)       NOT NULL,
        [CutoffDate]            DATETIMEOFFSET      NULL,
        [ExcludedCategories]    NVARCHAR(MAX)       NULL,
        [IsActive]              BIT                 NOT NULL CONSTRAINT DF_UserRandomPostSettings_IsActive DEFAULT (1),
        [NextRunDateUtc]        DATETIMEOFFSET      NULL,
        [CronParseFailureCount] INT                 NOT NULL CONSTRAINT DF_UserRandomPostSettings_CronParseFailureCount DEFAULT (0),
        [CreatedOn]             DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserRandomPostSettings_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]         DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserRandomPostSettings_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserRandomPostSettings PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT FK_UserRandomPostSettings_SocialMediaPlatforms
            FOREIGN KEY ([SocialMediaPlatformId]) REFERENCES [dbo].[SocialMediaPlatforms]([Id]),
        CONSTRAINT UQ_UserRandomPostSettings_Owner_Platform_Cron
            UNIQUE ([CreatedByEntraOid], [SocialMediaPlatformId], [CronExpression])
    );
    CREATE NONCLUSTERED INDEX IX_UserRandomPostSettings_Active
        ON [dbo].[UserRandomPostSettings] ([IsActive] ASC, [CreatedByEntraOid] ASC);
    CREATE NONCLUSTERED INDEX IX_UserRandomPostSettings_IsActive_NextRunDateUtc
        ON [dbo].[UserRandomPostSettings] ([IsActive], [NextRunDateUtc])
        WHERE [IsActive] = 1;
    PRINT 'Created table UserRandomPostSettings';
END
ELSE
BEGIN
    PRINT 'Table UserRandomPostSettings already exists — skipped';
END
GO

-- ============================================================
-- UserEventDispatcherMappings (Issue #995)
-- Maps collector event types to dispatcher platforms per user
-- EventType valid values align with MessageTemplates.MessageTypes
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserEventDispatcherMappings')
BEGIN
    CREATE TABLE [dbo].[UserEventDispatcherMappings]
    (
        [Id]                    INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid]     NVARCHAR(36)        NOT NULL,
        [EventType]             NVARCHAR(50)        NOT NULL,
        [SocialMediaPlatformId] INT                 NOT NULL,
        [IsActive]              BIT                 NOT NULL CONSTRAINT DF_UserEventDispatcherMapping_IsActive DEFAULT (1),
        [CreatedOn]             DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserEventDispatcherMapping_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]         DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserEventDispatcherMapping_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserEventDispatcherMapping PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT FK_UserEventDispatcherMapping_SocialMediaPlatforms
            FOREIGN KEY ([SocialMediaPlatformId]) REFERENCES [dbo].[SocialMediaPlatforms]([Id]),
        CONSTRAINT UQ_UserEventDispatcherMapping_Owner_Event_Platform
            UNIQUE ([CreatedByEntraOid], [EventType], [SocialMediaPlatformId]),
        CONSTRAINT CK_UserEventDispatcherMapping_EventType
            CHECK ([EventType] IN ('NewSyndicationFeedItem', 'NewYouTubeItem', 'NewSpeakingEngagement', 'RandomPost', 'ScheduledItem'))
    );
    CREATE NONCLUSTERED INDEX IX_UserEventDispatcherMapping_Active
        ON [dbo].[UserEventDispatcherMappings] ([IsActive] ASC, [CreatedByEntraOid] ASC);
    PRINT 'Created table UserEventDispatcherMappings';
END
ELSE
BEGIN
    PRINT 'Table UserEventDispatcherMappings already exists — skipped';
END
GO

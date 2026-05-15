-- Migration: 2026-05-15 — Per-publisher typed settings tables
-- Phase 1: Create four publisher-specific settings tables.
-- UserPublisherSettings (generic shim) is intentionally left intact for Phase 2 cutover.

-- ============================================================
-- UserPublisherBlueskySettings
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPublisherBlueskySettings')
BEGIN
    CREATE TABLE [dbo].[UserPublisherBlueskySettings]
    (
        [Id]                 INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid]  NVARCHAR(36)        NOT NULL,
        [IsEnabled]          BIT                 NOT NULL CONSTRAINT DF_UserPublisherBlueskySettings_IsEnabled DEFAULT (0),
        [UserName]           NVARCHAR(255)       NULL,
        [HasAppPassword]     BIT                 NOT NULL CONSTRAINT DF_UserPublisherBlueskySettings_HasAppPassword DEFAULT (0),
        [CreatedOn]          DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPublisherBlueskySettings_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]      DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPublisherBlueskySettings_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserPublisherBlueskySettings PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT UQ_UserPublisherBlueskySettings_Owner UNIQUE ([CreatedByEntraOid])
    );

    PRINT 'Created table UserPublisherBlueskySettings';
END
ELSE
BEGIN
    PRINT 'Table UserPublisherBlueskySettings already exists — skipped';
END
GO

-- ============================================================
-- UserPublisherTwitterSettings
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPublisherTwitterSettings')
BEGIN
    CREATE TABLE [dbo].[UserPublisherTwitterSettings]
    (
        [Id]                   INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid]    NVARCHAR(36)        NOT NULL,
        [IsEnabled]            BIT                 NOT NULL CONSTRAINT DF_UserPublisherTwitterSettings_IsEnabled DEFAULT (0),
        [HasConsumerKey]       BIT                 NOT NULL CONSTRAINT DF_UserPublisherTwitterSettings_HasConsumerKey DEFAULT (0),
        [HasConsumerSecret]    BIT                 NOT NULL CONSTRAINT DF_UserPublisherTwitterSettings_HasConsumerSecret DEFAULT (0),
        [HasAccessToken]       BIT                 NOT NULL CONSTRAINT DF_UserPublisherTwitterSettings_HasAccessToken DEFAULT (0),
        [HasAccessTokenSecret] BIT                 NOT NULL CONSTRAINT DF_UserPublisherTwitterSettings_HasAccessTokenSecret DEFAULT (0),
        [CreatedOn]            DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPublisherTwitterSettings_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]        DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPublisherTwitterSettings_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserPublisherTwitterSettings PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT UQ_UserPublisherTwitterSettings_Owner UNIQUE ([CreatedByEntraOid])
    );

    PRINT 'Created table UserPublisherTwitterSettings';
END
ELSE
BEGIN
    PRINT 'Table UserPublisherTwitterSettings already exists — skipped';
END
GO

-- ============================================================
-- UserPublisherLinkedInSettings
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPublisherLinkedInSettings')
BEGIN
    CREATE TABLE [dbo].[UserPublisherLinkedInSettings]
    (
        [Id]               INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid] NVARCHAR(36)       NOT NULL,
        [IsEnabled]        BIT                 NOT NULL CONSTRAINT DF_UserPublisherLinkedInSettings_IsEnabled DEFAULT (0),
        [AuthorId]         NVARCHAR(255)       NULL,
        [ClientId]         NVARCHAR(255)       NULL,
        [HasClientSecret]  BIT                 NOT NULL CONSTRAINT DF_UserPublisherLinkedInSettings_HasClientSecret DEFAULT (0),
        [HasAccessToken]   BIT                 NOT NULL CONSTRAINT DF_UserPublisherLinkedInSettings_HasAccessToken DEFAULT (0),
        [CreatedOn]        DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPublisherLinkedInSettings_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]    DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPublisherLinkedInSettings_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserPublisherLinkedInSettings PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT UQ_UserPublisherLinkedInSettings_Owner UNIQUE ([CreatedByEntraOid])
    );

    PRINT 'Created table UserPublisherLinkedInSettings';
END
ELSE
BEGIN
    PRINT 'Table UserPublisherLinkedInSettings already exists — skipped';
END
GO

-- ============================================================
-- UserPublisherFacebookSettings
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPublisherFacebookSettings')
BEGIN
    CREATE TABLE [dbo].[UserPublisherFacebookSettings]
    (
        [Id]                       INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid]        NVARCHAR(36)        NOT NULL,
        [IsEnabled]                BIT                 NOT NULL CONSTRAINT DF_UserPublisherFacebookSettings_IsEnabled DEFAULT (0),
        [PageId]                   NVARCHAR(255)       NULL,
        [AppId]                    NVARCHAR(255)       NULL,
        [HasPageAccessToken]       BIT                 NOT NULL CONSTRAINT DF_UserPublisherFacebookSettings_HasPageAccessToken DEFAULT (0),
        [HasAppSecret]             BIT                 NOT NULL CONSTRAINT DF_UserPublisherFacebookSettings_HasAppSecret DEFAULT (0),
        [HasClientToken]           BIT                 NOT NULL CONSTRAINT DF_UserPublisherFacebookSettings_HasClientToken DEFAULT (0),
        [HasShortLivedAccessToken] BIT                 NOT NULL CONSTRAINT DF_UserPublisherFacebookSettings_HasShortLivedAccessToken DEFAULT (0),
        [HasLongLivedAccessToken]  BIT                 NOT NULL CONSTRAINT DF_UserPublisherFacebookSettings_HasLongLivedAccessToken DEFAULT (0),
        [CreatedOn]                DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPublisherFacebookSettings_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]            DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserPublisherFacebookSettings_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserPublisherFacebookSettings PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT UQ_UserPublisherFacebookSettings_Owner UNIQUE ([CreatedByEntraOid])
    );

    PRINT 'Created table UserPublisherFacebookSettings';
END
ELSE
BEGIN
    PRINT 'Table UserPublisherFacebookSettings already exists — skipped';
END
GO

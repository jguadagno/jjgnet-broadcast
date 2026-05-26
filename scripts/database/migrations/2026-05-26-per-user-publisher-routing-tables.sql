-- Migration: 2026-05-26 — Per-user publisher routing tables (Issue #995 Phase 1)
-- Creates UserRandomPostSettings and UserEventPublisherMapping tables.

-- ============================================================
-- UserRandomPostSettings
-- Per-user random post scheduling + content filtering.
-- Each row = one schedule: (user, publisher, cron, content-filter settings).
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
    PRINT 'Created table UserRandomPostSettings';
END
ELSE
BEGIN
    PRINT 'Table UserRandomPostSettings already exists — skipped';
END
GO

-- ============================================================
-- UserEventPublisherMapping
-- Maps collector event types to publisher platforms per user.
-- EventType valid values align with MessageTemplates.MessageTypes.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserEventPublisherMapping')
BEGIN
    CREATE TABLE [dbo].[UserEventPublisherMapping]
    (
        [Id]                    INT IDENTITY(1,1)   NOT NULL,
        [CreatedByEntraOid]     NVARCHAR(36)        NOT NULL,
        [EventType]             NVARCHAR(50)        NOT NULL,
        [SocialMediaPlatformId] INT                 NOT NULL,
        [IsActive]              BIT                 NOT NULL CONSTRAINT DF_UserEventPublisherMapping_IsActive DEFAULT (1),
        [CreatedOn]             DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserEventPublisherMapping_CreatedOn DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]         DATETIMEOFFSET      NOT NULL CONSTRAINT DF_UserEventPublisherMapping_LastUpdatedOn DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_UserEventPublisherMapping PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT FK_UserEventPublisherMapping_SocialMediaPlatforms
            FOREIGN KEY ([SocialMediaPlatformId]) REFERENCES [dbo].[SocialMediaPlatforms]([Id]),
        CONSTRAINT UQ_UserEventPublisherMapping_Owner_Event_Platform
            UNIQUE ([CreatedByEntraOid], [EventType], [SocialMediaPlatformId]),
        CONSTRAINT CK_UserEventPublisherMapping_EventType
            CHECK ([EventType] IN ('NewSyndicationFeedItem', 'NewYouTubeItem', 'NewSpeakingEngagement', 'RandomPost', 'ScheduledItem'))
    );
    CREATE NONCLUSTERED INDEX IX_UserEventPublisherMapping_Active
        ON [dbo].[UserEventPublisherMapping] ([IsActive] ASC, [CreatedByEntraOid] ASC);
    PRINT 'Created table UserEventPublisherMapping';
END
ELSE
BEGIN
    PRINT 'Table UserEventPublisherMapping already exists — skipped';
END
GO

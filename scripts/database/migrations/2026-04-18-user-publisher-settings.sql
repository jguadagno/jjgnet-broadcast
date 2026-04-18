-- Migration: Add per-user publisher settings table
-- Issue: #731
-- Date: 2026-04-18
-- Description: Adds dbo.UserPublisherSettings for owner-scoped publisher configuration
--              keyed by CreatedByEntraOid + SocialMediaPlatformId with a JSON settings payload.

USE JJGNet;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE object_id = OBJECT_ID(N'[dbo].[UserPublisherSettings]')
)
BEGIN
    CREATE TABLE [dbo].[UserPublisherSettings]
    (
        [Id]                    INT IDENTITY
            CONSTRAINT [PK_UserPublisherSettings]
                PRIMARY KEY CLUSTERED,
        [CreatedByEntraOid]     NVARCHAR(36)   NOT NULL,
        [SocialMediaPlatformId] INT            NOT NULL
            CONSTRAINT [FK_UserPublisherSettings_SocialMediaPlatforms]
                REFERENCES [dbo].[SocialMediaPlatforms]([Id]),
        [IsEnabled]             BIT            NOT NULL
            CONSTRAINT [DF_UserPublisherSettings_IsEnabled]
                DEFAULT ((0)),
        [Settings]              NVARCHAR(MAX)  NULL,
        [CreatedOn]             DATETIMEOFFSET NOT NULL
            CONSTRAINT [DF_UserPublisherSettings_CreatedOn]
                DEFAULT (GETUTCDATE()),
        [LastUpdatedOn]         DATETIMEOFFSET NOT NULL
            CONSTRAINT [DF_UserPublisherSettings_LastUpdatedOn]
                DEFAULT (GETUTCDATE()),
        CONSTRAINT [UQ_UserPublisherSettings_User_Platform]
            UNIQUE ([CreatedByEntraOid], [SocialMediaPlatformId])
    );
END
GO

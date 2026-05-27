-- ============================================================
-- Phase 3: Rename UserPublisher*Settings → UserPlatform*Settings
-- Issue #995 — per-user publisher routing rename
-- Idempotent: each step is guarded by an IF EXISTS check.
-- ============================================================

-- ============================================================
-- 1. UserPublisherSettings → UserPlatformSettings
-- ============================================================
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPublisherSettings')
    EXEC sp_rename 'dbo.UserPublisherSettings', 'UserPlatformSettings';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'PK_UserPublisherSettings')
    EXEC sp_rename 'dbo.UserPlatformSettings.PK_UserPublisherSettings', 'PK_UserPlatformSettings', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'FK_UserPublisherSettings_SocialMediaPlatforms')
    EXEC sp_rename 'dbo.UserPlatformSettings.FK_UserPublisherSettings_SocialMediaPlatforms', 'FK_UserPlatformSettings_SocialMediaPlatforms', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherSettings_CreatedOn')
    EXEC sp_rename 'dbo.UserPlatformSettings.DF_UserPublisherSettings_CreatedOn', 'DF_UserPlatformSettings_CreatedOn', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherSettings_LastUpdatedOn')
    EXEC sp_rename 'dbo.UserPlatformSettings.DF_UserPublisherSettings_LastUpdatedOn', 'DF_UserPlatformSettings_LastUpdatedOn', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_UserPublisherSettings_User_Platform')
    EXEC sp_rename 'dbo.UserPlatformSettings.UQ_UserPublisherSettings_User_Platform', 'UQ_UserPlatformSettings_User_Platform', 'OBJECT';
GO

-- ============================================================
-- 2. UserPublisherBlueskySettings → UserPlatformBlueskySettings
-- ============================================================
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPublisherBlueskySettings')
    EXEC sp_rename 'dbo.UserPublisherBlueskySettings', 'UserPlatformBlueskySettings';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'PK_UserPublisherBlueskySettings')
    EXEC sp_rename 'dbo.UserPlatformBlueskySettings.PK_UserPublisherBlueskySettings', 'PK_UserPlatformBlueskySettings', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_UserPublisherBlueskySettings_Owner')
    EXEC sp_rename 'dbo.UserPlatformBlueskySettings.UQ_UserPublisherBlueskySettings_Owner', 'UQ_UserPlatformBlueskySettings_Owner', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherBlueskySettings_IsEnabled')
    EXEC sp_rename 'dbo.UserPlatformBlueskySettings.DF_UserPublisherBlueskySettings_IsEnabled', 'DF_UserPlatformBlueskySettings_IsEnabled', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherBlueskySettings_HasAppPassword')
    EXEC sp_rename 'dbo.UserPlatformBlueskySettings.DF_UserPublisherBlueskySettings_HasAppPassword', 'DF_UserPlatformBlueskySettings_HasAppPassword', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherBlueskySettings_CreatedOn')
    EXEC sp_rename 'dbo.UserPlatformBlueskySettings.DF_UserPublisherBlueskySettings_CreatedOn', 'DF_UserPlatformBlueskySettings_CreatedOn', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherBlueskySettings_LastUpdatedOn')
    EXEC sp_rename 'dbo.UserPlatformBlueskySettings.DF_UserPublisherBlueskySettings_LastUpdatedOn', 'DF_UserPlatformBlueskySettings_LastUpdatedOn', 'OBJECT';
GO

-- ============================================================
-- 3. UserPublisherTwitterSettings → UserPlatformTwitterSettings
-- ============================================================
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPublisherTwitterSettings')
    EXEC sp_rename 'dbo.UserPublisherTwitterSettings', 'UserPlatformTwitterSettings';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'PK_UserPublisherTwitterSettings')
    EXEC sp_rename 'dbo.UserPlatformTwitterSettings.PK_UserPublisherTwitterSettings', 'PK_UserPlatformTwitterSettings', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_UserPublisherTwitterSettings_Owner')
    EXEC sp_rename 'dbo.UserPlatformTwitterSettings.UQ_UserPublisherTwitterSettings_Owner', 'UQ_UserPlatformTwitterSettings_Owner', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherTwitterSettings_IsEnabled')
    EXEC sp_rename 'dbo.UserPlatformTwitterSettings.DF_UserPublisherTwitterSettings_IsEnabled', 'DF_UserPlatformTwitterSettings_IsEnabled', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherTwitterSettings_HasConsumerKey')
    EXEC sp_rename 'dbo.UserPlatformTwitterSettings.DF_UserPublisherTwitterSettings_HasConsumerKey', 'DF_UserPlatformTwitterSettings_HasConsumerKey', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherTwitterSettings_HasConsumerSecret')
    EXEC sp_rename 'dbo.UserPlatformTwitterSettings.DF_UserPublisherTwitterSettings_HasConsumerSecret', 'DF_UserPlatformTwitterSettings_HasConsumerSecret', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherTwitterSettings_HasAccessToken')
    EXEC sp_rename 'dbo.UserPlatformTwitterSettings.DF_UserPublisherTwitterSettings_HasAccessToken', 'DF_UserPlatformTwitterSettings_HasAccessToken', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherTwitterSettings_HasAccessTokenSecret')
    EXEC sp_rename 'dbo.UserPlatformTwitterSettings.DF_UserPublisherTwitterSettings_HasAccessTokenSecret', 'DF_UserPlatformTwitterSettings_HasAccessTokenSecret', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherTwitterSettings_CreatedOn')
    EXEC sp_rename 'dbo.UserPlatformTwitterSettings.DF_UserPublisherTwitterSettings_CreatedOn', 'DF_UserPlatformTwitterSettings_CreatedOn', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherTwitterSettings_LastUpdatedOn')
    EXEC sp_rename 'dbo.UserPlatformTwitterSettings.DF_UserPublisherTwitterSettings_LastUpdatedOn', 'DF_UserPlatformTwitterSettings_LastUpdatedOn', 'OBJECT';
GO

-- ============================================================
-- 4. UserPublisherLinkedInSettings → UserPlatformLinkedInSettings
-- ============================================================
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPublisherLinkedInSettings')
    EXEC sp_rename 'dbo.UserPublisherLinkedInSettings', 'UserPlatformLinkedInSettings';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'PK_UserPublisherLinkedInSettings')
    EXEC sp_rename 'dbo.UserPlatformLinkedInSettings.PK_UserPublisherLinkedInSettings', 'PK_UserPlatformLinkedInSettings', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_UserPublisherLinkedInSettings_Owner')
    EXEC sp_rename 'dbo.UserPlatformLinkedInSettings.UQ_UserPublisherLinkedInSettings_Owner', 'UQ_UserPlatformLinkedInSettings_Owner', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherLinkedInSettings_IsEnabled')
    EXEC sp_rename 'dbo.UserPlatformLinkedInSettings.DF_UserPublisherLinkedInSettings_IsEnabled', 'DF_UserPlatformLinkedInSettings_IsEnabled', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherLinkedInSettings_HasClientSecret')
    EXEC sp_rename 'dbo.UserPlatformLinkedInSettings.DF_UserPublisherLinkedInSettings_HasClientSecret', 'DF_UserPlatformLinkedInSettings_HasClientSecret', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherLinkedInSettings_CreatedOn')
    EXEC sp_rename 'dbo.UserPlatformLinkedInSettings.DF_UserPublisherLinkedInSettings_CreatedOn', 'DF_UserPlatformLinkedInSettings_CreatedOn', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherLinkedInSettings_LastUpdatedOn')
    EXEC sp_rename 'dbo.UserPlatformLinkedInSettings.DF_UserPublisherLinkedInSettings_LastUpdatedOn', 'DF_UserPlatformLinkedInSettings_LastUpdatedOn', 'OBJECT';
GO

-- ============================================================
-- 5. UserPublisherFacebookSettings → UserPlatformFacebookSettings
-- ============================================================
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserPublisherFacebookSettings')
    EXEC sp_rename 'dbo.UserPublisherFacebookSettings', 'UserPlatformFacebookSettings';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'PK_UserPublisherFacebookSettings')
    EXEC sp_rename 'dbo.UserPlatformFacebookSettings.PK_UserPublisherFacebookSettings', 'PK_UserPlatformFacebookSettings', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'UQ_UserPublisherFacebookSettings_Owner')
    EXEC sp_rename 'dbo.UserPlatformFacebookSettings.UQ_UserPublisherFacebookSettings_Owner', 'UQ_UserPlatformFacebookSettings_Owner', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherFacebookSettings_IsEnabled')
    EXEC sp_rename 'dbo.UserPlatformFacebookSettings.DF_UserPublisherFacebookSettings_IsEnabled', 'DF_UserPlatformFacebookSettings_IsEnabled', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherFacebookSettings_HasPageAccessToken')
    EXEC sp_rename 'dbo.UserPlatformFacebookSettings.DF_UserPublisherFacebookSettings_HasPageAccessToken', 'DF_UserPlatformFacebookSettings_HasPageAccessToken', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherFacebookSettings_HasAppSecret')
    EXEC sp_rename 'dbo.UserPlatformFacebookSettings.DF_UserPublisherFacebookSettings_HasAppSecret', 'DF_UserPlatformFacebookSettings_HasAppSecret', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherFacebookSettings_HasClientToken')
    EXEC sp_rename 'dbo.UserPlatformFacebookSettings.DF_UserPublisherFacebookSettings_HasClientToken', 'DF_UserPlatformFacebookSettings_HasClientToken', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherFacebookSettings_HasShortLivedAccessToken')
    EXEC sp_rename 'dbo.UserPlatformFacebookSettings.DF_UserPublisherFacebookSettings_HasShortLivedAccessToken', 'DF_UserPlatformFacebookSettings_HasShortLivedAccessToken', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherFacebookSettings_HasLongLivedAccessToken')
    EXEC sp_rename 'dbo.UserPlatformFacebookSettings.DF_UserPublisherFacebookSettings_HasLongLivedAccessToken', 'DF_UserPlatformFacebookSettings_HasLongLivedAccessToken', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherFacebookSettings_CreatedOn')
    EXEC sp_rename 'dbo.UserPlatformFacebookSettings.DF_UserPublisherFacebookSettings_CreatedOn', 'DF_UserPlatformFacebookSettings_CreatedOn', 'OBJECT';
GO

IF EXISTS (SELECT 1 FROM sys.objects WHERE name = 'DF_UserPublisherFacebookSettings_LastUpdatedOn')
    EXEC sp_rename 'dbo.UserPlatformFacebookSettings.DF_UserPublisherFacebookSettings_LastUpdatedOn', 'DF_UserPlatformFacebookSettings_LastUpdatedOn', 'OBJECT';
GO

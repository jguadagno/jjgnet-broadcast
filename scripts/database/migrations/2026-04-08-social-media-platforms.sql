-- ============================================================================
-- Epic #667: Social Media Platforms Table and Junction Tables
-- Created: 2026-04-08
-- Issues: #53, #54, #536, #537, #667
--
-- Summary:
-- Creates dbo.SocialMediaPlatforms lookup table and 
-- dbo.EngagementSocialMediaPlatforms junction table to replace ad-hoc social 
-- media fields (BlueSkyHandle, ConferenceHashtag, ConferenceTwitterHandle).
--
-- Also migrates ScheduledItems.Platform (nvarchar) to SocialMediaPlatformId (int FK)
-- and MessageTemplates.Platform (part of composite PK) to SocialMediaPlatformId.
--
-- Strategy for MessageTemplates migration:
-- Platform is part of the composite PK (Platform, MessageType), so we:
-- 1. Add new SocialMediaPlatformId column (nullable initially)
-- 2. Populate it based on Platform string mapping
-- 3. Drop old PK constraint
-- 4. Create new PK on (SocialMediaPlatformId, MessageType)
-- 5. Drop old Platform column
-- 6. Add FK constraint to SocialMediaPlatforms
-- ============================================================================

use JJGNet
go

-- ============================================================================
-- PART 1: Create new SocialMediaPlatforms table
-- ============================================================================

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

-- ============================================================================
-- PART 2: Create EngagementSocialMediaPlatforms junction table
-- ============================================================================

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

-- ============================================================================
-- PART 3: Seed SocialMediaPlatforms data
-- ============================================================================

-- Twitter/X
INSERT INTO dbo.SocialMediaPlatforms (Name, Url, Icon, IsActive)
VALUES ('Twitter', 'https://twitter.com', 'bi-twitter-x', 1)
go

-- BlueSky
INSERT INTO dbo.SocialMediaPlatforms (Name, Url, Icon, IsActive)
VALUES ('BlueSky', 'https://bsky.app', 'bi-cloud', 1)
go

-- LinkedIn
INSERT INTO dbo.SocialMediaPlatforms (Name, Url, Icon, IsActive)
VALUES ('LinkedIn', 'https://www.linkedin.com', 'bi-linkedin', 1)
go

-- Facebook
INSERT INTO dbo.SocialMediaPlatforms (Name, Url, Icon, IsActive)
VALUES ('Facebook', 'https://www.facebook.com', 'bi-facebook', 1)
go

-- Mastodon
INSERT INTO dbo.SocialMediaPlatforms (Name, Url, Icon, IsActive)
VALUES ('Mastodon', 'https://mastodon.social', 'bi-mastodon', 1)
go

-- ============================================================================
-- PART 4: Migrate ScheduledItems.Platform to SocialMediaPlatformId
-- ============================================================================

-- Add SocialMediaPlatformId column (nullable initially for safe migration)
ALTER TABLE dbo.ScheduledItems
    ADD SocialMediaPlatformId int null
go

-- Populate SocialMediaPlatformId based on existing Platform string values
-- Best-effort mapping: Twitter, Bluesky, LinkedIn, Facebook
UPDATE dbo.ScheduledItems
SET SocialMediaPlatformId = (
    SELECT TOP 1 Id 
    FROM dbo.SocialMediaPlatforms 
    WHERE Name = 'Twitter'
)
WHERE Platform IN ('Twitter', 'twitter', 'X', 'x', 'TwitterX')
go

UPDATE dbo.ScheduledItems
SET SocialMediaPlatformId = (
    SELECT TOP 1 Id 
    FROM dbo.SocialMediaPlatforms 
    WHERE Name = 'BlueSky'
)
WHERE Platform IN ('BlueSky', 'Bluesky', 'bluesky', 'bsky')
go

UPDATE dbo.ScheduledItems
SET SocialMediaPlatformId = (
    SELECT TOP 1 Id 
    FROM dbo.SocialMediaPlatforms 
    WHERE Name = 'LinkedIn'
)
WHERE Platform IN ('LinkedIn', 'linkedin')
go

UPDATE dbo.ScheduledItems
SET SocialMediaPlatformId = (
    SELECT TOP 1 Id 
    FROM dbo.SocialMediaPlatforms 
    WHERE Name = 'Facebook'
)
WHERE Platform IN ('Facebook', 'facebook', 'fb', 'FB')
go

UPDATE dbo.ScheduledItems
SET SocialMediaPlatformId = (
    SELECT TOP 1 Id 
    FROM dbo.SocialMediaPlatforms 
    WHERE Name = 'Mastodon'
)
WHERE Platform IN ('Mastodon', 'mastodon')
go

-- Add FK constraint to SocialMediaPlatforms
ALTER TABLE dbo.ScheduledItems
    ADD CONSTRAINT FK_ScheduledItems_SocialMediaPlatforms
        FOREIGN KEY (SocialMediaPlatformId)
        REFERENCES dbo.SocialMediaPlatforms(Id)
go

-- Drop old Platform column
ALTER TABLE dbo.ScheduledItems
    DROP COLUMN Platform
go

-- ============================================================================
-- PART 5: Migrate MessageTemplates.Platform to SocialMediaPlatformId
-- ============================================================================

-- Add new SocialMediaPlatformId column (nullable initially)
ALTER TABLE dbo.MessageTemplates
    ADD SocialMediaPlatformId int null
go

-- Populate SocialMediaPlatformId based on existing Platform string values
UPDATE dbo.MessageTemplates
SET SocialMediaPlatformId = (
    SELECT TOP 1 Id 
    FROM dbo.SocialMediaPlatforms 
    WHERE Name = 'Twitter'
)
WHERE Platform IN ('Twitter', 'twitter', 'X', 'x')
go

UPDATE dbo.MessageTemplates
SET SocialMediaPlatformId = (
    SELECT TOP 1 Id 
    FROM dbo.SocialMediaPlatforms 
    WHERE Name = 'BlueSky'
)
WHERE Platform IN ('BlueSky', 'Bluesky', 'bluesky')
go

UPDATE dbo.MessageTemplates
SET SocialMediaPlatformId = (
    SELECT TOP 1 Id 
    FROM dbo.SocialMediaPlatforms 
    WHERE Name = 'LinkedIn'
)
WHERE Platform IN ('LinkedIn', 'linkedin')
go

UPDATE dbo.MessageTemplates
SET SocialMediaPlatformId = (
    SELECT TOP 1 Id 
    FROM dbo.SocialMediaPlatforms 
    WHERE Name = 'Facebook'
)
WHERE Platform IN ('Facebook', 'facebook')
go

-- Drop old composite PK constraint
ALTER TABLE dbo.MessageTemplates
    DROP CONSTRAINT PK_MessageTemplates
go

-- Make SocialMediaPlatformId NOT NULL now that it's populated
ALTER TABLE dbo.MessageTemplates
    ALTER COLUMN SocialMediaPlatformId int NOT NULL
go

-- Create new composite PK on (SocialMediaPlatformId, MessageType)
ALTER TABLE dbo.MessageTemplates
    ADD CONSTRAINT PK_MessageTemplates
        PRIMARY KEY (SocialMediaPlatformId, MessageType)
go

-- Add FK constraint to SocialMediaPlatforms
ALTER TABLE dbo.MessageTemplates
    ADD CONSTRAINT FK_MessageTemplates_SocialMediaPlatforms
        FOREIGN KEY (SocialMediaPlatformId)
        REFERENCES dbo.SocialMediaPlatforms(Id)
go

-- Drop old Platform column
ALTER TABLE dbo.MessageTemplates
    DROP COLUMN Platform
go

-- ============================================================================
-- PART 6: Remove old social media columns from Engagements
-- ============================================================================

-- Drop the superseded columns
ALTER TABLE dbo.Engagements
    DROP COLUMN ConferenceHashtag
go

ALTER TABLE dbo.Engagements
    DROP COLUMN ConferenceTwitterHandle
go

ALTER TABLE dbo.Engagements
    DROP COLUMN BlueSkyHandle
go

-- ============================================================================
-- PART 7: Remove old BlueSkyHandle from Talks
-- ============================================================================

-- Talks inherit social media from parent Engagement via junction table
ALTER TABLE dbo.Talks
    DROP COLUMN BlueSkyHandle
go

-- ============================================================================
-- Verification
-- ============================================================================

SELECT 'SocialMediaPlatforms' as TableName, COUNT(*) as RowCount FROM dbo.SocialMediaPlatforms
UNION ALL
SELECT 'EngagementSocialMediaPlatforms', COUNT(*) FROM dbo.EngagementSocialMediaPlatforms
UNION ALL
SELECT 'MessageTemplates', COUNT(*) FROM dbo.MessageTemplates
UNION ALL
SELECT 'ScheduledItems', COUNT(*) FROM dbo.ScheduledItems
go

SELECT * FROM dbo.SocialMediaPlatforms
ORDER BY Name
go

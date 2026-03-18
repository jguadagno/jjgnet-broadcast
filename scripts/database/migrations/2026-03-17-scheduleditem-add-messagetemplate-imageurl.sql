-- Migration: ScheduledItems Add ImageUrl + New MessageTemplates table (Issue #269)
-- Date: 2026-03-17
--
-- 1. Add ImageUrl column (NVARCHAR(2048), nullable) to ScheduledItems for image URLs to include in broadcasts
-- 2. Create MessageTemplates table with composite PK (Platform, MessageType) for Scriban broadcast message templates
-- 3. Seed default templates for Twitter, Facebook, LinkedIn, and Bluesky

ALTER TABLE dbo.ScheduledItems
    ADD ImageUrl nvarchar(2048) NULL;
go

CREATE TABLE [dbo].[MessageTemplates] (
    [Platform]    NVARCHAR(50)  NOT NULL,
    [MessageType] NVARCHAR(50)  NOT NULL,
    [Template]    NVARCHAR(MAX) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    CONSTRAINT [PK_MessageTemplates] PRIMARY KEY ([Platform], [MessageType])
);
go

INSERT INTO [dbo].[MessageTemplates] ([Platform], [MessageType], [Template], [Description]) VALUES
    ('Twitter',  'RandomPost', '{{ title }} - {{ url }}',                                    'Default Twitter random post template'),
    ('Facebook', 'RandomPost', '{{ title }}' + CHAR(10) + CHAR(10) + '{{ description }}' + CHAR(10) + CHAR(10) + '{{ url }}', 'Default Facebook random post template'),
    ('LinkedIn', 'RandomPost', '{{ title }}' + CHAR(10) + CHAR(10) + '{{ description }}' + CHAR(10) + CHAR(10) + '{{ url }}', 'Default LinkedIn random post template'),
    ('Bluesky',  'RandomPost', '{{ title }} - {{ url }}',                                    'Default Bluesky random post template');
go

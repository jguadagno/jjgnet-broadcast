-- Sprint 7: Seed default Scriban templates for all 4 platforms
-- Issues: #475 (Bluesky), #476 (Facebook), #477 (LinkedIn), #478 (Twitter)
-- Epic: #474 (Templatize all messages)

use JJGNet
go

-- ============================================================================
-- Bluesky Templates (300 char limit, supports rich text links)
-- Issue #475
-- ============================================================================

-- Bluesky: New Syndication Feed Item (Blog Post)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Bluesky',
    'NewSyndicationFeedItem',
    'Blog Post: {{ title }} {{ url }} {{ tags }}',
    'Bluesky template for new blog posts with title, URL, and hashtags'
);

-- Bluesky: New YouTube Item (Video)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Bluesky',
    'NewYouTubeItem',
    'Video: {{ title }} {{ url }} {{ tags }}',
    'Bluesky template for new YouTube videos with title, URL, and hashtags'
);

-- Bluesky: New Speaking Engagement
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Bluesky',
    'NewSpeakingEngagement',
    'I''m speaking at {{ title }} ({{ url }}) {{ description }}',
    'Bluesky template for upcoming speaking engagements'
);

-- Bluesky: Scheduled Item (Talk)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Bluesky',
    'ScheduledItem',
    'My talk: {{ title }} ({{ url }}) {{ description }} Come see it!',
    'Bluesky template for scheduled talks at conferences'
);

-- Bluesky: Random Post
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Bluesky',
    'RandomPost',
    '{{ title }} {{ url }} {{ tags }}',
    'Bluesky template for random posts'
);

-- ============================================================================
-- Facebook Templates (2000 char limit, link preview handles URL)
-- Issue #476
-- ============================================================================

-- Facebook: New Syndication Feed Item (Blog Post)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Facebook',
    'NewSyndicationFeedItem',
    'ICYMI: Blog Post: {{ title }} {{ tags }}',
    'Facebook template for new blog posts (URL handled via LinkUri)'
);

-- Facebook: New YouTube Item (Video)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Facebook',
    'NewYouTubeItem',
    'ICYMI: Video: {{ title }} {{ tags }}',
    'Facebook template for new YouTube videos (URL handled via LinkUri)'
);

-- Facebook: New Speaking Engagement
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Facebook',
    'NewSpeakingEngagement',
    'I''m speaking at {{ title }} ({{ url }})

{{ description }}',
    'Facebook template for upcoming speaking engagements'
);

-- Facebook: Scheduled Item (Talk)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Facebook',
    'ScheduledItem',
    'Talk: {{ title }} ({{ url }})

{{ description }}',
    'Facebook template for scheduled talks at conferences'
);

-- Facebook: Random Post
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Facebook',
    'RandomPost',
    '{{ title }}

{{ description }}',
    'Facebook template for random posts'
);

-- ============================================================================
-- LinkedIn Templates (professional tone, supports hashtags)
-- Issue #477
-- ============================================================================

-- LinkedIn: New Syndication Feed Item (Blog Post)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'LinkedIn',
    'NewSyndicationFeedItem',
    'New blog post: {{ title }}

{{ description }}

{{ tags }}',
    'LinkedIn template for new blog posts with professional formatting'
);

-- LinkedIn: New YouTube Item (Video)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'LinkedIn',
    'NewYouTubeItem',
    'New video: {{ title }}

{{ description }}

{{ tags }}',
    'LinkedIn template for new YouTube videos with professional formatting'
);

-- LinkedIn: New Speaking Engagement
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'LinkedIn',
    'NewSpeakingEngagement',
    'Excited to announce I''ll be speaking at {{ title }}!

{{ description }}

Learn more: {{ url }}',
    'LinkedIn template for upcoming speaking engagements'
);

-- LinkedIn: Scheduled Item (Talk)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'LinkedIn',
    'ScheduledItem',
    'My talk: {{ title }}

{{ description }}

Join me: {{ url }}',
    'LinkedIn template for scheduled talks at conferences'
);

-- LinkedIn: Random Post
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'LinkedIn',
    'RandomPost',
    '{{ title }}

{{ description }}

{{ tags }}',
    'LinkedIn template for random posts'
);

-- ============================================================================
-- Twitter/X Templates (280 char limit, needs URL shortening awareness)
-- Issue #478
-- ============================================================================

-- Twitter: New Syndication Feed Item (Blog Post)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Twitter',
    'NewSyndicationFeedItem',
    'Blog Post: {{ title }} {{ url }} {{ tags }}',
    'Twitter template for new blog posts (compact format for 280 char limit)'
);

-- Twitter: New YouTube Item (Video)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Twitter',
    'NewYouTubeItem',
    'Video: {{ title }} {{ url }} {{ tags }}',
    'Twitter template for new YouTube videos (compact format for 280 char limit)'
);

-- Twitter: New Speaking Engagement
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Twitter',
    'NewSpeakingEngagement',
    'I''m speaking at {{ title }} ({{ url }}) {{ description }}',
    'Twitter template for upcoming speaking engagements'
);

-- Twitter: Scheduled Item (Talk)
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Twitter',
    'ScheduledItem',
    'My talk: {{ title }} ({{ url }}) {{ description }} Come see it!',
    'Twitter template for scheduled talks at conferences'
);

-- Twitter: Random Post
INSERT INTO dbo.MessageTemplates (Platform, MessageType, Template, Description)
VALUES (
    'Twitter',
    'RandomPost',
    '{{ title }} {{ url }} {{ tags }}',
    'Twitter template for random posts'
);

go

-- Verify all templates were inserted
SELECT Platform, MessageType, Description
FROM dbo.MessageTemplates
ORDER BY Platform, MessageType;
go

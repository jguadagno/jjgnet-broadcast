-- Rename SyndicationFeedSources to SyndicationFeedItems
EXEC sp_rename 'SyndicationFeedSources', 'SyndicationFeedItems';
EXEC sp_rename 'dbo.SyndicationFeedItems.SyndicationFeedSource_pk_Id', 'SyndicationFeedItem_pk_Id', 'INDEX';

-- Rename YouTubeSources to YouTubeItems
EXEC sp_rename 'YouTubeSources', 'YouTubeItems';
EXEC sp_rename 'YouTubeSources.YouTubeItem_pk_Id', 'YouTubeItem_pk_Id', 'INDEX';
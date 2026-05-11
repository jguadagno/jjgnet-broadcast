-- Rename SyndicationFeedItems to SyndicationFeedItems
EXEC sp_rename 'SyndicationFeedItems', 'SyndicationFeedItems';
EXEC sp_rename 'SyndicationFeedItems.SyndicationFeedItem_pk_Id', 'SyndicationFeedItem_pk_Id', 'INDEX';

-- Rename YouTubeItems to YouTubeItems
EXEC sp_rename 'YouTubeItems', 'YouTubeItems';
EXEC sp_rename 'YouTubeItems.YouTubeItem_pk_Id', 'YouTubeItem_pk_Id', 'INDEX';
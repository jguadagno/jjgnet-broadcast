namespace JosephGuadagno.Broadcasting.Domain;

public static class Constants
{
    public static class Queues
    {
        public const string TwitterTweetsToSend = "twitter-tweets-to-send";
        public const string FacebookPostStatusToPage = "facebook-post-status-to-page";
        public const string LinkedInPostLink = "linkedin-post-link";
        public const string LinkedInPostText = "linkedin-post-text";
        public const string LinkedInPostImage = "linkedin-post-image";
        public const string BlueskyPostToSend = "bluesky-post-to-send";
    }

    public static class Tables
    {
        public const string Configuration  = "Configuration";
        public const string SourceData = "SourceData";
        public const string Logging = "Logging";
        public const string TokenRefresh = "TokenRefresh";
    }

    public static class ConfigurationFunctionNames
    {
        public const string CollectorsFeedLoadNewPosts = "CollectorsFeedLoadNewPosts";
        public const string CollectorsFeedLoadAllPosts = "CollectorsFeedLoadAllPosts";
        public const string CollectorsYouTubeLoadNewVideos = "CollectorsYouTubeLoadNewVideos";
        public const string CollectorsYouTubeLoadAllVideos = "CollectorsYouTubeLoadAllVideos";
        public const string PublishersRandomPosts = "PublishersRandomPosts";
        public const string PublishersScheduledItems = "PublishersScheduledItems";
        public const string FacebookPostPageStatus = "FacebookPostPageStatus";
        public const string FacebookProcessNewSourceData = "FacebookProcessNewSourceData";
        public const string FacebookProcessScheduledItemFired = "FacebookProcessScheduledItemFired";
        public const string FacebookTokenRefresh = "FacebookTokenRefresh";
        public const string TwitterSendTweet = "TwitterSendTweet";
        public const string TwitterProcessNewSourceData = "TwitterProcessNewSourceData";
        public const string TwitterProcessScheduledItemFired = "TwitterProcessScheduledItemFired";
        public const string TwitterProcessRandomPostFired = "TwitterProcessRandomPostFired";
        public const string MaintenanceClearOldLogs = "MaintenanceClearOldLogs";
        public const string MaintenanceAdClientSecretRefresh = "MaintenanceAdClientSecretRefresh";
        public const string LinkedInPostLink = "LinkedInPostLink";
        public const string LinkedInPostText = "LinkedInPostText";
        public const string LinkedInPostImage = "LinkedInPostImage";
        public const string LinkedInProcessNewSourceData = "LinkedInProcessNewSourceData";
        public const string LinkedInProcessScheduledItemFired = "LinkedInProcessScheduledItemFired";
        public const string BlueskyPostMessage = "BlueskyPostMessage";
        public const string BlueskyProcessRandomPostFired = "BlueskyProcessRandomPostFired";
        public const string BlueskyProcessNewSourceDataFired = "BlueskyProcessNewSourceDataFired";
        public const string BlueskyProcessScheduledItemFired = "BlueskyProcessScheduledItemFired";
    }

    public static class Topics
    {
        public const string NewSourceData = "new-source-data";
        public const string ScheduledItemFired = "scheduled-item-fired";
        public const string NewRandomPost = "new-random-post";
    }

    public static class Metrics
    {
        public const string PostAddedOrUpdated = "PostAddedOrUpdated";
        public const string VideoAddedOrUpdated = "VideoAddedOrUpdated";
        public const string ScheduledItemFired = "ScheduledItemFired";
        public const string RandomPostFired = "RandomPostFired";
        public const string ClearOldLogs = "ClearOldLogs";
        
        public const string BlueskyProcessedNewSourceData = "BlueskyProcessedNewSourceData";
        public const string BlueskyProcessedScheduledItemFired = "BlueskyProcessedScheduledItemFired";
        public const string BlueskyProcessedRandomPost = "BlueskyProcessedRandomPost";
        public const string BlueskyPostSent = "BlueskyPostSent";

        public const string FacebookProcessedNewSourceData = "FacebookProcessedNewSourceData";
        public const string FacebookProcessedScheduledItemFired = "FacebookProcessedScheduledItemFired";
        public const string FacebookPostPageStatus = "FacebookPostPageStatus";

        public const string LinkedInProcessedNewSourceData = "LinkedInProcessedNewSourceData";
        public const string LinkedInProcessedScheduledItemFired = "LinkedInProcessedScheduledItemFired";
        public const string LinkedInPostImage = "LinkedInPostImage";
        public const string LinkedInPostLink = "LinkedInPostLink";
        public const string LinkedInPostText = "LinkedInPostText";

        public const string TwitterProcessedRandomPost = "TwitterProcessedRandomPost";
        public const string TwitterProcessedNewSourceData = "TwitterProcessedNewSourceData";
        public const string TwitterProcessScheduledItemFired = "TwitterProcessScheduledItemFired";
        public const string TwitterPostSent = "TwitterPostSent";
    }
}
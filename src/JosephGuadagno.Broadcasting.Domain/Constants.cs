namespace JosephGuadagno.Broadcasting.Domain;

public static class Constants
{
    public static class Queues
    {
        public const string TwitterTweetsToSend = "twitter-tweets-to-send";
        public const string FacebookPostStatusToPage = "facebook-post-status-to-page";
    }

    public static class Tables
    {
        public const string Configuration  = "Configuration";
        public const string SourceData = "SourceData";
        public const string Engagements = "Engagements";
        public const string Talks = "Talks";
        public const string ScheduledItems = "ScheduledItems";
    }

    public static class ConfigurationFunctionNames
    {
        public const string CollectorsFeedLoadNewPosts = "CollectorsFeedLoadNewPosts";
        public const string CollectorsFeedLoadAllPosts = "CollectorsFeedLoadAllPosts";
        public const string CollectorsYouTubeLoadNewVideos = "CollectorsYouTubeLoadNewVideos";
        public const string CollectorsYouTubeLoadAllVideos = "CollectorsYouTubeLoadAllVideos";
        public const string PublishersRandomPosts = "PublishersRandomPosts";
    }

    public static class Topics
    {
        public const string NewSourceData = "new-source-data";
    }

    public static class Metrics
    {
        public const string PostAddedOrUpdated = "PostAddedOrUpdated";
        public const string VideoAddedOrUpdated = "VideoAddedOrUpdated";
        public const string RandomTweetSent = "RandomTweetSent";
    }
}
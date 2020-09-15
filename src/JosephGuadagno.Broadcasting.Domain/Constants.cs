namespace JosephGuadagno.Broadcasting.Domain
{
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
        }

        public static class ConfigurationFunctionNames
        {
            public const string CollectorsCheckForFeedUpdates = "CollectorsCheckForFeedUpdates";
            public const string CollectorsLoadJsonFeedItems = "CollectorsLoadJsonFeedItems";
        }

        public static class Topics
        {
            public const string NewSourceData = "new-source-data";
        }
    }
}
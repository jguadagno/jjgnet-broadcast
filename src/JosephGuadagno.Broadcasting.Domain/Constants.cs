namespace JosephGuadagno.Broadcasting.Domain
{
    public static class Constants
    {
        public static class Queues
        {
            public const string TwitterTweetsToSend = "twitter-tweets-to-send";
        }

        public static class Tables
        {
            public const string Configuration  = "Configuration";
            public const string SourceData = "SourceData";
        }

        public static class ConfigurationFunctionNames
        {
            public const string CollectorsFeedCollector = "CollectorsFeedCollector";
        }

        public static class Topics
        {
            public const string NewSourceData = "new-source-data";
        }
    }
}
using System.Net.NetworkInformation;

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
            public const string ScheduledTweets = "ScheduledTweets";
        }

        public static class ConfigurationFunctionNames
        {
            public const string NewPostChecker = "NewPostChecker";
        }
    }
}
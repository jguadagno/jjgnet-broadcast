namespace JosephGuadagno.Broadcasting.Domain.Constants;

public static class MessageTemplates
{
    public static class Platforms
    {
        public const string Twitter = "Twitter";
        public const string Facebook = "Facebook";
        public const string LinkedIn = "LinkedIn";
        public const string Bluesky = "Bluesky";
    }

    public static class MessageTypes
    {
        public const string RandomPost = "RandomPost";
        public const string NewSyndicationFeedItem = "NewSyndicationFeedItem";
        public const string NewYouTubeItem = "NewYouTubeItem";
        public const string NewSpeakingEngagement = "NewSpeakingEngagement";
        public const string ScheduledItem = "ScheduledItem";
    }
}

namespace JosephGuadagno.Broadcasting.Web.Constants;

/// <summary>
/// Bootstrap icon class names and display labels for collector types used across the Web application.
/// </summary>
public static class CollectorIcons
{
    /// <summary>Bootstrap icon for the Collectors section as a whole.</summary>
    public const string Collection = "bi-collection";

    /// <summary>Icon and label constants for the RSS / Atom / JSON feed collector type.</summary>
    public static class FeedSource
    {
        public const string MessageType = "NewSyndicationFeedItem";
        public const string Icon = "bi-rss";
        public const string Label = "RSS / Atom Feed";
    }

    /// <summary>Icon and label constants for the YouTube channel collector type.</summary>
    public static class YouTubeChannel
    {
        public const string MessageType = "NewYouTubeItem";
        public const string Icon = "bi-youtube";
        public const string Label = "YouTube Channel";
    }

    /// <summary>Icon and label constants for the speaking engagement collector type.</summary>
    public static class SpeakingEngagement
    {
        public const string MessageType = "NewSpeakingEngagement";
        public const string Icon = "bi-mic-fill";
        public const string Label = "Speaking Engagement";
    }

    /// <summary>
    /// Lookup from MessageType constant string → (Icon, Label).
    /// Falls back to (Collection icon, messageType) for unknown types.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Icon, string Label)> ByMessageType =
        new Dictionary<string, (string Icon, string Label)>(StringComparer.OrdinalIgnoreCase)
        {
            [FeedSource.MessageType] = (FeedSource.Icon, FeedSource.Label),
            [YouTubeChannel.MessageType] = (YouTubeChannel.Icon, YouTubeChannel.Label),
            [SpeakingEngagement.MessageType] = (SpeakingEngagement.Icon, SpeakingEngagement.Label),
        };
}

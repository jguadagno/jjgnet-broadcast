namespace JosephGuadagno.Broadcasting.Domain.Constants;

public static class ConfigurationFunctionNames
{
    // Collectors
    public const string CollectorsFeedLoadNewPosts = "CollectorsFeedLoadNewPosts";
    public const string CollectorsFeedLoadAllPosts = "CollectorsFeedLoadAllPosts";
    public const string CollectorsSpeakingEngagementsLoadNew = "CollectorsSpeakingEngagementsLoadNew";
    public const string CollectorsSpeakingEngagementsLoadAll = "CollectorsSpeakingEngagementsLoadAll";
    public const string CollectorsYouTubeLoadNewVideos = "CollectorsYouTubeLoadNewVideos";
    public const string CollectorsYouTubeLoadAllVideos = "CollectorsYouTubeLoadAllVideos";

    // Distributors
    public const string DistributorsRandomPosts = "DistributorsRandomPosts";
    public const string DistributorsScheduledItems = "DistributorsScheduledItems";

    // Social media platforms
    public const string BlueskyPostMessage = "BlueskyPostMessage";
    public const string FacebookPostPageStatus = "FacebookPostPageStatus";
    public const string LinkedInPostLink = "LinkedInPostLink";
    public const string TwitterSendTweet = "TwitterSendTweet";

    // Email
    public const string EmailSendEmail = "EmailSendEmail";
    public const string EmailSendEmailPoison = "EmailSendEmailPoison";

    // Maintenance
    public const string FacebookTokenRefresh = "FacebookTokenRefresh";
    public const string MaintenanceClearOldLogs = "MaintenanceClearOldLogs";
    public const string LinkedInNotifyExpiringTokens = "LinkedInNotifyExpiringTokens";
}
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

    // Publishers
    public const string PublishersRandomPosts = "PublishersRandomPosts";
    public const string PublishersScheduledItems = "PublishersScheduledItems";

    // Social media platforms
    public const string FacebookPostPageStatus = "FacebookPostPageStatus";
    public const string FacebookProcessNewSyndicationDataFired = "FacebookProcessNewSyndicationDataFired";
    public const string FacebookProcessNewYouTubeDataFired = "FacebookProcessNewYouTubeDataFired";
    public const string FacebookProcessScheduledItemFired = "FacebookProcessScheduledItemFired";
    public const string TwitterSendTweet = "TwitterSendTweet";
    public const string TwitterProcessNewSyndicationDataFired = "TwitterProcessNewSyndicationDataFired";
    public const string TwitterProcessNewYouTubeDataFired = "TwitterProcessNewYouTubeDataFired";
    public const string TwitterProcessScheduledItemFired = "TwitterProcessScheduledItemFired";
    public const string TwitterProcessRandomPostFired = "TwitterProcessRandomPostFired";
    public const string LinkedInPostLink = "LinkedInPostLink";
    public const string LinkedInPostText = "LinkedInPostText";
    public const string LinkedInPostImage = "LinkedInPostImage";
    public const string LinkedInProcessNewSyndicationDataFired = "LinkedInProcessNewSyndicationDataFired";
    public const string LinkedInProcessNewYouTubeDataFired = "LinkedInProcessNewYouTubeDataFired";
    public const string LinkedInProcessScheduledItemFired = "LinkedInProcessScheduledItemFired";
    public const string BlueskyPostMessage = "BlueskyPostMessage";
    public const string BlueskyProcessRandomPostFired = "BlueskyProcessRandomPostFired";
    public const string BlueskyProcessNewSyndicationDataFired = "BlueskyProcessNewSyndicationDataFired";
    public const string BlueskyProcessNewYouTubeDataFired = "BlueskyProcessNewYouTubeDataFired";
    public const string BlueskyProcessScheduledItemFired = "BlueskyProcessScheduledItemFired";

    // Maintenance
    public const string FacebookTokenRefresh = "FacebookTokenRefresh";
    public const string MaintenanceClearOldLogs = "MaintenanceClearOldLogs";
}
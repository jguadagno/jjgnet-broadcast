namespace JosephGuadagno.Broadcasting.Domain.Constants;

public static class Metrics
{
    public const string PostAddedOrUpdated = "PostAddedOrUpdated";
    public const string VideoAddedOrUpdated = "VideoAddedOrUpdated";
    public const string SpeakingEngagementAddedOrUpdated = "SpeakingEngagementAddedOrUpdated";
    public const string ScheduledItemFired = "ScheduledItemFired";
    public const string RandomPostFired = "RandomPostFired";
    public const string RandomPostCronCircuitBroken = "RandomPostCronCircuitBroken";
    public const string ClearOldLogs = "ClearOldLogs";

    public const string BlueskyPostSent = "BlueskyPostSent";
    public const string FacebookPostPageStatus = "FacebookPostPageStatus";
    public const string LinkedInPostLink = "LinkedInPostLink";
    public const string TwitterPostSent = "TwitterPostSent";
}
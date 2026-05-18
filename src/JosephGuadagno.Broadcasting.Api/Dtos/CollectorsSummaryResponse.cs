namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Aggregated summary of all collector configurations for the resolved owner.
/// </summary>
public class CollectorsSummaryResponse
{
    /// <summary>
    /// The number of YouTube channel collector configurations for the resolved owner.
    /// </summary>
    public int YouTubeChannelCount { get; set; }

    /// <summary>
    /// The number of RSS/Atom feed source collector configurations for the resolved owner.
    /// </summary>
    public int FeedSourceCount { get; set; }

    /// <summary>
    /// The number of speaking engagement collector configurations for the resolved owner.
    /// </summary>
    public int SpeakingEngagementCount { get; set; }
}

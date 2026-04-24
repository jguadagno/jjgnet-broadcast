namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for the collector settings page.
/// </summary>
public class CollectorSettingsPageViewModel
{
    public string TargetUserEntraOid { get; set; } = string.Empty;
    public string? TargetUserDisplayName { get; set; }
    public bool IsManagedBySiteAdmin { get; set; }
    public List<UserCollectorFeedSourceViewModel> FeedSources { get; set; } = [];
    public List<UserCollectorYouTubeChannelViewModel> YouTubeChannels { get; set; } = [];
}

namespace JosephGuadagno.Broadcasting.Web.Models;

/// <summary>
/// View model for the publisher settings page.
/// </summary>
public class UserPublisherSettingsPageViewModel
{
    public string TargetUserEntraOid { get; set; } = string.Empty;
    public string? TargetUserDisplayName { get; set; }
    public bool IsManagedBySiteAdmin { get; set; }
    public List<PublisherPlatformSettingsViewModel> Platforms { get; set; } = [];
}

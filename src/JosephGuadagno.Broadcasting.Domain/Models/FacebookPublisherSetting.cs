namespace JosephGuadagno.Broadcasting.Domain.Models;

public class FacebookPublisherSetting
{
    public string? PageId { get; set; }

    public string? AppId { get; set; }

    public bool HasPageAccessToken { get; set; }

    public bool HasAppSecret { get; set; }

    public bool HasClientToken { get; set; }

    public bool HasShortLivedAccessToken { get; set; }

    public bool HasLongLivedAccessToken { get; set; }
}

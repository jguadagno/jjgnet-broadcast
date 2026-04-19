namespace JosephGuadagno.Broadcasting.Domain.Models;

public class FacebookPublisherSettings
{
    public string? AppId { get; set; }

    public string? AppSecret { get; set; }

    public string? ClientToken { get; set; }

    public string? LongLivedAccessToken { get; set; }

    public string? PageId { get; set; }

    public string? PageAccessToken { get; set; }

    public string? ShortLivedAccessToken { get; set; }

    public string? GraphApiVersion { get; set; }

    public string? GraphApiRootUrl { get; set; }
}

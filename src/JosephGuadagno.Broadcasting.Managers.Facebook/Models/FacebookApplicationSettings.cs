using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Models;

public class FacebookApplicationSettings: IFacebookApplicationSettings
{
    public string? PageId { get; set; }
    public string? PageAccessToken { get; set; }
    public string? AppId { get; set; }
    public string? AppSecret { get; set; }
    public string? ClientToken { get; set; }
    public string? ShortLivedAccessToken { get; set; }
    public string? LongLivedAccessToken { get; set; }
    public string? GraphApiRootUrl { get; set; }
    public string? GraphApiVersion { get; set; }
}
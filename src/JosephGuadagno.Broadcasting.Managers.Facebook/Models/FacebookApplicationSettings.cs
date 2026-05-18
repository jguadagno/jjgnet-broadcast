using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Models;

public class FacebookApplicationSettings: IFacebookApplicationSettings
{
	public string PageId { get; set; } = string.Empty;
    public string PageAccessToken { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string ClientToken { get; set; } = string.Empty;
    public string ShortLivedAccessToken { get; set; } = string.Empty;
    public string LongLivedAccessToken { get; set; } = string.Empty;
    public string GraphApiRootUrl { get; set; } = "https://graph.facebook.com";
    public string GraphApiVersion { get; set; } = "v20.0";
}
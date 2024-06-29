using JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Models;

public class FacebookApplicationSettings: IFacebookApplicationSettings
{
    public FacebookApplicationSettings()
    {
        PageId = string.Empty;
        PageAccessToken = string.Empty;
        AppId = string.Empty;
        AppSecret = string.Empty;
        ClientToken = string.Empty;
        ShortLivedAccessToken = string.Empty;
        LongLivedAccessToken = string.Empty;
        GraphApiRootUrl = "https://graph.facebook.com";
        GraphApiVersion = "v20.0";
    }
    public string PageId { get; set; }
    public string PageAccessToken { get; set; }
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public string ClientToken { get; set; }
    public string ShortLivedAccessToken { get; set; }
    public string LongLivedAccessToken { get; set; }
    public string GraphApiRootUrl { get; set; }
    public string GraphApiVersion { get; set; }
}
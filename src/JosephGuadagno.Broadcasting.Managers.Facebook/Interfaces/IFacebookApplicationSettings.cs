namespace JosephGuadagno.Broadcasting.Managers.Facebook.Interfaces;

public interface IFacebookApplicationSettings
{
    string? PageId { get; set; }
    string? PageAccessToken { get; set; }
    string? AppId { get; set; }
    string? AppSecret { get; set; }
    string? ClientToken { get; set; }
    string? ShortLivedAccessToken { get; set; }
    string? LongLivedAccessToken { get; set; }
    string? GraphApiRootUrl { get; set; }
    string? GraphApiVersion { get; set; }
}
namespace JosephGuadagno.Broadcasting.Domain.Models;

public class UserOAuthToken
{
    public int Id { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
    public int SocialMediaPlatformId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}

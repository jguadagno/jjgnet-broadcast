namespace JosephGuadagno.Broadcasting.Data.Sql.Models;

/// <summary>
/// EF Core entity representing an OAuth token for a user and social media platform
/// </summary>
public class UserOAuthToken
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the Entra Object ID of the user who owns this token
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the social media platform ID
    /// </summary>
    public int SocialMediaPlatformId { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation property to the social media platform
    /// </summary>
    public SocialMediaPlatform? SocialMediaPlatform { get; set; }
    
    /// <summary>
    /// Gets or sets the OAuth access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the OAuth refresh token if provided by the platform
    /// </summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// Gets or sets when the access token expires
    /// </summary>
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    
    /// <summary>
    /// Gets or sets when the refresh token expires if provided by the platform
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    
    /// <summary>
    /// Gets or sets when this token was created
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }
    
    /// <summary>
    /// Gets or sets when this token was last updated
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}

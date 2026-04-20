namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Response DTO for a user's publisher settings for a platform.
/// </summary>
public class UserPublisherSettingResponse
{
    /// <summary>
    /// Gets or sets the publisher setting identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Entra object ID of the owner who created the settings.
    /// </summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the social media platform identifier.
    /// </summary>
    public int SocialMediaPlatformId { get; set; }

    /// <summary>
    /// Gets or sets the social media platform metadata.
    /// </summary>
    public SocialMediaPlatformResponse? SocialMediaPlatform { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether publishing is enabled for the platform.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the Bluesky-specific settings metadata.
    /// </summary>
    public BlueskyPublisherSettingResponse? Bluesky { get; set; }

    /// <summary>
    /// Gets or sets the Twitter-specific settings metadata.
    /// </summary>
    public TwitterPublisherSettingResponse? Twitter { get; set; }

    /// <summary>
    /// Gets or sets the Facebook-specific settings metadata.
    /// </summary>
    public FacebookPublisherSettingResponse? Facebook { get; set; }

    /// <summary>
    /// Gets or sets the LinkedIn-specific settings metadata.
    /// </summary>
    public LinkedInPublisherSettingResponse? LinkedIn { get; set; }

    /// <summary>
    /// Gets or sets when the publisher settings were created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets when the publisher settings were last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}

/// <summary>
/// Response DTO for Bluesky publisher settings.
/// </summary>
public class BlueskyPublisherSettingResponse
{
    /// <summary>
    /// Gets or sets the Bluesky account user name.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an app password is configured.
    /// </summary>
    public bool HasAppPassword { get; set; }
}

/// <summary>
/// Response DTO for Twitter publisher settings.
/// </summary>
public class TwitterPublisherSettingResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether a consumer key is configured.
    /// </summary>
    public bool HasConsumerKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a consumer secret is configured.
    /// </summary>
    public bool HasConsumerSecret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an access token is configured.
    /// </summary>
    public bool HasAccessToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an access token secret is configured.
    /// </summary>
    public bool HasAccessTokenSecret { get; set; }
}

/// <summary>
/// Response DTO for Facebook publisher settings.
/// </summary>
public class FacebookPublisherSettingResponse
{
    /// <summary>
    /// Gets or sets the Facebook page identifier.
    /// </summary>
    public string? PageId { get; set; }

    /// <summary>
    /// Gets or sets the Facebook application identifier.
    /// </summary>
    public string? AppId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a page access token is configured.
    /// </summary>
    public bool HasPageAccessToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an app secret is configured.
    /// </summary>
    public bool HasAppSecret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a client token is configured.
    /// </summary>
    public bool HasClientToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a short-lived access token is configured.
    /// </summary>
    public bool HasShortLivedAccessToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a long-lived access token is configured.
    /// </summary>
    public bool HasLongLivedAccessToken { get; set; }
}

/// <summary>
/// Response DTO for LinkedIn publisher settings.
/// </summary>
public class LinkedInPublisherSettingResponse
{
    /// <summary>
    /// Gets or sets the LinkedIn author identifier.
    /// </summary>
    public string? AuthorId { get; set; }

    /// <summary>
    /// Gets or sets the LinkedIn client identifier.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a client secret is configured.
    /// </summary>
    public bool HasClientSecret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an access token is configured.
    /// </summary>
    public bool HasAccessToken { get; set; }
}

namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>
/// Request DTO for creating or updating a user's publisher settings for a platform.
/// </summary>
public class UserPublisherSettingRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether publishing is enabled for the platform.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the Bluesky-specific settings.
    /// </summary>
    public BlueskyPublisherSettingRequest? Bluesky { get; set; }

    /// <summary>
    /// Gets or sets the Twitter-specific settings.
    /// </summary>
    public TwitterPublisherSettingRequest? Twitter { get; set; }

    /// <summary>
    /// Gets or sets the Facebook-specific settings.
    /// </summary>
    public FacebookPublisherSettingRequest? Facebook { get; set; }

    /// <summary>
    /// Gets or sets the LinkedIn-specific settings.
    /// </summary>
    public LinkedInPublisherSettingRequest? LinkedIn { get; set; }
}

/// <summary>
/// Request DTO for Bluesky publisher settings.
/// </summary>
public class BlueskyPublisherSettingRequest
{
    /// <summary>
    /// Gets or sets the Bluesky account user name.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the Bluesky app password.
    /// </summary>
    public string? AppPassword { get; set; }
}

/// <summary>
/// Request DTO for Twitter publisher settings.
/// </summary>
public class TwitterPublisherSettingRequest
{
    /// <summary>
    /// Gets or sets the Twitter consumer key.
    /// </summary>
    public string? ConsumerKey { get; set; }

    /// <summary>
    /// Gets or sets the Twitter consumer secret.
    /// </summary>
    public string? ConsumerSecret { get; set; }

    /// <summary>
    /// Gets or sets the Twitter access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the Twitter access token secret.
    /// </summary>
    public string? AccessTokenSecret { get; set; }
}

/// <summary>
/// Request DTO for Facebook publisher settings.
/// </summary>
public class FacebookPublisherSettingRequest
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
    /// Gets or sets the Facebook page access token.
    /// </summary>
    public string? PageAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the Facebook application secret.
    /// </summary>
    public string? AppSecret { get; set; }

    /// <summary>
    /// Gets or sets the Facebook client token.
    /// </summary>
    public string? ClientToken { get; set; }

    /// <summary>
    /// Gets or sets the Facebook short-lived access token.
    /// </summary>
    public string? ShortLivedAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the Facebook long-lived access token.
    /// </summary>
    public string? LongLivedAccessToken { get; set; }
}

/// <summary>
/// Request DTO for LinkedIn publisher settings.
/// </summary>
public class LinkedInPublisherSettingRequest
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
    /// Gets or sets the LinkedIn client secret.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the LinkedIn access token.
    /// </summary>
    public string? AccessToken { get; set; }
}

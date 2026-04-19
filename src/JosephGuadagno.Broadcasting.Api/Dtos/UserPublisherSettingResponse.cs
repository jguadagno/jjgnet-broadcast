namespace JosephGuadagno.Broadcasting.Api.Dtos;

public class UserPublisherSettingResponse
{
    public int Id { get; set; }

    public string CreatedByEntraOid { get; set; } = string.Empty;

    public int SocialMediaPlatformId { get; set; }

    public SocialMediaPlatformResponse? SocialMediaPlatform { get; set; }

    public bool IsEnabled { get; set; }

    public BlueskyPublisherSettingResponse? Bluesky { get; set; }

    public TwitterPublisherSettingResponse? Twitter { get; set; }

    public FacebookPublisherSettingResponse? Facebook { get; set; }

    public LinkedInPublisherSettingResponse? LinkedIn { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public DateTimeOffset LastUpdatedOn { get; set; }
}

public class BlueskyPublisherSettingResponse
{
    public string? UserName { get; set; }

    public bool HasAppPassword { get; set; }
}

public class TwitterPublisherSettingResponse
{
    public bool HasConsumerKey { get; set; }

    public bool HasConsumerSecret { get; set; }

    public bool HasAccessToken { get; set; }

    public bool HasAccessTokenSecret { get; set; }
}

public class FacebookPublisherSettingResponse
{
    public string? PageId { get; set; }

    public string? AppId { get; set; }

    public bool HasPageAccessToken { get; set; }

    public bool HasAppSecret { get; set; }

    public bool HasClientToken { get; set; }

    public bool HasShortLivedAccessToken { get; set; }

    public bool HasLongLivedAccessToken { get; set; }
}

public class LinkedInPublisherSettingResponse
{
    public string? AuthorId { get; set; }

    public string? ClientId { get; set; }

    public bool HasClientSecret { get; set; }

    public bool HasAccessToken { get; set; }
}

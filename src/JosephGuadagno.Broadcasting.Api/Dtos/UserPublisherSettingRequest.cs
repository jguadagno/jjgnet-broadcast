namespace JosephGuadagno.Broadcasting.Api.Dtos;

public class UserPublisherSettingRequest
{
    public bool IsEnabled { get; set; }

    public BlueskyPublisherSettingRequest? Bluesky { get; set; }

    public TwitterPublisherSettingRequest? Twitter { get; set; }

    public FacebookPublisherSettingRequest? Facebook { get; set; }

    public LinkedInPublisherSettingRequest? LinkedIn { get; set; }
}

public class BlueskyPublisherSettingRequest
{
    public string? UserName { get; set; }

    public string? AppPassword { get; set; }
}

public class TwitterPublisherSettingRequest
{
    public string? ConsumerKey { get; set; }

    public string? ConsumerSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? AccessTokenSecret { get; set; }
}

public class FacebookPublisherSettingRequest
{
    public string? PageId { get; set; }

    public string? AppId { get; set; }

    public string? PageAccessToken { get; set; }

    public string? AppSecret { get; set; }

    public string? ClientToken { get; set; }

    public string? ShortLivedAccessToken { get; set; }

    public string? LongLivedAccessToken { get; set; }
}

public class LinkedInPublisherSettingRequest
{
    public string? AuthorId { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? AccessToken { get; set; }
}

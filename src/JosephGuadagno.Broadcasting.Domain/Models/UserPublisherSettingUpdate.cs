namespace JosephGuadagno.Broadcasting.Domain.Models;

public class UserPublisherSettingUpdate
{
    public string CreatedByEntraOid { get; set; } = string.Empty;

    public int SocialMediaPlatformId { get; set; }

    public bool IsEnabled { get; set; }

    public BlueskyPublisherSettingUpdate? Bluesky { get; set; }

    public TwitterPublisherSettingUpdate? Twitter { get; set; }

    public FacebookPublisherSettingUpdate? Facebook { get; set; }

    public LinkedInPublisherSettingUpdate? LinkedIn { get; set; }
}

public class BlueskyPublisherSettingUpdate
{
    public string? UserName { get; set; }

    public string? AppPassword { get; set; }
}

public class TwitterPublisherSettingUpdate
{
    public string? ConsumerKey { get; set; }

    public string? ConsumerSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? AccessTokenSecret { get; set; }
}

public class FacebookPublisherSettingUpdate
{
    public string? PageId { get; set; }

    public string? AppId { get; set; }

    public string? PageAccessToken { get; set; }

    public string? AppSecret { get; set; }

    public string? ClientToken { get; set; }

    public string? ShortLivedAccessToken { get; set; }

    public string? LongLivedAccessToken { get; set; }
}

public class LinkedInPublisherSettingUpdate
{
    public string? AuthorId { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? AccessToken { get; set; }
}

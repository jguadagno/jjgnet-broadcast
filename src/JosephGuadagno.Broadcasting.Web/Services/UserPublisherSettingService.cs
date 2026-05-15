using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using Microsoft.Identity.Abstractions;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Calls out to the user publisher settings API.
/// </summary>
public class UserPublisherSettingService(
    IDownstreamApi apiClient,
    ISocialMediaPlatformService socialMediaPlatformService,
    ILogger<UserPublisherSettingService> logger) : IUserPublisherSettingService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string PublishersBaseUrl = "/Publishers";
    private const string MaskedSecretValue = "••••••••";

    public Task<List<UserPublisherSetting>> GetCurrentUserAsync()
    {
        return GetSettingsAsync(null);
    }

    public Task<List<UserPublisherSetting>> GetByUserAsync(string ownerOid)
    {
        return GetSettingsAsync(ownerOid);
    }

    public Task<UserPublisherSetting?> SaveCurrentUserAsync(UserPublisherSetting setting)
    {
        return SaveAsync(null, setting);
    }

    public Task<UserPublisherSetting?> SaveByUserAsync(string ownerOid, UserPublisherSetting setting)
    {
        return SaveAsync(ownerOid, setting);
    }

    private async Task<List<UserPublisherSetting>> GetSettingsAsync(string? ownerOid)
    {
        var relativePath = BuildBasePath(ownerOid);

        var aggregateTask = apiClient.GetForUserAsync<PublishersAggregateApiResponse>(ApiServiceName, options =>
        {
            options.RelativePath = relativePath;
        });
        var platformsTask = socialMediaPlatformService.GetAllAsync(pageSize: Pagination.MaxPageSize, includeInactive: true);

        await Task.WhenAll(aggregateTask, platformsTask);

        var aggregate = aggregateTask.Result;
        if (aggregate is null)
        {
            return [];
        }

        var platforms = platformsTask.Result?.Items ?? [];
        var results = new List<UserPublisherSetting>(4);

        if (aggregate.Bluesky is not null)
        {
            var platform = FindPlatform(platforms, "bluesky");
            results.Add(MapBlueskyResponse(aggregate.Bluesky, platform));
        }

        if (aggregate.Twitter is not null)
        {
            var platform = FindPlatform(platforms, "twitter");
            results.Add(MapTwitterResponse(aggregate.Twitter, platform));
        }

        if (aggregate.LinkedIn is not null)
        {
            var platform = FindPlatform(platforms, "linkedin");
            results.Add(MapLinkedInResponse(aggregate.LinkedIn, platform));
        }

        if (aggregate.Facebook is not null)
        {
            var platform = FindPlatform(platforms, "facebook");
            results.Add(MapFacebookResponse(aggregate.Facebook, platform));
        }

        return results;
    }

    private async Task<UserPublisherSetting?> SaveAsync(string? ownerOid, UserPublisherSetting setting)
    {
        var platform = NormalizePlatformName(setting.SocialMediaPlatformName ?? setting.SocialMediaPlatform?.Name);
        if (string.IsNullOrEmpty(platform))
        {
            logger.LogWarning("Cannot save publisher setting: platform name is missing for owner '{OwnerOid}'",
                setting.CreatedByEntraOid);
            return null;
        }

        var platformPath = CapitalizePlatformPath(platform);
        var relativePath = BuildPlatformPath(platformPath, ownerOid);

        switch (platform)
        {
            case "bluesky":
            {
                var request = BuildBlueskyRequest(setting);
                var response = await apiClient.PutForUserAsync<BlueskySettingsApiRequest, BlueskySettingsApiResponse>(
                    ApiServiceName, request, options => { options.RelativePath = relativePath; });
                if (response is null)
                {
                    LogSaveFailure(setting);
                    return null;
                }

                return MapBlueskyResponse(response, setting.SocialMediaPlatform, setting.SocialMediaPlatformId);
            }
            case "twitter":
            {
                var request = BuildTwitterRequest(setting);
                var response = await apiClient.PutForUserAsync<TwitterSettingsApiRequest, TwitterSettingsApiResponse>(
                    ApiServiceName, request, options => { options.RelativePath = relativePath; });
                if (response is null)
                {
                    LogSaveFailure(setting);
                    return null;
                }

                return MapTwitterResponse(response, setting.SocialMediaPlatform, setting.SocialMediaPlatformId);
            }
            case "linkedin":
            {
                var request = BuildLinkedInRequest(setting);
                var response = await apiClient.PutForUserAsync<LinkedInSettingsApiRequest, LinkedInSettingsApiResponse>(
                    ApiServiceName, request, options => { options.RelativePath = relativePath; });
                if (response is null)
                {
                    LogSaveFailure(setting);
                    return null;
                }

                return MapLinkedInResponse(response, setting.SocialMediaPlatform, setting.SocialMediaPlatformId);
            }
            case "facebook":
            {
                var request = BuildFacebookRequest(setting);
                var response = await apiClient.PutForUserAsync<FacebookSettingsApiRequest, FacebookSettingsApiResponse>(
                    ApiServiceName, request, options => { options.RelativePath = relativePath; });
                if (response is null)
                {
                    LogSaveFailure(setting);
                    return null;
                }

                return MapFacebookResponse(response, setting.SocialMediaPlatform, setting.SocialMediaPlatformId);
            }
            default:
                logger.LogWarning("Unrecognized platform '{Platform}' for owner '{OwnerOid}'",
                    platform, setting.CreatedByEntraOid);
                return null;
        }
    }

    private void LogSaveFailure(UserPublisherSetting setting)
    {
        logger.LogWarning(
            "Publisher settings save returned no content for owner '{OwnerOid}' and platform '{PlatformName}'",
            setting.CreatedByEntraOid,
            setting.SocialMediaPlatformName ?? setting.SocialMediaPlatform?.Name);
    }

    private static string BuildBasePath(string? ownerOid)
    {
        return string.IsNullOrWhiteSpace(ownerOid)
            ? PublishersBaseUrl
            : $"{PublishersBaseUrl}?ownerOid={Uri.EscapeDataString(ownerOid)}";
    }

    private static string BuildPlatformPath(string platformPath, string? ownerOid)
    {
        var path = $"{PublishersBaseUrl}/{platformPath}";
        return string.IsNullOrWhiteSpace(ownerOid)
            ? path
            : $"{path}?ownerOid={Uri.EscapeDataString(ownerOid)}";
    }

    private static string CapitalizePlatformPath(string normalizedPlatform) => normalizedPlatform switch
    {
        "bluesky" => "Bluesky",
        "twitter" => "Twitter",
        "linkedin" => "LinkedIn",
        "facebook" => "Facebook",
        _ => normalizedPlatform
    };

    private static SocialMediaPlatform? FindPlatform(IEnumerable<SocialMediaPlatform> platforms, string normalizedName)
    {
        return platforms.FirstOrDefault(p => NormalizePlatformName(p.Name) == normalizedName);
    }

    // Request builders

    private static BlueskySettingsApiRequest BuildBlueskyRequest(UserPublisherSetting setting) => new()
    {
        IsEnabled = setting.IsEnabled,
        UserName = GetSettingValue(setting.Settings, nameof(BlueskyPublisherSettings.BlueskyUserName)),
        AppPassword = GetSettingValue(setting.Settings, nameof(BlueskyPublisherSettings.BlueskyPassword))
    };

    private static TwitterSettingsApiRequest BuildTwitterRequest(UserPublisherSetting setting) => new()
    {
        IsEnabled = setting.IsEnabled,
        ConsumerKey = GetSettingValue(setting.Settings, nameof(TwitterPublisherSettings.ConsumerKey)),
        ConsumerSecret = GetSettingValue(setting.Settings, nameof(TwitterPublisherSettings.ConsumerSecret)),
        AccessToken = GetSettingValue(setting.Settings, nameof(TwitterPublisherSettings.OAuthToken)),
        AccessTokenSecret = GetSettingValue(setting.Settings, nameof(TwitterPublisherSettings.OAuthTokenSecret))
    };

    private static LinkedInSettingsApiRequest BuildLinkedInRequest(UserPublisherSetting setting) => new()
    {
        IsEnabled = setting.IsEnabled,
        AuthorId = GetSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.AuthorId)),
        ClientId = GetSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.ClientId)),
        ClientSecret = GetSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.ClientSecret)),
        AccessToken = GetSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.AccessToken))
    };

    private static FacebookSettingsApiRequest BuildFacebookRequest(UserPublisherSetting setting) => new()
    {
        IsEnabled = setting.IsEnabled,
        PageId = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.PageId)),
        AppId = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.AppId)),
        PageAccessToken = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.PageAccessToken)),
        AppSecret = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.AppSecret)),
        ClientToken = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.ClientToken)),
        ShortLivedAccessToken = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.ShortLivedAccessToken)),
        LongLivedAccessToken = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.LongLivedAccessToken))
    };

    // Response mappers — platform overloads for GET (lookup from ISocialMediaPlatformService)

    private static UserPublisherSetting MapBlueskyResponse(BlueskySettingsApiResponse r, SocialMediaPlatform? platform)
        => MapBlueskyResponse(r, platform, platform?.Id ?? 0);

    private static UserPublisherSetting MapBlueskyResponse(
        BlueskySettingsApiResponse r, SocialMediaPlatform? platform, int platformId)
    {
        var setting = BuildBaseSetting(r.Id, r.CreatedByEntraOid, platformId, platform, r.IsEnabled, r.CreatedOn, r.LastUpdatedOn);

        setting.Bluesky = new BlueskyPublisherSetting
        {
            UserName = r.UserName,
            HasAppPassword = r.HasAppPassword
        };

        AddSettingValue(setting.Settings, nameof(BlueskyPublisherSettings.BlueskyUserName), r.UserName);
        AddWriteOnlyValue(setting, nameof(BlueskyPublisherSettings.BlueskyPassword), r.HasAppPassword);

        return setting;
    }

    private static UserPublisherSetting MapTwitterResponse(TwitterSettingsApiResponse r, SocialMediaPlatform? platform)
        => MapTwitterResponse(r, platform, platform?.Id ?? 0);

    private static UserPublisherSetting MapTwitterResponse(
        TwitterSettingsApiResponse r, SocialMediaPlatform? platform, int platformId)
    {
        var setting = BuildBaseSetting(r.Id, r.CreatedByEntraOid, platformId, platform, r.IsEnabled, r.CreatedOn, r.LastUpdatedOn);

        setting.Twitter = new TwitterPublisherSetting
        {
            HasConsumerKey = r.HasConsumerKey,
            HasConsumerSecret = r.HasConsumerSecret,
            HasAccessToken = r.HasAccessToken,
            HasAccessTokenSecret = r.HasAccessTokenSecret
        };

        AddWriteOnlyValue(setting, nameof(TwitterPublisherSettings.ConsumerKey), r.HasConsumerKey);
        AddWriteOnlyValue(setting, nameof(TwitterPublisherSettings.ConsumerSecret), r.HasConsumerSecret);
        AddWriteOnlyValue(setting, nameof(TwitterPublisherSettings.OAuthToken), r.HasAccessToken);
        AddWriteOnlyValue(setting, nameof(TwitterPublisherSettings.OAuthTokenSecret), r.HasAccessTokenSecret);

        return setting;
    }

    private static UserPublisherSetting MapLinkedInResponse(LinkedInSettingsApiResponse r, SocialMediaPlatform? platform)
        => MapLinkedInResponse(r, platform, platform?.Id ?? 0);

    private static UserPublisherSetting MapLinkedInResponse(
        LinkedInSettingsApiResponse r, SocialMediaPlatform? platform, int platformId)
    {
        var setting = BuildBaseSetting(r.Id, r.CreatedByEntraOid, platformId, platform, r.IsEnabled, r.CreatedOn, r.LastUpdatedOn);

        setting.LinkedIn = new LinkedInPublisherSetting
        {
            AuthorId = r.AuthorId,
            ClientId = r.ClientId,
            HasClientSecret = r.HasClientSecret,
            HasAccessToken = r.HasAccessToken
        };

        AddSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.AuthorId), r.AuthorId);
        AddSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.ClientId), r.ClientId);
        AddWriteOnlyValue(setting, nameof(LinkedInPublisherSettings.ClientSecret), r.HasClientSecret);
        AddWriteOnlyValue(setting, nameof(LinkedInPublisherSettings.AccessToken), r.HasAccessToken);

        return setting;
    }

    private static UserPublisherSetting MapFacebookResponse(FacebookSettingsApiResponse r, SocialMediaPlatform? platform)
        => MapFacebookResponse(r, platform, platform?.Id ?? 0);

    private static UserPublisherSetting MapFacebookResponse(
        FacebookSettingsApiResponse r, SocialMediaPlatform? platform, int platformId)
    {
        var setting = BuildBaseSetting(r.Id, r.CreatedByEntraOid, platformId, platform, r.IsEnabled, r.CreatedOn, r.LastUpdatedOn);

        setting.Facebook = new FacebookPublisherSetting
        {
            PageId = r.PageId,
            AppId = r.AppId,
            HasPageAccessToken = r.HasPageAccessToken,
            HasAppSecret = r.HasAppSecret,
            HasClientToken = r.HasClientToken,
            HasShortLivedAccessToken = r.HasShortLivedAccessToken,
            HasLongLivedAccessToken = r.HasLongLivedAccessToken
        };

        AddSettingValue(setting.Settings, nameof(FacebookPublisherSettings.PageId), r.PageId);
        AddSettingValue(setting.Settings, nameof(FacebookPublisherSettings.AppId), r.AppId);
        AddWriteOnlyValue(setting, nameof(FacebookPublisherSettings.PageAccessToken), r.HasPageAccessToken);
        AddWriteOnlyValue(setting, nameof(FacebookPublisherSettings.AppSecret), r.HasAppSecret);
        AddWriteOnlyValue(setting, nameof(FacebookPublisherSettings.ClientToken), r.HasClientToken);
        AddWriteOnlyValue(setting, nameof(FacebookPublisherSettings.ShortLivedAccessToken), r.HasShortLivedAccessToken);
        AddWriteOnlyValue(setting, nameof(FacebookPublisherSettings.LongLivedAccessToken), r.HasLongLivedAccessToken);

        return setting;
    }

    private static UserPublisherSetting BuildBaseSetting(
        int id,
        string createdByEntraOid,
        int socialMediaPlatformId,
        SocialMediaPlatform? platform,
        bool isEnabled,
        DateTimeOffset createdOn,
        DateTimeOffset lastUpdatedOn) => new()
    {
        Id = id,
        CreatedByEntraOid = createdByEntraOid,
        SocialMediaPlatformId = socialMediaPlatformId,
        SocialMediaPlatformName = platform?.Name,
        SocialMediaPlatform = platform,
        IsEnabled = isEnabled,
        CreatedOn = createdOn,
        LastUpdatedOn = lastUpdatedOn
    };

    private static void AddSettingValue(IDictionary<string, string?> settings, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            settings[key] = value;
        }
    }

    private static void AddWriteOnlyValue(UserPublisherSetting setting, string key, bool hasValue)
    {
        if (!hasValue)
        {
            return;
        }

        setting.Settings[key] = MaskedSecretValue;
        setting.WriteOnlyFields.Add(key);
    }

    private static string? GetSettingValue(IReadOnlyDictionary<string, string?> settings, string key)
    {
        return settings.TryGetValue(key, out var directValue)
            ? directValue
            : settings.FirstOrDefault(pair => pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static string NormalizePlatformName(string? platformName)
    {
        return platformName
            ?.Replace("/", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant() switch
        {
            null => string.Empty,
            var value when value.StartsWith("twitter", StringComparison.Ordinal) => "twitter",
            var value => value
        };
    }
}

// Local API mirror types — Web project does not reference the Api project

public sealed class PublishersAggregateApiResponse
{
    public BlueskySettingsApiResponse? Bluesky { get; set; }
    public TwitterSettingsApiResponse? Twitter { get; set; }
    public LinkedInSettingsApiResponse? LinkedIn { get; set; }
    public FacebookSettingsApiResponse? Facebook { get; set; }
}

public sealed class BlueskySettingsApiRequest
{
    public bool IsEnabled { get; set; }
    public string? UserName { get; set; }
    public string? AppPassword { get; set; }
}

public sealed class BlueskySettingsApiResponse
{
    public int Id { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? UserName { get; set; }
    public bool HasAppPassword { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}

public sealed class TwitterSettingsApiRequest
{
    public bool IsEnabled { get; set; }
    public string? ConsumerKey { get; set; }
    public string? ConsumerSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? AccessTokenSecret { get; set; }
}

public sealed class TwitterSettingsApiResponse
{
    public int Id { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool HasConsumerKey { get; set; }
    public bool HasConsumerSecret { get; set; }
    public bool HasAccessToken { get; set; }
    public bool HasAccessTokenSecret { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}

public sealed class LinkedInSettingsApiRequest
{
    public bool IsEnabled { get; set; }
    public string? AuthorId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AccessToken { get; set; }
}

public sealed class LinkedInSettingsApiResponse
{
    public int Id { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? AuthorId { get; set; }
    public string? ClientId { get; set; }
    public bool HasClientSecret { get; set; }
    public bool HasAccessToken { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}

public sealed class FacebookSettingsApiRequest
{
    public bool IsEnabled { get; set; }
    public string? PageId { get; set; }
    public string? AppId { get; set; }
    public string? PageAccessToken { get; set; }
    public string? AppSecret { get; set; }
    public string? ClientToken { get; set; }
    public string? ShortLivedAccessToken { get; set; }
    public string? LongLivedAccessToken { get; set; }
}

public sealed class FacebookSettingsApiResponse
{
    public int Id { get; set; }
    public string CreatedByEntraOid { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? PageId { get; set; }
    public string? AppId { get; set; }
    public bool HasPageAccessToken { get; set; }
    public bool HasAppSecret { get; set; }
    public bool HasClientToken { get; set; }
    public bool HasShortLivedAccessToken { get; set; }
    public bool HasLongLivedAccessToken { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
}




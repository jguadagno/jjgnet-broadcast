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
    ILogger<UserPublisherSettingService> logger) : IUserPublisherSettingService
{
    private const string ApiServiceName = "JosephGuadagnoBroadcastingApi";
    private const string PublisherSettingsBaseUrl = "/UserPublisherSettings";
    private const string MaskedSecretValue = "••••••••";

    public async Task<List<UserPublisherSetting>> GetCurrentUserAsync()
    {
        var response = await apiClient.GetForUserAsync<PagedResponse<UserPublisherSettingApiResponse>>(ApiServiceName, options =>
        {
            options.RelativePath = $"{PublisherSettingsBaseUrl}?pageSize={Pagination.MaxPageSize}";
        });

        return response?.Items.Select(MapResponse).ToList() ?? [];
    }

    public async Task<List<UserPublisherSetting>> GetByUserAsync(string ownerOid)
    {
        var response = await apiClient.GetForUserAsync<PagedResponse<UserPublisherSettingApiResponse>>(ApiServiceName, options =>
        {
            options.RelativePath = BuildRelativePath(ownerOid) + $"&pageSize={Pagination.MaxPageSize}";
        });

        return response?.Items.Select(MapResponse).ToList() ?? [];
    }

    public Task<UserPublisherSetting?> SaveCurrentUserAsync(UserPublisherSetting setting)
    {
        return SaveAsync(BuildRelativePath(platformId: setting.SocialMediaPlatformId), setting);
    }

    public Task<UserPublisherSetting?> SaveByUserAsync(string ownerOid, UserPublisherSetting setting)
    {
        return SaveAsync(BuildRelativePath(ownerOid, setting.SocialMediaPlatformId), setting);
    }

    private async Task<UserPublisherSetting?> SaveAsync(string relativePath, UserPublisherSetting setting)
    {
        var savedSetting = await apiClient.PutForUserAsync<UserPublisherSettingApiRequest, UserPublisherSettingApiResponse>(
            ApiServiceName,
            MapRequest(setting),
            options =>
            {
                options.RelativePath = relativePath;
            });

        if (savedSetting is null)
        {
            logger.LogWarning(
                "Publisher settings save returned no content for owner {OwnerOid} and platform {PlatformId}",
                setting.CreatedByEntraOid,
                setting.SocialMediaPlatformId);
        }

        return savedSetting is null ? null : MapResponse(savedSetting);
    }

    private static string BuildRelativePath(string? ownerOid = null, int? platformId = null)
    {
        var relativePath = platformId.HasValue
            ? $"{PublisherSettingsBaseUrl}/{platformId.Value}"
            : PublisherSettingsBaseUrl;

        return string.IsNullOrWhiteSpace(ownerOid)
            ? relativePath
            : $"{relativePath}?ownerOid={Uri.EscapeDataString(ownerOid)}";
    }

    private static UserPublisherSettingApiRequest MapRequest(UserPublisherSetting setting)
    {
        var request = new UserPublisherSettingApiRequest
        {
            IsEnabled = setting.IsEnabled
        };

        switch (NormalizePlatformName(setting.SocialMediaPlatformName ?? setting.SocialMediaPlatform?.Name))
        {
            case "bluesky":
                request.Bluesky = new BlueskyPublisherSettingApiRequest
                {
                    UserName = GetSettingValue(setting.Settings, nameof(BlueskyPublisherSettings.BlueskyUserName)),
                    AppPassword = GetSettingValue(setting.Settings, nameof(BlueskyPublisherSettings.BlueskyPassword))
                };
                break;
            case "twitter":
                request.Twitter = new TwitterPublisherSettingApiRequest
                {
                    ConsumerKey = GetSettingValue(setting.Settings, nameof(TwitterPublisherSettings.ConsumerKey)),
                    ConsumerSecret = GetSettingValue(setting.Settings, nameof(TwitterPublisherSettings.ConsumerSecret)),
                    AccessToken = GetSettingValue(setting.Settings, nameof(TwitterPublisherSettings.OAuthToken)),
                    AccessTokenSecret = GetSettingValue(setting.Settings, nameof(TwitterPublisherSettings.OAuthTokenSecret))
                };
                break;
            case "facebook":
                request.Facebook = new FacebookPublisherSettingApiRequest
                {
                    PageId = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.PageId)),
                    AppId = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.AppId)),
                    PageAccessToken = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.PageAccessToken)),
                    AppSecret = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.AppSecret)),
                    ClientToken = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.ClientToken)),
                    ShortLivedAccessToken = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.ShortLivedAccessToken)),
                    LongLivedAccessToken = GetSettingValue(setting.Settings, nameof(FacebookPublisherSettings.LongLivedAccessToken))
                };
                break;
            case "linkedin":
                request.LinkedIn = new LinkedInPublisherSettingApiRequest
                {
                    AuthorId = GetSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.AuthorId)),
                    ClientId = GetSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.ClientId)),
                    ClientSecret = GetSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.ClientSecret)),
                    AccessToken = GetSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.AccessToken))
                };
                break;
        }

        return request;
    }

    private static UserPublisherSetting MapResponse(UserPublisherSettingApiResponse response)
    {
        var setting = new UserPublisherSetting
        {
            Id = response.Id,
            CreatedByEntraOid = response.CreatedByEntraOid,
            SocialMediaPlatformId = response.SocialMediaPlatformId,
            SocialMediaPlatformName = response.SocialMediaPlatform?.Name,
            SocialMediaPlatform = response.SocialMediaPlatform is null
                ? null
                : new SocialMediaPlatform
                {
                    Id = response.SocialMediaPlatform.Id,
                    Name = response.SocialMediaPlatform.Name,
                    Url = response.SocialMediaPlatform.Url,
                    Icon = response.SocialMediaPlatform.Icon,
                    IsActive = response.SocialMediaPlatform.IsActive
                },
            IsEnabled = response.IsEnabled,
            CreatedOn = response.CreatedOn,
            LastUpdatedOn = response.LastUpdatedOn
        };

        if (response.Bluesky is not null)
        {
            setting.Bluesky = new BlueskyPublisherSetting
            {
                UserName = response.Bluesky.UserName,
                HasAppPassword = response.Bluesky.HasAppPassword
            };

            AddSettingValue(setting.Settings, nameof(BlueskyPublisherSettings.BlueskyUserName), response.Bluesky.UserName);
            AddWriteOnlyValue(setting, nameof(BlueskyPublisherSettings.BlueskyPassword), response.Bluesky.HasAppPassword);
        }

        if (response.Twitter is not null)
        {
            setting.Twitter = new TwitterPublisherSetting
            {
                HasConsumerKey = response.Twitter.HasConsumerKey,
                HasConsumerSecret = response.Twitter.HasConsumerSecret,
                HasAccessToken = response.Twitter.HasAccessToken,
                HasAccessTokenSecret = response.Twitter.HasAccessTokenSecret
            };

            AddWriteOnlyValue(setting, nameof(TwitterPublisherSettings.ConsumerKey), response.Twitter.HasConsumerKey);
            AddWriteOnlyValue(setting, nameof(TwitterPublisherSettings.ConsumerSecret), response.Twitter.HasConsumerSecret);
            AddWriteOnlyValue(setting, nameof(TwitterPublisherSettings.OAuthToken), response.Twitter.HasAccessToken);
            AddWriteOnlyValue(setting, nameof(TwitterPublisherSettings.OAuthTokenSecret), response.Twitter.HasAccessTokenSecret);
        }

        if (response.Facebook is not null)
        {
            setting.Facebook = new FacebookPublisherSetting
            {
                PageId = response.Facebook.PageId,
                AppId = response.Facebook.AppId,
                HasPageAccessToken = response.Facebook.HasPageAccessToken,
                HasAppSecret = response.Facebook.HasAppSecret,
                HasClientToken = response.Facebook.HasClientToken,
                HasShortLivedAccessToken = response.Facebook.HasShortLivedAccessToken,
                HasLongLivedAccessToken = response.Facebook.HasLongLivedAccessToken
            };

            AddSettingValue(setting.Settings, nameof(FacebookPublisherSettings.PageId), response.Facebook.PageId);
            AddSettingValue(setting.Settings, nameof(FacebookPublisherSettings.AppId), response.Facebook.AppId);
            AddWriteOnlyValue(setting, nameof(FacebookPublisherSettings.PageAccessToken), response.Facebook.HasPageAccessToken);
            AddWriteOnlyValue(setting, nameof(FacebookPublisherSettings.AppSecret), response.Facebook.HasAppSecret);
            AddWriteOnlyValue(setting, nameof(FacebookPublisherSettings.ClientToken), response.Facebook.HasClientToken);
            AddWriteOnlyValue(setting, nameof(FacebookPublisherSettings.ShortLivedAccessToken), response.Facebook.HasShortLivedAccessToken);
            AddWriteOnlyValue(setting, nameof(FacebookPublisherSettings.LongLivedAccessToken), response.Facebook.HasLongLivedAccessToken);
        }

        if (response.LinkedIn is not null)
        {
            setting.LinkedIn = new LinkedInPublisherSetting
            {
                AuthorId = response.LinkedIn.AuthorId,
                ClientId = response.LinkedIn.ClientId,
                HasClientSecret = response.LinkedIn.HasClientSecret,
                HasAccessToken = response.LinkedIn.HasAccessToken
            };

            AddSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.AuthorId), response.LinkedIn.AuthorId);
            AddSettingValue(setting.Settings, nameof(LinkedInPublisherSettings.ClientId), response.LinkedIn.ClientId);
            AddWriteOnlyValue(setting, nameof(LinkedInPublisherSettings.ClientSecret), response.LinkedIn.HasClientSecret);
            AddWriteOnlyValue(setting, nameof(LinkedInPublisherSettings.AccessToken), response.LinkedIn.HasAccessToken);
        }

        return setting;
    }

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

public sealed class UserPublisherSettingApiRequest
{
    public bool IsEnabled { get; set; }

    public BlueskyPublisherSettingApiRequest? Bluesky { get; set; }

    public TwitterPublisherSettingApiRequest? Twitter { get; set; }

    public FacebookPublisherSettingApiRequest? Facebook { get; set; }

    public LinkedInPublisherSettingApiRequest? LinkedIn { get; set; }
}

public sealed class BlueskyPublisherSettingApiRequest
{
    public string? UserName { get; set; }

    public string? AppPassword { get; set; }
}

public sealed class TwitterPublisherSettingApiRequest
{
    public string? ConsumerKey { get; set; }

    public string? ConsumerSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? AccessTokenSecret { get; set; }
}

public sealed class FacebookPublisherSettingApiRequest
{
    public string? PageId { get; set; }

    public string? AppId { get; set; }

    public string? PageAccessToken { get; set; }

    public string? AppSecret { get; set; }

    public string? ClientToken { get; set; }

    public string? ShortLivedAccessToken { get; set; }

    public string? LongLivedAccessToken { get; set; }
}

public sealed class LinkedInPublisherSettingApiRequest
{
    public string? AuthorId { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? AccessToken { get; set; }
}

public sealed class UserPublisherSettingApiResponse
{
    public int Id { get; set; }

    public string CreatedByEntraOid { get; set; } = string.Empty;

    public int SocialMediaPlatformId { get; set; }

    public UserPublisherSettingSocialMediaPlatformApiResponse? SocialMediaPlatform { get; set; }

    public bool IsEnabled { get; set; }

    public BlueskyPublisherSettingApiResponse? Bluesky { get; set; }

    public TwitterPublisherSettingApiResponse? Twitter { get; set; }

    public FacebookPublisherSettingApiResponse? Facebook { get; set; }

    public LinkedInPublisherSettingApiResponse? LinkedIn { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public DateTimeOffset LastUpdatedOn { get; set; }
}

public sealed class BlueskyPublisherSettingApiResponse
{
    public string? UserName { get; set; }

    public bool HasAppPassword { get; set; }
}

public sealed class TwitterPublisherSettingApiResponse
{
    public bool HasConsumerKey { get; set; }

    public bool HasConsumerSecret { get; set; }

    public bool HasAccessToken { get; set; }

    public bool HasAccessTokenSecret { get; set; }
}

public sealed class FacebookPublisherSettingApiResponse
{
    public string? PageId { get; set; }

    public string? AppId { get; set; }

    public bool HasPageAccessToken { get; set; }

    public bool HasAppSecret { get; set; }

    public bool HasClientToken { get; set; }

    public bool HasShortLivedAccessToken { get; set; }

    public bool HasLongLivedAccessToken { get; set; }
}

public sealed class LinkedInPublisherSettingApiResponse
{
    public string? AuthorId { get; set; }

    public string? ClientId { get; set; }

    public bool HasClientSecret { get; set; }

    public bool HasAccessToken { get; set; }
}

public sealed class UserPublisherSettingSocialMediaPlatformApiResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? Icon { get; set; }

    public bool IsActive { get; set; }
}

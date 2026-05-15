using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

public class UserPublisherSettingManager : IUserPublisherSettingManager
{
    private readonly IUserPublisherSettingDataStore _userPublisherSettingDataStore;
    private readonly ISocialMediaPlatformManager _socialMediaPlatformManager;
    private readonly IKeyVault _keyVault;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserPublisherSettingManager> _logger;

    private static string CacheKeyAllByOwner(string ownerOid) => $"UserPublisherSettings_All_{ownerOid}";

    private static readonly MemoryCacheEntryOptions CacheOptions =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    public UserPublisherSettingManager(
        IUserPublisherSettingDataStore userPublisherSettingDataStore,
        ISocialMediaPlatformManager socialMediaPlatformManager,
        IKeyVault keyVault,
        IMemoryCache cache,
        ILogger<UserPublisherSettingManager> logger)
    {
        _userPublisherSettingDataStore = userPublisherSettingDataStore;
        _socialMediaPlatformManager = socialMediaPlatformManager;
        _keyVault = keyVault;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<UserPublisherSetting>> GetByUserAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyAllByOwner(ownerOid);
        if (_cache.TryGetValue(cacheKey, out List<UserPublisherSetting>? cached) && cached is not null)
        {
            return cached;
        }

        var settings = await _userPublisherSettingDataStore.GetByUserAsync(ownerOid, cancellationToken);
        var result = settings.Select(ProjectForResponse).ToList();
        _cache.Set(cacheKey, result, CacheOptions);
        return result;
    }

    public async Task<UserPublisherSetting?> GetByUserAndPlatformAsync(string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        var setting = await _userPublisherSettingDataStore.GetByUserAndPlatformAsync(ownerOid, platformId, cancellationToken);
        return setting is null ? null : ProjectForResponse(setting);
    }

    public async Task<UserPublisherSetting?> SaveAsync(UserPublisherSettingUpdate setting, CancellationToken cancellationToken = default)
    {
        var platform = await _socialMediaPlatformManager.GetByIdAsync(setting.SocialMediaPlatformId, cancellationToken);
        if (platform is null)
        {
            return null;
        }

        var existing = await _userPublisherSettingDataStore.GetByUserAndPlatformAsync(
            setting.CreatedByEntraOid,
            setting.SocialMediaPlatformId,
            cancellationToken);

        var persisted = await _userPublisherSettingDataStore.SaveAsync(
            new UserPublisherSetting
            {
                Id = existing?.Id ?? 0,
                CreatedByEntraOid = setting.CreatedByEntraOid,
                SocialMediaPlatformId = setting.SocialMediaPlatformId,
                SocialMediaPlatform = platform,
                IsEnabled = setting.IsEnabled,
                Settings = BuildSettings(platform.Name, setting, existing?.Settings),
                CreatedOn = existing?.CreatedOn ?? default,
                LastUpdatedOn = existing?.LastUpdatedOn ?? default
            },
            cancellationToken);

        InvalidateUserCaches(setting.CreatedByEntraOid);
        return persisted is null ? null : ProjectForResponse(persisted);
    }

    public Task<bool> DeleteAsync(string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        InvalidateUserCaches(ownerOid);
        return _userPublisherSettingDataStore.DeleteAsync(ownerOid, platformId, cancellationToken);
    }

    public async Task<PagedResult<UserPublisherSetting>> GetAllAsync(string ownerOid, int page, int pageSize, string sortBy = "platformname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        var pagedResult = await _userPublisherSettingDataStore.GetAllAsync(ownerOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
        return new PagedResult<UserPublisherSetting>
        {
            Items = pagedResult.Items.Select(ProjectForResponse).ToList(),
            TotalCount = pagedResult.TotalCount
        };
    }

    public async Task<Dictionary<string, string?>> GetCredentialsAsync(string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        var setting = await _userPublisherSettingDataStore.GetByUserAndPlatformAsync(ownerOid, platformId, cancellationToken);
        if (setting is null || setting.Settings.Count == 0)
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // KV path: Settings contains only { "SecretName": "publisher-{platform}-{oid}" }
        if (setting.Settings.TryGetValue("SecretName", out var secretName) && !string.IsNullOrWhiteSpace(secretName))
        {
            try
            {
                var kvSecret = await _keyVault.GetSecretAsync(secretName);
                var json = kvSecret.Value;
                return JsonSerializer.Deserialize<Dictionary<string, string?>>(json, new JsonSerializerOptions
                    { PropertyNameCaseInsensitive = true })
                    ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve credentials from Key Vault for secret '{SecretName}', owner '{OwnerOid}', platform {PlatformId}",
                    secretName, LogSanitizer.Sanitize(ownerOid), platformId);
                return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            }
        }

        // Fallback: raw credentials stored directly in Settings
        return setting.Settings;
    }

    private void InvalidateUserCaches(string? ownerOid)
    {
        if (!string.IsNullOrEmpty(ownerOid))
        {
            _cache.Remove(CacheKeyAllByOwner(ownerOid));
        }
    }

    private static UserPublisherSetting ProjectForResponse(UserPublisherSetting setting)
    {
        var projected = new UserPublisherSetting
        {
            Id = setting.Id,
            CreatedByEntraOid = setting.CreatedByEntraOid,
            SocialMediaPlatformId = setting.SocialMediaPlatformId,
            SocialMediaPlatform = setting.SocialMediaPlatform,
            IsEnabled = setting.IsEnabled,
            CreatedOn = setting.CreatedOn,
            LastUpdatedOn = setting.LastUpdatedOn
        };

        switch (NormalizePlatformName(setting.SocialMediaPlatform?.Name))
        {
            case "bluesky":
                projected.Bluesky = new BlueskyPublisherSetting
                {
                    UserName = GetValue(setting.Settings, "BlueskyUserName"),
                    HasAppPassword = HasValue(setting.Settings, "BlueskyPassword")
                };
                break;
            case "twitter":
                projected.Twitter = new TwitterPublisherSetting
                {
                    HasConsumerKey = HasValue(setting.Settings, "ConsumerKey"),
                    HasConsumerSecret = HasValue(setting.Settings, "ConsumerSecret"),
                    HasAccessToken = HasValue(setting.Settings, "OAuthToken"),
                    HasAccessTokenSecret = HasValue(setting.Settings, "OAuthTokenSecret")
                };
                break;
            case "facebook":
                projected.Facebook = new FacebookPublisherSetting
                {
                    PageId = GetValue(setting.Settings, "PageId"),
                    AppId = GetValue(setting.Settings, "AppId"),
                    HasPageAccessToken = HasValue(setting.Settings, "PageAccessToken"),
                    HasAppSecret = HasValue(setting.Settings, "AppSecret"),
                    HasClientToken = HasValue(setting.Settings, "ClientToken"),
                    HasShortLivedAccessToken = HasValue(setting.Settings, "ShortLivedAccessToken"),
                    HasLongLivedAccessToken = HasValue(setting.Settings, "LongLivedAccessToken")
                };
                break;
            case "linkedin":
                projected.LinkedIn = new LinkedInPublisherSetting
                {
                    AuthorId = GetValue(setting.Settings, "AuthorId"),
                    ClientId = GetValue(setting.Settings, "ClientId"),
                    HasClientSecret = HasValue(setting.Settings, "ClientSecret"),
                    HasAccessToken = HasValue(setting.Settings, "AccessToken")
                };
                break;
        }

        return projected;
    }

    private static Dictionary<string, string?> BuildSettings(
        string platformName,
        UserPublisherSettingUpdate request,
        Dictionary<string, string?>? existingSettings)
    {
        existingSettings ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        return NormalizePlatformName(platformName) switch
        {
            "bluesky" => BuildBlueskySettings(request.Bluesky, existingSettings),
            "twitter" => BuildTwitterSettings(request.Twitter, existingSettings),
            "facebook" => BuildFacebookSettings(request.Facebook, existingSettings),
            "linkedin" => BuildLinkedInSettings(request.LinkedIn, existingSettings),
            _ => new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static Dictionary<string, string?> BuildBlueskySettings(
        BlueskyPublisherSettingUpdate? settings,
        IReadOnlyDictionary<string, string?> existingSettings)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["BlueskyUserName"] = NormalizeValue(settings?.UserName),
            ["BlueskyPassword"] = MergeSecret(settings?.AppPassword, existingSettings, "BlueskyPassword")
        };
    }

    private static Dictionary<string, string?> BuildTwitterSettings(
        TwitterPublisherSettingUpdate? settings,
        IReadOnlyDictionary<string, string?> existingSettings)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConsumerKey"] = MergeSecret(settings?.ConsumerKey, existingSettings, "ConsumerKey"),
            ["ConsumerSecret"] = MergeSecret(settings?.ConsumerSecret, existingSettings, "ConsumerSecret"),
            ["OAuthToken"] = MergeSecret(settings?.AccessToken, existingSettings, "OAuthToken"),
            ["OAuthTokenSecret"] = MergeSecret(settings?.AccessTokenSecret, existingSettings, "OAuthTokenSecret")
        };
    }

    private static Dictionary<string, string?> BuildFacebookSettings(
        FacebookPublisherSettingUpdate? settings,
        IReadOnlyDictionary<string, string?> existingSettings)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["PageId"] = NormalizeValue(settings?.PageId),
            ["AppId"] = NormalizeValue(settings?.AppId),
            ["PageAccessToken"] = MergeSecret(settings?.PageAccessToken, existingSettings, "PageAccessToken"),
            ["AppSecret"] = MergeSecret(settings?.AppSecret, existingSettings, "AppSecret"),
            ["ClientToken"] = MergeSecret(settings?.ClientToken, existingSettings, "ClientToken"),
            ["ShortLivedAccessToken"] = MergeSecret(settings?.ShortLivedAccessToken, existingSettings, "ShortLivedAccessToken"),
            ["LongLivedAccessToken"] = MergeSecret(settings?.LongLivedAccessToken, existingSettings, "LongLivedAccessToken")
        };
    }

    private static Dictionary<string, string?> BuildLinkedInSettings(
        LinkedInPublisherSettingUpdate? settings,
        IReadOnlyDictionary<string, string?> existingSettings)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["AuthorId"] = NormalizeValue(settings?.AuthorId),
            ["ClientId"] = NormalizeValue(settings?.ClientId),
            ["ClientSecret"] = MergeSecret(settings?.ClientSecret, existingSettings, "ClientSecret"),
            ["AccessToken"] = MergeSecret(settings?.AccessToken, existingSettings, "AccessToken")
        };
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

    private static bool HasValue(IReadOnlyDictionary<string, string?> values, string key)
    {
        return !string.IsNullOrWhiteSpace(GetValue(values, key));
    }

    private static string? GetValue(IReadOnlyDictionary<string, string?> values, string key)
    {
        return values.TryGetValue(key, out var directValue)
            ? directValue
            : values.FirstOrDefault(pair => pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static string? MergeSecret(string? value, IReadOnlyDictionary<string, string?> existingSettings, string key)
    {
        var normalized = NormalizeValue(value);
        return string.IsNullOrWhiteSpace(normalized) ? GetValue(existingSettings, key) : normalized;
    }

    private static string? NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class UserPublisherSettingDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<UserPublisherSettingDataStore> logger) : IUserPublisherSettingDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static string SanitizeForLog(string? value) =>
        value?.Replace("\r", string.Empty).Replace("\n", string.Empty) ?? string.Empty;

    public async Task<List<UserPublisherSetting>> GetByUserAsync(string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);

        var entities = await broadcastingContext.UserPublisherSettings
            .AsNoTracking()
            .Include(setting => setting.SocialMediaPlatform)
            .Where(setting => setting.CreatedByEntraOid == ownerOid)
            .OrderBy(setting => setting.SocialMediaPlatform.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task<UserPublisherSetting?> GetByUserAndPlatformAsync(string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);

        var entity = await broadcastingContext.UserPublisherSettings
            .AsNoTracking()
            .Include(setting => setting.SocialMediaPlatform)
            .FirstOrDefaultAsync(
                setting => setting.CreatedByEntraOid == ownerOid && setting.SocialMediaPlatformId == platformId,
                cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<UserPublisherSetting?> SaveAsync(UserPublisherSetting setting, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(setting);
        ArgumentException.ThrowIfNullOrWhiteSpace(setting.CreatedByEntraOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(setting.SocialMediaPlatformId);

        try
        {
            var existing = await broadcastingContext.UserPublisherSettings
                .FirstOrDefaultAsync(
                    item => item.CreatedByEntraOid == setting.CreatedByEntraOid
                            && item.SocialMediaPlatformId == setting.SocialMediaPlatformId,
                    cancellationToken);

            if (existing is null)
            {
                existing = new Models.UserPublisherSetting
                {
                    CreatedByEntraOid = setting.CreatedByEntraOid,
                    SocialMediaPlatformId = setting.SocialMediaPlatformId,
                    CreatedOn = setting.CreatedOn == default ? DateTimeOffset.UtcNow : setting.CreatedOn
                };
                broadcastingContext.UserPublisherSettings.Add(existing);
            }

            existing.IsEnabled = setting.IsEnabled;
            existing.Settings = SerializeSettings(setting.Settings);
            existing.LastUpdatedOn = DateTimeOffset.UtcNow;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            return await GetByUserAndPlatformAsync(setting.CreatedByEntraOid, setting.SocialMediaPlatformId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save user publisher settings for owner {OwnerOid} and platform {PlatformId}",
                SanitizeForLog(setting.CreatedByEntraOid),
                setting.SocialMediaPlatformId);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string ownerOid, int platformId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);

        try
        {
            var existing = await broadcastingContext.UserPublisherSettings
                .FirstOrDefaultAsync(
                    item => item.CreatedByEntraOid == ownerOid && item.SocialMediaPlatformId == platformId,
                    cancellationToken);

            if (existing is null)
            {
                return false;
            }

            broadcastingContext.UserPublisherSettings.Remove(existing);
            return await broadcastingContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to delete user publisher settings for owner {OwnerOid} and platform {PlatformId}",
                SanitizeForLog(ownerOid),
                platformId);
            return false;
        }
    }

    private UserPublisherSetting MapToDomain(Models.UserPublisherSetting setting)
    {
        var settings = DeserializeSettings(setting.Settings);

        return new UserPublisherSetting
        {
            Id = setting.Id,
            CreatedByEntraOid = setting.CreatedByEntraOid,
            SocialMediaPlatformId = setting.SocialMediaPlatformId,
            SocialMediaPlatformName = setting.SocialMediaPlatform?.Name,
            SocialMediaPlatform = setting.SocialMediaPlatform is null
                ? null
                : mapper.Map<SocialMediaPlatform>(setting.SocialMediaPlatform),
            IsEnabled = setting.IsEnabled,
            Settings = settings,
            Bluesky = BuildBluesky(setting.SocialMediaPlatform?.Name, settings),
            Twitter = BuildTwitter(setting.SocialMediaPlatform?.Name, settings),
            Facebook = BuildFacebook(setting.SocialMediaPlatform?.Name, settings),
            LinkedIn = BuildLinkedIn(setting.SocialMediaPlatform?.Name, settings),
            CreatedOn = setting.CreatedOn,
            LastUpdatedOn = setting.LastUpdatedOn
        };
    }

    private static Dictionary<string, string?> DeserializeSettings(string? settings)
    {
        if (string.IsNullOrWhiteSpace(settings))
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        return JsonSerializer.Deserialize<Dictionary<string, string?>>(settings, SerializerOptions)
               ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    private static string? SerializeSettings(Dictionary<string, string?> settings)
    {
        if (settings.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(settings, SerializerOptions);
    }

    private static BlueskyPublisherSetting? BuildBluesky(string? platformName, IReadOnlyDictionary<string, string?> settings)
    {
        if (!IsPlatform(platformName, "bluesky"))
        {
            return null;
        }

        return new BlueskyPublisherSetting
        {
            UserName = GetSettingValue(settings, "BlueskyUserName"),
            HasAppPassword = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "BlueskyPassword"))
        };
    }

    private static TwitterPublisherSetting? BuildTwitter(string? platformName, IReadOnlyDictionary<string, string?> settings)
    {
        if (!IsPlatform(platformName, "twitter"))
        {
            return null;
        }

        return new TwitterPublisherSetting
        {
            HasConsumerKey = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "ConsumerKey")),
            HasConsumerSecret = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "ConsumerSecret")),
            HasAccessToken = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "OAuthToken")),
            HasAccessTokenSecret = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "OAuthTokenSecret"))
        };
    }

    private static FacebookPublisherSetting? BuildFacebook(string? platformName, IReadOnlyDictionary<string, string?> settings)
    {
        if (!IsPlatform(platformName, "facebook"))
        {
            return null;
        }

        return new FacebookPublisherSetting
        {
            PageId = GetSettingValue(settings, "PageId"),
            AppId = GetSettingValue(settings, "AppId"),
            HasPageAccessToken = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "PageAccessToken")),
            HasAppSecret = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "AppSecret")),
            HasClientToken = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "ClientToken")),
            HasShortLivedAccessToken = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "ShortLivedAccessToken")),
            HasLongLivedAccessToken = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "LongLivedAccessToken"))
        };
    }

    private static LinkedInPublisherSetting? BuildLinkedIn(string? platformName, IReadOnlyDictionary<string, string?> settings)
    {
        if (!IsPlatform(platformName, "linkedin"))
        {
            return null;
        }

        return new LinkedInPublisherSetting
        {
            AuthorId = GetSettingValue(settings, "AuthorId"),
            ClientId = GetSettingValue(settings, "ClientId"),
            HasClientSecret = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "ClientSecret")),
            HasAccessToken = !string.IsNullOrWhiteSpace(GetSettingValue(settings, "AccessToken"))
        };
    }

    private static bool IsPlatform(string? platformName, string expectedPlatformName) =>
        string.Equals(platformName?.Trim(), expectedPlatformName, StringComparison.OrdinalIgnoreCase);

    private static string? GetSettingValue(IReadOnlyDictionary<string, string?> settings, string key) =>
        settings.TryGetValue(key, out var value)
            ? value
            : settings.FirstOrDefault(pair => pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
}

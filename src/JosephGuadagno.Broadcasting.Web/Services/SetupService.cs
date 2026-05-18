using System.Security.Claims;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JosephGuadagno.Broadcasting.Web.Services;

/// <summary>
/// Checks all three onboarding areas (collectors, publishers, message templates) for the current user
/// and returns a <see cref="SetupStatus"/> snapshot. Results are cached per user for five minutes
/// to avoid API overhead on every page render.
/// </summary>
public class SetupService(
    IUserCollectorFeedSourceService feedSourceService,
    IUserCollectorYouTubeChannelService youTubeChannelService,
    IUserCollectorSpeakingEngagementService speakingEngagementService,
    IUserPublisherBlueskySettingsService blueskySettingsService,
    IUserPublisherTwitterSettingsService twitterSettingsService,
    IUserPublisherLinkedInSettingsService linkedInSettingsService,
    IUserPublisherFacebookSettingsService facebookSettingsService,
    IMessageTemplateService messageTemplateService,
    IMemoryCache cache,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SetupService> logger) : ISetupService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<SetupStatus> GetSetupStatusAsync(bool forceRefresh = false)
    {
        var userOid = httpContextAccessor.HttpContext?.User
            .FindFirstValue(ApplicationClaimTypes.EntraObjectId);

        var cacheKey = $"setup_status_{userOid}";

        if (!forceRefresh
            && !string.IsNullOrEmpty(userOid)
            && cache.TryGetValue(cacheKey, out SetupStatus? cached)
            && cached is not null)
        {
            return cached;
        }

        var status = await BuildSetupStatusAsync();

        if (!string.IsNullOrEmpty(userOid))
        {
            cache.Set(cacheKey, status, CacheTtl);
        }

        return status;
    }

    private async Task<SetupStatus> BuildSetupStatusAsync()
    {
        // Collectors — sequential awaits per project convention
        var feedSources = await feedSourceService.GetCurrentUserAsync();
        var youTubeChannels = await youTubeChannelService.GetCurrentUserAsync();
        var speakingEngagements = await speakingEngagementService.GetCurrentUserAsync();

        var hasCollector = feedSources.Count > 0
            || youTubeChannels.Count > 0
            || speakingEngagements.Count > 0;

        // Publishers — enabled flag indicates the platform is ready to use
        var bluesky = await blueskySettingsService.GetCurrentUserAsync();
        var twitter = await twitterSettingsService.GetCurrentUserAsync();
        var linkedIn = await linkedInSettingsService.GetCurrentUserAsync();
        var facebook = await facebookSettingsService.GetCurrentUserAsync();

        var configuredPublishers = new List<string>();
        if (bluesky?.IsEnabled == true) configuredPublishers.Add(MessageTemplates.Platforms.Bluesky);
        if (twitter?.IsEnabled == true) configuredPublishers.Add(MessageTemplates.Platforms.Twitter);
        if (linkedIn?.IsEnabled == true) configuredPublishers.Add(MessageTemplates.Platforms.LinkedIn);
        if (facebook?.IsEnabled == true) configuredPublishers.Add(MessageTemplates.Platforms.Facebook);

        var hasPublisher = configuredPublishers.Count > 0;

        // Templates — for each configured publisher platform, at least one template must exist
        var templateResult = await messageTemplateService.GetAllAsync(pageSize: Pagination.MaxPageSize);
        var templatePlatforms = templateResult?.Items
            .Select(t => t.Platform)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        var missingTemplatePlatforms = configuredPublishers
            .Where(p => !templatePlatforms.Contains(p))
            .ToList();

        // Templates are complete when: no publishers configured, or all configured publishers have templates
        var hasMessageTemplates = configuredPublishers.Count == 0 || missingTemplatePlatforms.Count == 0;

        logger.LogDebug(
            "Setup status built: HasCollector={HasCollector}, HasPublisher={HasPublisher}, HasMessageTemplates={HasMessageTemplates}",
            hasCollector, hasPublisher, hasMessageTemplates);

        return new SetupStatus
        {
            HasCollector = hasCollector,
            HasPublisher = hasPublisher,
            HasMessageTemplates = hasMessageTemplates,
            ConfiguredPublisherPlatforms = configuredPublishers,
            MissingTemplatePlatforms = missingTemplatePlatforms
        };
    }
}

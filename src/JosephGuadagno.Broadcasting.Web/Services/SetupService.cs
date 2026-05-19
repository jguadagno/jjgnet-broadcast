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
    ISocialMediaPlatformService socialMediaPlatformService,
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

        // Map each collector type the user has to its message type constant
        var collectorTypes = new List<string>();
        if (feedSources.Count > 0) collectorTypes.Add(MessageTemplates.MessageTypes.NewSyndicationFeedItem);
        if (youTubeChannels.Count > 0) collectorTypes.Add(MessageTemplates.MessageTypes.NewYouTubeItem);
        if (speakingEngagements.Count > 0) collectorTypes.Add(MessageTemplates.MessageTypes.NewSpeakingEngagement);

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

        // Build platform icon lookup from the DB so the view never hard-codes icon classes
        var platformIcons = (await socialMediaPlatformService.GetAllAsync(pageSize: 100))
            ?.Items
            .ToDictionary(p => p.Name, p => p.Icon ?? "bi-broadcast", StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var publisherSummaries = configuredPublishers
            .Select(name => new PlatformSummary(name, platformIcons.GetValueOrDefault(name, "bi-broadcast")))
            .ToList();

        // Templates— intersection-based: every (publisher × collectorType) pair needs a template
        var templateResult = await messageTemplateService.GetAllAsync(pageSize: Pagination.MaxPageSize);
        var existingTemplates = templateResult?.Items
            .Where(t => !string.IsNullOrEmpty(t.Platform) && !string.IsNullOrEmpty(t.MessageType))
            .Select(t => (
                Platform: t.Platform.Trim(),
                MessageType: t.MessageType.Trim()))
            .ToHashSet() ?? [];

        var missingTemplatePairs = new List<MissingTemplateKey>();
        foreach (var publisher in configuredPublishers)
        {
            foreach (var messageType in collectorTypes)
            {
                if (!existingTemplates.Contains((publisher, messageType)))
                {
                    missingTemplatePairs.Add(new MissingTemplateKey(publisher, messageType));
                }
            }
        }

        // Derive the distinct platforms for display convenience
        var missingTemplatePlatforms = missingTemplatePairs
            .Select(p => p.Platform)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Complete when there are no publishers, no collectors, or all required pairs exist
        var hasMessageTemplates = configuredPublishers.Count == 0
            || collectorTypes.Count == 0
            || missingTemplatePairs.Count == 0;

        logger.LogDebug(
            "Setup status built: HasCollector={HasCollector}, HasPublisher={HasPublisher}, HasMessageTemplates={HasMessageTemplates}, MissingPairs={MissingPairCount}",
            hasCollector, hasPublisher, hasMessageTemplates, missingTemplatePairs.Count);

        return new SetupStatus
        {
            HasCollector = hasCollector,
            HasPublisher = hasPublisher,
            HasMessageTemplates = hasMessageTemplates,
            ConfiguredPublisherPlatforms = configuredPublishers,
            ConfiguredPublisherSummaries = publisherSummaries,
            ConfiguredCollectorTypes = collectorTypes,
            MissingTemplatePairs = missingTemplatePairs,
            MissingTemplatePlatforms = missingTemplatePlatforms
        };
    }
}

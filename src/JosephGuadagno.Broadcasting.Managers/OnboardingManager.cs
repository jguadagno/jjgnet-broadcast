using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Recomputes and persists the <c>IsOnboarded</c> flag on <c>ApplicationUser</c>
/// whenever the user's collectors, publishers, or message templates change.
/// <para>
/// A user is considered onboarded when:
/// <list type="bullet">
///   <item>They have at least one collector (feed source, YouTube channel, or speaking engagement).</item>
///   <item>They have at least one enabled publisher (Bluesky, Twitter, LinkedIn, or Facebook).</item>
///   <item>For every (publisher × collector-type) pair, a message template exists.</item>
/// </list>
/// When there are no publishers or no collectors the template check is vacuously satisfied.
/// </para>
/// </summary>
public class OnboardingManager(
    IApplicationUserDataStore applicationUserDataStore,
    IUserCollectorFeedSourceDataStore feedSourceDataStore,
    IUserCollectorYouTubeChannelDataStore youTubeChannelDataStore,
    IUserCollectorSpeakingEngagementDataStore speakingEngagementDataStore,
    IUserPublisherBlueskySettingsDataStore blueskyDataStore,
    IUserPublisherTwitterSettingsDataStore twitterDataStore,
    IUserPublisherLinkedInSettingsDataStore linkedInDataStore,
    IUserPublisherFacebookSettingsDataStore facebookDataStore,
    IMessageTemplateDataStore messageTemplateDataStore,
    IMemoryCache cache,
    ILogger<OnboardingManager> logger) : IOnboardingManager
{
    /// <inheritdoc />
    public async Task RecalculateAsync(string entraOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entraOid);

        try
        {
            var isOnboarded = await ComputeIsOnboardedAsync(entraOid, cancellationToken);

            var updated = await applicationUserDataStore.UpdateIsOnboardedAsync(entraOid, isOnboarded, cancellationToken);
            if (!updated)
            {
                logger.LogWarning(
                    "UpdateIsOnboardedAsync returned false for Entra OID {EntraOid}. User may not exist yet",
                    entraOid);
                return;
            }

            // Evict the claims cache so the next API request picks up the updated IsOnboarded flag.
            cache.Remove($"claims_{entraOid}");

            logger.LogInformation(
                "Onboarding status recalculated for Entra OID {EntraOid}: IsOnboarded={IsOnboarded}",
                entraOid,
                isOnboarded);
        }
        catch (Exception ex)
        {
            // Non-fatal — log and swallow so the original mutation still succeeds.
            logger.LogError(
                ex,
                "Failed to recalculate onboarding status for Entra OID {EntraOid}",
                entraOid);
        }
    }

    private async Task<bool> ComputeIsOnboardedAsync(string entraOid, CancellationToken cancellationToken)
    {
        // Sequential awaits: all data stores share the same scoped BroadcastingContext.
        // EF Core's DbContext is not thread-safe — Task.WhenAll causes concurrent reads on the
        // same connection, which throws "BeginExecuteReader requires an open and available Connection."
        var feedSources = await feedSourceDataStore.GetByUserAsync(entraOid, activeOnly: true, cancellationToken);
        var youTubeChannels = await youTubeChannelDataStore.GetByUserAsync(entraOid, activeOnly: true, cancellationToken);
        var speakingEngagements = await speakingEngagementDataStore.GetByUserAsync(entraOid, activeOnly: true, cancellationToken);

        var hasCollector = feedSources.Count > 0
            || youTubeChannels.Count > 0
            || speakingEngagements.Count > 0;

        var collectorTypes = new List<string>();
        if (feedSources.Count > 0) collectorTypes.Add(MessageTemplates.MessageTypes.NewSyndicationFeedItem);
        if (youTubeChannels.Count > 0) collectorTypes.Add(MessageTemplates.MessageTypes.NewYouTubeItem);
        if (speakingEngagements.Count > 0) collectorTypes.Add(MessageTemplates.MessageTypes.NewSpeakingEngagement);

        // Publishers — only enabled ones count
        var bluesky = await blueskyDataStore.GetByUserAsync(entraOid, cancellationToken);
        var twitter = await twitterDataStore.GetByUserAsync(entraOid, cancellationToken);
        var linkedIn = await linkedInDataStore.GetByUserAsync(entraOid, cancellationToken);
        var facebook = await facebookDataStore.GetByUserAsync(entraOid, cancellationToken);

        var configuredPublishers = new List<string>();
        if (bluesky?.IsEnabled == true) configuredPublishers.Add(MessageTemplates.Platforms.Bluesky);
        if (twitter?.IsEnabled == true) configuredPublishers.Add(MessageTemplates.Platforms.Twitter);
        if (linkedIn?.IsEnabled == true) configuredPublishers.Add(MessageTemplates.Platforms.LinkedIn);
        if (facebook?.IsEnabled == true) configuredPublishers.Add(MessageTemplates.Platforms.Facebook);

        var hasPublisher = configuredPublishers.Count > 0;

        // Message templates — vacuously complete when there are no publishers or no collectors
        var templates = await messageTemplateDataStore.GetAllAsync(entraOid, cancellationToken);
        var hasMessageTemplates = configuredPublishers.Count == 0
            || collectorTypes.Count == 0
            || await HasAllRequiredTemplatesAsync(templates, configuredPublishers, collectorTypes);

        return hasCollector && hasPublisher && hasMessageTemplates;
    }

    private static Task<bool> HasAllRequiredTemplatesAsync(
        IEnumerable<Domain.Models.MessageTemplate> templates,
        IEnumerable<string> configuredPublishers,
        IEnumerable<string> collectorTypes)
    {
        var existingPairs = templates
            .Where(t => !string.IsNullOrEmpty(t.Platform) && !string.IsNullOrEmpty(t.MessageType))
            .Select(t => (Platform: t.Platform!.Trim(), MessageType: t.MessageType!.Trim()))
            .ToHashSet();

        foreach (var publisher in configuredPublishers)
        {
            foreach (var messageType in collectorTypes)
            {
                if (!existingPairs.Contains((publisher, messageType)))
                {
                    return Task.FromResult(false);
                }
            }
        }

        return Task.FromResult(true);
    }
}

using JosephGuadagno.Broadcasting.Domain.Constants;

namespace JosephGuadagno.Broadcasting.Web.Constants;

/// <summary>
/// Shared display metadata for per-user distributor event routing options.
/// </summary>
public static class DistributorEventTypes
{
    /// <summary>
    /// The complete set of supported event types for per-user distributor mappings.
    /// </summary>
    public static readonly IReadOnlyList<DistributorEventTypeOption> All =
    [
        new(MessageTemplates.MessageTypes.NewSyndicationFeedItem, "Feed Item", CollectorIcons.FeedSource.Icon),
        new(MessageTemplates.MessageTypes.NewYouTubeItem, "YouTube", CollectorIcons.YouTubeChannel.Icon),
        new(MessageTemplates.MessageTypes.NewSpeakingEngagement, "Speaking Engagement", CollectorIcons.SpeakingEngagement.Icon),
        new(MessageTemplates.MessageTypes.RandomPost, "Random Post", "bi-shuffle"),
        new(MessageTemplates.MessageTypes.ScheduledItem, "Scheduled Item", "bi-calendar-event")
    ];

    /// <summary>
    /// Gets the display metadata for the supplied event type.
    /// </summary>
    public static DistributorEventTypeOption Get(string? eventType)
    {
        return All.FirstOrDefault(option =>
                   string.Equals(option.Value, eventType, StringComparison.OrdinalIgnoreCase))
               ?? new DistributorEventTypeOption(eventType ?? string.Empty, eventType ?? string.Empty, CollectorIcons.Collection);
    }
}

/// <summary>
/// Represents a selectable distributor event type.
/// </summary>
/// <param name="Value">The message type value sent to the API.</param>
/// <param name="Label">The display label shown in the UI.</param>
/// <param name="Icon">The icon shown beside the event type.</param>
public sealed record DistributorEventTypeOption(string Value, string Label, string Icon);

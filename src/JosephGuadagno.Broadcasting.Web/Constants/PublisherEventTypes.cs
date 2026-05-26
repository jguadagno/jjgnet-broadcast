using JosephGuadagno.Broadcasting.Domain.Constants;

namespace JosephGuadagno.Broadcasting.Web.Constants;

/// <summary>
/// Shared display metadata for per-user publisher event routing options.
/// </summary>
public static class PublisherEventTypes
{
    /// <summary>
    /// The complete set of supported event types for per-user publisher mappings.
    /// </summary>
    public static readonly IReadOnlyList<PublisherEventTypeOption> All =
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
    public static PublisherEventTypeOption Get(string? eventType)
    {
        return All.FirstOrDefault(option =>
                   string.Equals(option.Value, eventType, StringComparison.OrdinalIgnoreCase))
               ?? new PublisherEventTypeOption(eventType ?? string.Empty, eventType ?? string.Empty, CollectorIcons.Collection);
    }
}

/// <summary>
/// Represents a selectable publisher event type.
/// </summary>
/// <param name="Value">The message type value sent to the API.</param>
/// <param name="Label">The display label shown in the UI.</param>
/// <param name="Icon">The icon shown beside the event type.</param>
public sealed record PublisherEventTypeOption(string Value, string Label, string Icon);

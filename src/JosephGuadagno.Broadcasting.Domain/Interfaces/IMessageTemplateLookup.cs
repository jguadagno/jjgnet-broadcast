namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Looks up the user-scoped message template for a given platform and message type.
/// </summary>
public interface IMessageTemplateLookup
{
    /// <summary>
    /// Returns the user-scoped template for the given platform, message type, and owner.
    /// </summary>
    /// <param name="platformName">The platform name (e.g., "Bluesky", "Twitter", "Facebook", "LinkedIn").</param>
    /// <param name="messageType">The message type (e.g., "NewSyndicationFeedItem", "ScheduledItem").</param>
    /// <param name="ownerEntraOid">The Entra Object ID of the user who owns the content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The <see cref="Models.MessageTemplate"/> if found; otherwise <c>null</c>.
    /// Callers must bail (not enqueue) when this returns <c>null</c> — templates are required.
    /// </returns>
    Task<Models.MessageTemplate?> GetAsync(
        string platformName,
        string messageType,
        string ownerEntraOid,
        CancellationToken cancellationToken = default);
}

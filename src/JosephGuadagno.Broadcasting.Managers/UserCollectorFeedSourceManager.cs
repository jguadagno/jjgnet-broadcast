using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Manager for per-user RSS/Atom/JSON feed collector configurations
/// </summary>
public class UserCollectorFeedSourceManager(IUserCollectorFeedSourceDataStore dataStore) : IUserCollectorFeedSourceManager
{
    /// <inheritdoc />
    public Task<List<UserCollectorFeedSource>> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.GetByUserAsync(ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserCollectorFeedSource?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return dataStore.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<UserCollectorFeedSource>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return dataStore.GetAllActiveAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserCollectorFeedSource?> SaveAsync(
        UserCollectorFeedSource config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.CreatedByEntraOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.FeedUrl);

        if (!Uri.TryCreate(config.FeedUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("FeedUrl must be a valid absolute URI", nameof(config));
        }

        return dataStore.SaveAsync(config, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(
        int id, string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.DeleteAsync(id, ownerOid, cancellationToken);
    }
}

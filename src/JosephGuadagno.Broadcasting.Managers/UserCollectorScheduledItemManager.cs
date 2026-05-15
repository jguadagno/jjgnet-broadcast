using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class UserCollectorScheduledItemManager(IUserCollectorScheduledItemDataStore dataStore) : IUserCollectorScheduledItemManager
{
    /// <inheritdoc />
    public Task<List<UserCollectorScheduledItem>> GetByUserAsync(
        string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.GetByUserAsync(ownerOid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserCollectorScheduledItem?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return dataStore.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<UserCollectorScheduledItem>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return dataStore.GetAllActiveAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserCollectorScheduledItem?> SaveAsync(
        UserCollectorScheduledItem config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.CreatedByEntraOid);

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

    /// <inheritdoc />
    public Task<PagedResult<UserCollectorScheduledItem>> GetAllAsync(
        string ownerOid, int page, int pageSize, string sortBy = "displayname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.GetAllAsync(ownerOid, page, pageSize, sortBy, sortDescending, filter, cancellationToken);
    }
}

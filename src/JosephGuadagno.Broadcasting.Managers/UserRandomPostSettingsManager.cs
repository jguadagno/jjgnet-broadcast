using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

/// <summary>
/// Manager for per-user random post schedules and filtering settings.
/// </summary>
public class UserRandomPostSettingsManager(IUserRandomPostSettingsDataStore dataStore) : IUserRandomPostSettingsManager
{
    /// <inheritdoc />
    public Task<List<UserRandomPostSettings>> GetByUserAsync(
        string ownerOid,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.GetByUserAsync(ownerOid, activeOnly, cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserRandomPostSettings?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return dataStore.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<UserRandomPostSettings>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return dataStore.GetAllActiveAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserRandomPostSettings?> SaveAsync(
        UserRandomPostSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CreatedByEntraOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.CronExpression);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.SocialMediaPlatformId);
        return dataStore.SaveAsync(settings, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(int id, string ownerOid, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerOid);
        return dataStore.DeleteAsync(id, ownerOid, cancellationToken);
    }
}

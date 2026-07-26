using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for per-user Bluesky publisher settings
/// </summary>
public interface IUserPlatformBlueskySettingsManager
{
    Task<UserPlatformBlueskySettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task<UserPlatformBlueskySettings?> SaveAsync(UserPlatformBlueskySettings settings, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task<string?> GetAppPasswordAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreAppPasswordAsync(string ownerOid, string appPassword, CancellationToken cancellationToken = default);
}


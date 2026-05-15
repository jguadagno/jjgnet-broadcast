using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for per-user Bluesky publisher settings
/// </summary>
public interface IUserPublisherBlueskySettingsManager
{
    Task<UserPublisherBlueskySettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task<UserPublisherBlueskySettings?> SaveAsync(UserPublisherBlueskySettings settings, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task<string?> GetAppPasswordAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreAppPasswordAsync(string ownerOid, string appPassword, CancellationToken cancellationToken = default);
}

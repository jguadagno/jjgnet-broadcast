using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for per-user LinkedIn publisher settings
/// </summary>
public interface IUserPlatformLinkedInSettingsManager
{
    Task<UserPlatformLinkedInSettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task<UserPlatformLinkedInSettings?> SaveAsync(UserPlatformLinkedInSettings settings, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task<string?> GetClientSecretAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreClientSecretAsync(string ownerOid, string clientSecret, CancellationToken cancellationToken = default);
    Task<string?> GetAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreAccessTokenAsync(string ownerOid, string accessToken, CancellationToken cancellationToken = default);
}


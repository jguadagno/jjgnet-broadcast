using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for per-user Twitter publisher settings
/// </summary>
public interface IUserPlatformTwitterSettingsManager
{
    Task<UserPlatformTwitterSettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task<UserPlatformTwitterSettings?> SaveAsync(UserPlatformTwitterSettings settings, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task<string?> GetConsumerKeyAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreConsumerKeyAsync(string ownerOid, string consumerKey, CancellationToken cancellationToken = default);
    Task<string?> GetConsumerSecretAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreConsumerSecretAsync(string ownerOid, string consumerSecret, CancellationToken cancellationToken = default);
    Task<string?> GetAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreAccessTokenAsync(string ownerOid, string accessToken, CancellationToken cancellationToken = default);
    Task<string?> GetAccessTokenSecretAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreAccessTokenSecretAsync(string ownerOid, string accessTokenSecret, CancellationToken cancellationToken = default);
}


using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manager for per-user Facebook publisher settings
/// </summary>
public interface IUserPublisherFacebookSettingsManager
{
    Task<UserPublisherFacebookSettings?> GetAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task<UserPublisherFacebookSettings?> SaveAsync(UserPublisherFacebookSettings settings, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task<string?> GetPageAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StorePageAccessTokenAsync(string ownerOid, string pageAccessToken, CancellationToken cancellationToken = default);
    Task<string?> GetAppSecretAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreAppSecretAsync(string ownerOid, string appSecret, CancellationToken cancellationToken = default);
    Task<string?> GetClientTokenAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreClientTokenAsync(string ownerOid, string clientToken, CancellationToken cancellationToken = default);
    Task<string?> GetShortLivedAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreShortLivedAccessTokenAsync(string ownerOid, string shortLivedAccessToken, CancellationToken cancellationToken = default);
    Task<string?> GetLongLivedAccessTokenAsync(string ownerOid, CancellationToken cancellationToken = default);
    Task StoreLongLivedAccessTokenAsync(string ownerOid, string longLivedAccessToken, CancellationToken cancellationToken = default);
}

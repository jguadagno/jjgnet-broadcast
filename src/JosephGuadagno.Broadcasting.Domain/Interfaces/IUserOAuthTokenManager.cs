using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IUserOAuthTokenManager
{
    Task<UserOAuthToken?> GetByUserAndPlatformAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores (create or replace) the token issued by an OAuth callback.
    /// </summary>
    Task<UserOAuthToken?> StoreOAuthCallbackTokenAsync(
        string ownerOid,
        int platformId,
        string accessToken,
        string? refreshToken,
        DateTimeOffset accessTokenExpiresAt,
        DateTimeOffset? refreshTokenExpiresAt,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default);
}

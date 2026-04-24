using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IUserOAuthTokenDataStore
{
    Task<UserOAuthToken?> GetByUserAndPlatformAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default);

    Task<UserOAuthToken?> UpsertAsync(
        UserOAuthToken token, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string ownerOid, int platformId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all tokens whose AccessTokenExpiresAt is at or before <paramref name="threshold"/>.
    /// Used by background refresh Functions.
    /// </summary>
    Task<List<UserOAuthToken>> GetExpiringAsync(
        DateTimeOffset threshold, CancellationToken cancellationToken = default);
}

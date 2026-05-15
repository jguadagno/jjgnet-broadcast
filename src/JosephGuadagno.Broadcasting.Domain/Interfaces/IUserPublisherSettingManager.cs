using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IUserPublisherSettingManager
{
    Task<List<UserPublisherSetting>> GetByUserAsync(string ownerOid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the raw credential values for the given user and platform.
    /// If the stored Settings contain a <c>SecretName</c> key, the credentials are
    /// fetched from Azure Key Vault; otherwise the raw Settings values are returned.
    /// </summary>
    Task<Dictionary<string, string?>> GetCredentialsAsync(string ownerOid, int platformId, CancellationToken cancellationToken = default);

    Task<UserPublisherSetting?> GetByUserAndPlatformAsync(string ownerOid, int platformId, CancellationToken cancellationToken = default);

    Task<UserPublisherSetting?> SaveAsync(UserPublisherSettingUpdate setting, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string ownerOid, int platformId, CancellationToken cancellationToken = default);

    Task<PagedResult<UserPublisherSetting>> GetAllAsync(string ownerOid, int page, int pageSize, string sortBy = "platformname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}

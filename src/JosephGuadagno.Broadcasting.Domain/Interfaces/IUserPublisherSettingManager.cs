using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IUserPublisherSettingManager
{
    Task<List<UserPublisherSetting>> GetByUserAsync(string ownerOid, CancellationToken cancellationToken = default);

    Task<UserPublisherSetting?> GetByUserAndPlatformAsync(string ownerOid, int platformId, CancellationToken cancellationToken = default);

    Task<UserPublisherSetting?> SaveAsync(UserPublisherSettingUpdate setting, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string ownerOid, int platformId, CancellationToken cancellationToken = default);

    Task<PagedResult<UserPublisherSetting>> GetAllAsync(string ownerOid, int page, int pageSize, string sortBy = "platformname", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}

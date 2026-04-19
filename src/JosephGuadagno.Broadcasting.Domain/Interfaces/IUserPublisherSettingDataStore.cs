using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IUserPublisherSettingDataStore
{
    Task<List<UserPublisherSetting>> GetByUserAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<UserPublisherSetting?> GetByUserAndPlatformAsync(string ownerEntraOid, int socialMediaPlatformId, CancellationToken cancellationToken = default);
    Task<UserPublisherSetting?> SaveAsync(UserPublisherSetting setting, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string ownerEntraOid, int socialMediaPlatformId, CancellationToken cancellationToken = default);
}

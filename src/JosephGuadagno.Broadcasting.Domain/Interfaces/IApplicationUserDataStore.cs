using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Data store for application user operations
/// </summary>
public interface IApplicationUserDataStore
{
    Task<ApplicationUser?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<ApplicationUser>> GetByApprovalStatusAsync(string approvalStatus, CancellationToken cancellationToken = default);
    Task<ApplicationUser> CreateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<ApplicationUser> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}

using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

/// <summary>
/// Service interface for interacting with the per-user scheduled item collector configuration in the API.
/// Each user has at most one scheduled item configuration.
/// </summary>
public interface IUserCollectorScheduledItemService
{
    Task<UserCollectorScheduledItem?> GetAsync(string ownerOid);
    Task<UserCollectorScheduledItem?> SaveAsync(UserCollectorScheduledItem item);
    Task<bool> DeleteAsync(string ownerOid);
}

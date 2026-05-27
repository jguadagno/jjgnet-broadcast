using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IDispatchersAggregateService
{
    Task<DispatchersAggregateViewModel?> GetCurrentUserAsync();
}

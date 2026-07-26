using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IPlatformsAggregateService
{
    Task<PlatformsAggregateViewModel?> GetCurrentUserAsync();
}

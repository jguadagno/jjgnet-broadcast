using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IPublishersAggregateService
{
    Task<PublishersAggregateViewModel?> GetCurrentUserAsync();
}

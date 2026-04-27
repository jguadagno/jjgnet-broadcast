using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface ISyndicationFeedSourceService
{
    Task<PagedResult<SyndicationFeedSource>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null);
    Task<SyndicationFeedSource?> GetAsync(int id);
    Task<SyndicationFeedSource?> SaveAsync(SyndicationFeedSource source);
    Task<bool> DeleteAsync(int id);
}

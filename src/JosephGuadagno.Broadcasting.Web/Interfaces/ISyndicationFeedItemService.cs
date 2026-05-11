using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface ISyndicationFeedItemService
{
    Task<PagedResult<SyndicationFeedItem>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null);
    Task<SyndicationFeedItem?> GetAsync(int id);
    Task<SyndicationFeedItem?> SaveAsync(SyndicationFeedItem source);
    Task<bool> DeleteAsync(int id);
}

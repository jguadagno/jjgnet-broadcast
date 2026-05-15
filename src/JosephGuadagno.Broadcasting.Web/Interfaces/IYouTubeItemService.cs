using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IYouTubeItemService
{
    Task<PagedResult<YouTubeItem>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null);
    Task<YouTubeItem?> GetAsync(int id);
    Task<YouTubeItem?> SaveAsync(YouTubeItem source);
    Task<bool> DeleteAsync(int id);
}

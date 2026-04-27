using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IYouTubeSourceService
{
    Task<PagedResult<YouTubeSource>> GetAllAsync(int page = Pagination.DefaultPage, int pageSize = Pagination.DefaultPageSize, string sortBy = "name", bool sortDescending = false, string? filter = null);
    Task<YouTubeSource?> GetAsync(int id);
    Task<YouTubeSource?> SaveAsync(YouTubeSource source);
    Task<bool> DeleteAsync(int id);
}

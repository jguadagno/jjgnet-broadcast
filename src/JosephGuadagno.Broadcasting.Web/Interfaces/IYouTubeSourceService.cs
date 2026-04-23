using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IYouTubeSourceService
{
    Task<List<YouTubeSource>> GetAllAsync();
    Task<YouTubeSource?> GetAsync(int id);
    Task<YouTubeSource?> SaveAsync(YouTubeSource source);
    Task<bool> DeleteAsync(int id);
}

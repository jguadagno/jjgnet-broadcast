using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface ISyndicationFeedSourceService
{
    Task<List<SyndicationFeedSource>> GetAllAsync();
    Task<SyndicationFeedSource?> GetAsync(int id);
    Task<SyndicationFeedSource?> SaveAsync(SyndicationFeedSource source);
    Task<bool> DeleteAsync(int id);
}

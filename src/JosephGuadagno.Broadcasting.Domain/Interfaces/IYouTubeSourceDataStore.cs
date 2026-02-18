using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IYouTubeSourceDataStore : IDataStore<Domain.Models.YouTubeSource>
{
    public Task<Domain.Models.YouTubeSource?> GetByUrlAsync(string url);
}

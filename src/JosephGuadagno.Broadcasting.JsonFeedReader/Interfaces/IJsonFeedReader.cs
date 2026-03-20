using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.JsonFeedReader.Interfaces;

public interface IJsonFeedReader
{
    public List<JsonFeedSource> GetSinceDate(DateTimeOffset sinceWhen);
    public Task<List<JsonFeedSource>> GetAsync(DateTimeOffset sinceWhen);
}

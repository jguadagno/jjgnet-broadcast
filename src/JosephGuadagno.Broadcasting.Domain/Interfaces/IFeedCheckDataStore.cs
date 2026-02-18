using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IFeedCheckDataStore : IDataStore<Domain.Models.FeedCheck>
{
    public Task<Domain.Models.FeedCheck?> GetByNameAsync(string name);
}
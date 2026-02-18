using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IFeedCheckRepository : IDataRepository<FeedCheck>
{
    public Task<FeedCheck?> GetByNameAsync(string name);
}
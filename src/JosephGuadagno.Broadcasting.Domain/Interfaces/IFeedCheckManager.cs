using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IFeedCheckManager : IManager<FeedCheck>
{
    public Task<FeedCheck?> GetByNameAsync(string name);
}
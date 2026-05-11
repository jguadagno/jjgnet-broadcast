namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IFeedCheckDataStore : IDataStore<Models.FeedCheck>
{
    public Task<Models.FeedCheck?> GetByNameAsync(string name, string entraOId, CancellationToken cancellationToken = default);
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class FeedCheckManager(IFeedCheckDataStore feedCheckDataStore) : IFeedCheckManager
{
	public async Task<FeedCheck> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await feedCheckDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<FeedCheck>> SaveAsync(FeedCheck entity, CancellationToken cancellationToken = default)
    {
        return await feedCheckDataStore.SaveAsync(entity, cancellationToken);
    }

    public async Task<List<FeedCheck>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await feedCheckDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(FeedCheck entity, CancellationToken cancellationToken = default)
    {
        return await feedCheckDataStore.DeleteAsync(entity, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await feedCheckDataStore.DeleteAsync(primaryKey, cancellationToken);
    }

    public async Task<FeedCheck?> GetByNameAsync(string name, string entraOId, CancellationToken cancellationToken = default)
    {
        return await feedCheckDataStore.GetByNameAsync(name, entraOId, cancellationToken);
    }
}
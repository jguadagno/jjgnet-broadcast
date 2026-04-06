using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class SyndicationFeedSourceManager : ISyndicationFeedSourceManager
{
    private readonly ISyndicationFeedSourceDataStore _syndicationFeedSourceDataStore;

    public SyndicationFeedSourceManager(ISyndicationFeedSourceDataStore syndicationFeedSourceDataStore)
    {
        _syndicationFeedSourceDataStore = syndicationFeedSourceDataStore;
    }

    public async Task<SyndicationFeedSource> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<SyndicationFeedSource>> SaveAsync(SyndicationFeedSource entity, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.SaveAsync(entity, cancellationToken);
    }

    public async Task<List<SyndicationFeedSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(SyndicationFeedSource entity, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.DeleteAsync(entity, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.DeleteAsync(primaryKey, cancellationToken);
    }

    public async Task<SyndicationFeedSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetByUrlAsync(url, cancellationToken);
    }

    public async Task<SyndicationFeedSource?> GetByFeedIdentifierAsync(string feedIdentifier, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetByFeedIdentifierAsync(feedIdentifier, cancellationToken);
    }

    public async Task<SyndicationFeedSource?> GetRandomSyndicationDataAsync(DateTimeOffset cutoffDate, List<string> excludedCategories, CancellationToken cancellationToken = default)
    {
        return await _syndicationFeedSourceDataStore.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories, cancellationToken);
    }
}
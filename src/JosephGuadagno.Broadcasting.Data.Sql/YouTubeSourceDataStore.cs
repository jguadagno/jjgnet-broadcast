using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class YouTubeSourceDataStore(BroadcastingContext broadcastingContext, IMapper mapper, ILogger<YouTubeSourceDataStore> logger) : IYouTubeSourceDataStore
{
    private const string SourceType = "YouTube";

    public async Task<Domain.Models.YouTubeSource> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources
            .FirstOrDefaultAsync(y => y.Id == primaryKey, cancellationToken);
        
        if (dbYouTubeSource is not null)
        {
            dbYouTubeSource.SourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == primaryKey && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
    }

    public async Task<OperationResult<Domain.Models.YouTubeSource>> SaveAsync(Domain.Models.YouTubeSource entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceId = await broadcastingContext.ExecuteInTransactionIfSupportedAsync(async ct =>
            {
                var dbYouTubeSource = mapper.Map<Models.YouTubeSource>(entity);
                broadcastingContext.Entry(dbYouTubeSource).State =
                    dbYouTubeSource.Id == 0 ? EntityState.Added : EntityState.Modified;

                await broadcastingContext.SaveChangesAsync(ct);
                await SyncSourceTagsAsync(dbYouTubeSource.Id, entity.Tags, ct);

                return dbYouTubeSource.Id;
            }, cancellationToken);

            var saved = await broadcastingContext.YouTubeSources
                .FirstOrDefaultAsync(y => y.Id == sourceId, cancellationToken);

            if (saved is not null)
            {
                saved.SourceTags = await broadcastingContext.SourceTags
                    .Where(st => st.SourceId == saved.Id && st.SourceType == SourceType)
                    .ToListAsync(cancellationToken);
            }

            return saved is not null
                ? OperationResult<Domain.Models.YouTubeSource>.Success(mapper.Map<Domain.Models.YouTubeSource>(saved))
                : OperationResult<Domain.Models.YouTubeSource>.Failure("Failed to save YouTube source");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save YouTube source {SourceId}", entity.Id);
            return OperationResult<Domain.Models.YouTubeSource>.Failure("An error occurred while saving the YouTube source", ex);
        }
    }

    public async Task<List<Domain.Models.YouTubeSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbYouTubeSources = await broadcastingContext.YouTubeSources
            .ToListAsync(cancellationToken);
        
        foreach (var source in dbYouTubeSources)
        {
            source.SourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == source.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return mapper.Map<List<Domain.Models.YouTubeSource>>(dbYouTubeSources);
    }

    public async Task<List<Domain.Models.YouTubeSource>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSources = await broadcastingContext.YouTubeSources
            .Where(y => y.CreatedByEntraOid == ownerEntraOid)
            .ToListAsync(cancellationToken);
        
        foreach (var source in dbYouTubeSources)
        {
            source.SourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == source.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return mapper.Map<List<Domain.Models.YouTubeSource>>(dbYouTubeSources);
    }

    public async Task<OperationResult<bool>> DeleteAsync(Domain.Models.YouTubeSource entity, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(entity.Id, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbYouTubeSource = await broadcastingContext.YouTubeSources
                .FirstOrDefaultAsync(y => y.Id == primaryKey, cancellationToken);
            if (dbYouTubeSource == null) return OperationResult<bool>.Success(true);

            var sourceTags = await broadcastingContext.SourceTags
                .Where(st => st.SourceId == primaryKey && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
            
            broadcastingContext.SourceTags.RemoveRange(sourceTags);
            broadcastingContext.YouTubeSources.Remove(dbYouTubeSource);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete YouTube source {SourceId}", primaryKey);
            return OperationResult<bool>.Failure("An error occurred while deleting the YouTube source", ex);
        }
    }

    public async Task<Domain.Models.YouTubeSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Url == url, cancellationToken);
        
        if (dbYouTubeSource is not null)
        {
            dbYouTubeSource.SourceTags = await broadcastingContext.SourceTags
                .AsNoTracking()
                .Where(st => st.SourceId == dbYouTubeSource.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return dbYouTubeSource is null ? null : mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
    }

    public async Task<Domain.Models.YouTubeSource?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.VideoId == videoId, cancellationToken);
        
        if (dbYouTubeSource is not null)
        {
            dbYouTubeSource.SourceTags = await broadcastingContext.SourceTags
                .AsNoTracking()
                .Where(st => st.SourceId == dbYouTubeSource.Id && st.SourceType == SourceType)
                .ToListAsync(cancellationToken);
        }
        
        return dbYouTubeSource is null ? null : mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
    }

    public async Task<string?> GetCollectorOwnerOidAsync(CancellationToken cancellationToken = default)
    {
        var ownerOid = await broadcastingContext.YouTubeSources
            .AsNoTracking()
            .Where(source => source.CreatedByEntraOid != string.Empty)
            .OrderByDescending(source => source.LastUpdatedOn)
            .Select(source => source.CreatedByEntraOid)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(ownerOid) ? null : ownerOid;
    }

    private async Task SyncSourceTagsAsync(int sourceId, IList<string> tags, CancellationToken cancellationToken)
    {
        var existing = await broadcastingContext.SourceTags
            .Where(st => st.SourceId == sourceId && st.SourceType == SourceType)
            .ToListAsync(cancellationToken);
        broadcastingContext.SourceTags.RemoveRange(existing);

        if (tags.Count > 0)
        {
            var newTags = tags.Select(tag => new Models.SourceTag
            {
                SourceId = sourceId,
                SourceType = SourceType,
                Tag = tag
            });
            broadcastingContext.SourceTags.AddRange(newTags);
        }

        await broadcastingContext.SaveChangesAsync(cancellationToken);
    }
}

using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class YouTubeSourceDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IYouTubeSourceDataStore
{
    private const string SourceType = "YouTube";

    public async Task<Domain.Models.YouTubeSource> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources
            .Include(y => y.SourceTags)
            .FirstOrDefaultAsync(y => y.Id == primaryKey, cancellationToken);
        return mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
    }

    public async Task<OperationResult<Domain.Models.YouTubeSource>> SaveAsync(Domain.Models.YouTubeSource entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbYouTubeSource = mapper.Map<Models.YouTubeSource>(entity);
            broadcastingContext.Entry(dbYouTubeSource).State =
                dbYouTubeSource.Id == 0 ? EntityState.Added : EntityState.Modified;

            await broadcastingContext.SaveChangesAsync(cancellationToken);

            await SyncSourceTagsAsync(dbYouTubeSource.Id, entity.Tags, cancellationToken);

            var saved = await broadcastingContext.YouTubeSources
                .Include(y => y.SourceTags)
                .FirstOrDefaultAsync(y => y.Id == dbYouTubeSource.Id, cancellationToken);

            return saved is not null
                ? OperationResult<Domain.Models.YouTubeSource>.Success(mapper.Map<Domain.Models.YouTubeSource>(saved))
                : OperationResult<Domain.Models.YouTubeSource>.Failure("Failed to save YouTube source");
        }
        catch (Exception ex)
        {
            return OperationResult<Domain.Models.YouTubeSource>.Failure("An error occurred while saving the YouTube source", ex);
        }
    }

    public async Task<List<Domain.Models.YouTubeSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbYouTubeSources = await broadcastingContext.YouTubeSources
            .Include(y => y.SourceTags)
            .ToListAsync(cancellationToken);
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
                .Include(y => y.SourceTags)
                .FirstOrDefaultAsync(y => y.Id == primaryKey, cancellationToken);
            if (dbYouTubeSource == null) return OperationResult<bool>.Success(true);

            broadcastingContext.SourceTags.RemoveRange(dbYouTubeSource.SourceTags);
            broadcastingContext.YouTubeSources.Remove(dbYouTubeSource);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure("An error occurred while deleting the YouTube source", ex);
        }
    }

    public async Task<Domain.Models.YouTubeSource?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources
            .Include(y => y.SourceTags)
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Url == url, cancellationToken);
        return dbYouTubeSource is null ? null : mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
    }

    public async Task<Domain.Models.YouTubeSource?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources
            .Include(y => y.SourceTags)
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.VideoId == videoId, cancellationToken);
        return dbYouTubeSource is null ? null : mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
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
using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class YouTubeSourceDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IYouTubeSourceDataStore
{
    public async Task<Domain.Models.YouTubeSource> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources.FindAsync(new object[] { primaryKey }, cancellationToken);
        return mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
    }

    public async Task<OperationResult<Domain.Models.YouTubeSource>> SaveAsync(Domain.Models.YouTubeSource entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbYouTubeSource = mapper.Map<Models.YouTubeSource>(entity);
            broadcastingContext.Entry(dbYouTubeSource).State =
                dbYouTubeSource.Id == 0 ? EntityState.Added : EntityState.Modified;

            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            if (result)
            {
                return OperationResult<Domain.Models.YouTubeSource>.Success(mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource));
            }
            return OperationResult<Domain.Models.YouTubeSource>.Failure("Failed to save YouTube source");
        }
        catch (Exception ex)
        {
            return OperationResult<Domain.Models.YouTubeSource>.Failure("An error occurred while saving the YouTube source", ex);
        }
    }

    public async Task<List<Domain.Models.YouTubeSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbYouTubeSources = await broadcastingContext.YouTubeSources.ToListAsync(cancellationToken);
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
            var dbYouTubeSource = await broadcastingContext.YouTubeSources.FindAsync(new object[] { primaryKey }, cancellationToken);
            if (dbYouTubeSource == null) return OperationResult<bool>.Success(true);

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
        var dbYouTubeSource = await broadcastingContext.YouTubeSources.AsNoTracking()
            .FirstOrDefaultAsync(y => y.Url == url, cancellationToken);
        return dbYouTubeSource is null ? null : mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
    }

    public async Task<Domain.Models.YouTubeSource?> GetByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources.AsNoTracking()
            .FirstOrDefaultAsync(y => y.VideoId == videoId, cancellationToken);
        return dbYouTubeSource is null ? null : mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
    }
}
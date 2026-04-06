using AutoMapper;
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

    public async Task<Domain.Models.YouTubeSource> SaveAsync(Domain.Models.YouTubeSource entity, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSource = mapper.Map<Models.YouTubeSource>(entity);
        broadcastingContext.Entry(dbYouTubeSource).State =
            dbYouTubeSource.Id == 0 ? EntityState.Added : EntityState.Modified;

        var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
        }

        throw new ApplicationException("Failed to save YouTube source");
    }

    public async Task<List<Domain.Models.YouTubeSource>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbYouTubeSources = await broadcastingContext.YouTubeSources.ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.YouTubeSource>>(dbYouTubeSources);
    }

    public async Task<bool> DeleteAsync(Domain.Models.YouTubeSource entity, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(entity.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources.FindAsync(new object[] { primaryKey }, cancellationToken);
        if (dbYouTubeSource == null)
        {
            return true;
        }

        broadcastingContext.YouTubeSources.Remove(dbYouTubeSource);
        return await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
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
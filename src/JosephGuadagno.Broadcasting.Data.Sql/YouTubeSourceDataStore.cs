using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class YouTubeSourceDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IYouTubeSourceDataStore
{

    public async Task<Domain.Models.YouTubeSource> GetAsync(int primaryKey)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources.FindAsync(primaryKey);
        return mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
    }

    public async Task<Domain.Models.YouTubeSource> SaveAsync(Domain.Models.YouTubeSource entity)
    {
        var dbYouTubeSource = mapper.Map<Models.YouTubeSource>(entity);
        broadcastingContext.Entry(dbYouTubeSource).State =
            dbYouTubeSource.Id == 0 ? EntityState.Added : EntityState.Modified;

        var result = await broadcastingContext.SaveChangesAsync() != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
        }

        throw new ApplicationException("Failed to save YouTube source");
    }

    public async Task<List<Domain.Models.YouTubeSource>> GetAllAsync()
    {
        var dbYouTubeSources = await broadcastingContext.YouTubeSources.ToListAsync();
        return mapper.Map<List<Domain.Models.YouTubeSource>>(dbYouTubeSources);
    }

    public async Task<bool> DeleteAsync(Domain.Models.YouTubeSource entity)
    {
        return await DeleteAsync(entity.Id);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources.FindAsync(primaryKey);
        if (dbYouTubeSource == null)
        {
            return true;
        }

        broadcastingContext.YouTubeSources.Remove(dbYouTubeSource);
        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<Domain.Models.YouTubeSource?> GetByUrlAsync(string url)
    {
        var dbYouTubeSource = await broadcastingContext.YouTubeSources.AsNoTracking()
            .FirstOrDefaultAsync(y => y.Url == url);
        return dbYouTubeSource is null ? null : mapper.Map<Domain.Models.YouTubeSource>(dbYouTubeSource);
    }
}
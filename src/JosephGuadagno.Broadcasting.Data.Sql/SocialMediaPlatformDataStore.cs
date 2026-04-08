using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class SocialMediaPlatformDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : ISocialMediaPlatformDataStore
{
    public async Task<Domain.Models.SocialMediaPlatform?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var dbPlatform = await broadcastingContext.SocialMediaPlatforms
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return mapper.Map<Domain.Models.SocialMediaPlatform>(dbPlatform);
    }

    public async Task<List<Domain.Models.SocialMediaPlatform>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.SocialMediaPlatforms.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        var dbPlatforms = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.SocialMediaPlatform>>(dbPlatforms);
    }

    public async Task<Domain.Models.SocialMediaPlatform?> AddAsync(Domain.Models.SocialMediaPlatform socialMediaPlatform, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbPlatform = mapper.Map<Models.SocialMediaPlatform>(socialMediaPlatform);
            broadcastingContext.SocialMediaPlatforms.Add(dbPlatform);
            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            if (result)
            {
                return mapper.Map<Domain.Models.SocialMediaPlatform>(dbPlatform);
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<Domain.Models.SocialMediaPlatform?> UpdateAsync(Domain.Models.SocialMediaPlatform socialMediaPlatform, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbPlatform = mapper.Map<Models.SocialMediaPlatform>(socialMediaPlatform);
            broadcastingContext.Entry(dbPlatform).State = EntityState.Modified;
            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            if (result)
            {
                return mapper.Map<Domain.Models.SocialMediaPlatform>(dbPlatform);
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbPlatform = await broadcastingContext.SocialMediaPlatforms
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (dbPlatform == null)
            {
                return false;
            }

            // Soft delete: set IsActive = false
            dbPlatform.IsActive = false;
            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            return result;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

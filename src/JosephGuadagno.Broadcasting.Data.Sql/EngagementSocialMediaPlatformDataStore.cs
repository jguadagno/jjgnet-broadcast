using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class EngagementSocialMediaPlatformDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IEngagementSocialMediaPlatformDataStore
{
    public async Task<List<Domain.Models.EngagementSocialMediaPlatform>> GetByEngagementIdAsync(int engagementId, CancellationToken cancellationToken = default)
    {
        var dbPlatforms = await broadcastingContext.EngagementSocialMediaPlatforms
            .Include(esmp => esmp.SocialMediaPlatform)
            .Where(esmp => esmp.EngagementId == engagementId)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.EngagementSocialMediaPlatform>>(dbPlatforms);
    }

    public async Task<Domain.Models.EngagementSocialMediaPlatform?> AddAsync(Domain.Models.EngagementSocialMediaPlatform engagementSocialMediaPlatform, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbPlatform = mapper.Map<Models.EngagementSocialMediaPlatform>(engagementSocialMediaPlatform);
            broadcastingContext.EngagementSocialMediaPlatforms.Add(dbPlatform);
            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            if (result)
            {
                return mapper.Map<Domain.Models.EngagementSocialMediaPlatform>(dbPlatform);
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(int engagementId, int socialMediaPlatformId, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbPlatform = await broadcastingContext.EngagementSocialMediaPlatforms
                .FirstOrDefaultAsync(esmp => esmp.EngagementId == engagementId && esmp.SocialMediaPlatformId == socialMediaPlatformId, cancellationToken);
            if (dbPlatform == null)
            {
                return false;
            }

            broadcastingContext.EngagementSocialMediaPlatforms.Remove(dbPlatform);
            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            return result;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

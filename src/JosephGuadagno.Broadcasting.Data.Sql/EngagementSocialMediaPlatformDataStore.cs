using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class EngagementSocialMediaPlatformDataStore(
    BroadcastingContext broadcastingContext,
    IMapper mapper,
    ILogger<EngagementSocialMediaPlatformDataStore> logger) : IEngagementSocialMediaPlatformDataStore
{
    public async Task<List<Domain.Models.EngagementSocialMediaPlatform>> GetByEngagementIdAsync(int engagementId, CancellationToken cancellationToken = default)
    {
        var dbPlatforms = await broadcastingContext.EngagementSocialMediaPlatforms
            .Include(esmp => esmp.SocialMediaPlatform)
            .Where(esmp => esmp.EngagementId == engagementId)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.EngagementSocialMediaPlatform>>(dbPlatforms);
    }

    public async Task<Domain.Models.EngagementSocialMediaPlatform?> GetAsync(int engagementId, int platformId, CancellationToken cancellationToken = default)
    {
        var dbPlatform = await broadcastingContext.EngagementSocialMediaPlatforms
            .Include(esmp => esmp.SocialMediaPlatform)
            .FirstOrDefaultAsync(esmp => esmp.EngagementId == engagementId && esmp.SocialMediaPlatformId == platformId, cancellationToken);
        return dbPlatform == null ? null : mapper.Map<Domain.Models.EngagementSocialMediaPlatform>(dbPlatform);
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
        catch (DbUpdateException ex) when (IsDuplicateAssociationException(ex))
        {
            logger.LogWarning(
                ex,
                "Duplicate engagement/platform association detected while saving engagement {EngagementId} and platform {PlatformId}",
                engagementSocialMediaPlatform.EngagementId,
                engagementSocialMediaPlatform.SocialMediaPlatformId);

            throw new DuplicateEngagementSocialMediaPlatformException(
                engagementSocialMediaPlatform.EngagementId,
                engagementSocialMediaPlatform.SocialMediaPlatformId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to add platform {PlatformId} to engagement {EngagementId}",
                engagementSocialMediaPlatform.SocialMediaPlatformId,
                engagementSocialMediaPlatform.EngagementId);

            throw;
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete platform {PlatformId} from engagement {EngagementId}", socialMediaPlatformId, engagementId);
            return false;
        }
    }

    private static bool IsDuplicateAssociationException(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException
        && (sqlException.Number == 2601 || sqlException.Number == 2627);
}

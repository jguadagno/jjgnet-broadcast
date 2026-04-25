using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class SocialMediaPlatformDataStore(BroadcastingContext broadcastingContext, IMapper mapper, ILogger<SocialMediaPlatformDataStore> logger) : ISocialMediaPlatformDataStore
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

    public async Task<Domain.Models.SocialMediaPlatform?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var dbPlatform = await broadcastingContext.SocialMediaPlatforms
            .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower() && p.IsActive, cancellationToken);
        return mapper.Map<Domain.Models.SocialMediaPlatform>(dbPlatform);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add social media platform '{Name}'", socialMediaPlatform.Name);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update social media platform with ID {Id}", socialMediaPlatform.Id);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete (soft) social media platform with ID {Id}", id);
            return false;
        }
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.SocialMediaPlatform>> GetAllAsync(int page, int pageSize, string sortBy = "name", bool sortDescending = false, string? filter = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.SocialMediaPlatform> query = broadcastingContext.SocialMediaPlatforms;

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(lowerFilter));
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "id" => sortDescending ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id),
            _ => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.SocialMediaPlatform>
        {
            Items = mapper.Map<List<Domain.Models.SocialMediaPlatform>>(dbItems),
            TotalCount = totalCount
        };
    }
}

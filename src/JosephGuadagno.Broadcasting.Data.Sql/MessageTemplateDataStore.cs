using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class MessageTemplateDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IMessageTemplateDataStore
{
    public async Task<Domain.Models.MessageTemplate?> GetAsync(int socialMediaPlatformId, string messageType, CancellationToken cancellationToken = default)
    {
        var dbMessageTemplate = await broadcastingContext.MessageTemplates
            .AsNoTracking()
            .Include(mt => mt.SocialMediaPlatform)
            .FirstOrDefaultAsync(mt => mt.SocialMediaPlatformId == socialMediaPlatformId && mt.MessageType == messageType, cancellationToken);
        return dbMessageTemplate is null ? null : mapper.Map<Domain.Models.MessageTemplate>(dbMessageTemplate);
    }

    public async Task<List<Domain.Models.MessageTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbMessageTemplates = await broadcastingContext.MessageTemplates
            .AsNoTracking()
            .Include(mt => mt.SocialMediaPlatform)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.MessageTemplate>>(dbMessageTemplates);
    }

    public async Task<List<Domain.Models.MessageTemplate>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var dbMessageTemplates = await broadcastingContext.MessageTemplates
            .AsNoTracking()
            .Include(mt => mt.SocialMediaPlatform)
            .Where(mt => mt.CreatedByEntraOid == ownerEntraOid)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.MessageTemplate>>(dbMessageTemplates);
    }

    public async Task<Domain.Models.MessageTemplate?> UpdateAsync(Domain.Models.MessageTemplate messageTemplate, CancellationToken cancellationToken = default)
    {
        var existing = await broadcastingContext.MessageTemplates
            .Include(mt => mt.SocialMediaPlatform)
            .FirstOrDefaultAsync(mt => mt.SocialMediaPlatformId == messageTemplate.SocialMediaPlatformId && mt.MessageType == messageTemplate.MessageType, cancellationToken);
        if (existing is null) return null;

        existing.Template = messageTemplate.Template;
        existing.Description = messageTemplate.Description;
        await broadcastingContext.SaveChangesAsync(cancellationToken);
        return mapper.Map<Domain.Models.MessageTemplate>(existing);
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.MessageTemplate>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await broadcastingContext.MessageTemplates.CountAsync(cancellationToken);
        var dbItems = await broadcastingContext.MessageTemplates
            .AsNoTracking()
            .Include(mt => mt.SocialMediaPlatform)
            .OrderBy(mt => mt.SocialMediaPlatformId)
            .ThenBy(mt => mt.MessageType)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.MessageTemplate>
        {
            Items = mapper.Map<List<Domain.Models.MessageTemplate>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.MessageTemplate>> GetAllAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = broadcastingContext.MessageTemplates
            .AsNoTracking()
            .Include(mt => mt.SocialMediaPlatform)
            .Where(mt => mt.CreatedByEntraOid == ownerEntraOid);
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(mt => mt.SocialMediaPlatformId)
            .ThenBy(mt => mt.MessageType)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.MessageTemplate>
        {
            Items = mapper.Map<List<Domain.Models.MessageTemplate>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.MessageTemplate>> GetAllAsync(int page, int pageSize, string sortBy = "messagetype", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.MessageTemplate> query = broadcastingContext.MessageTemplates
            .AsNoTracking()
            .Include(mt => mt.SocialMediaPlatform);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(mt => mt.MessageType.ToLower().Contains(lowerFilter));
        }

        var sortByLower = sortBy?.ToLowerInvariant();
        if (sortByLower == nameof(Models.MessageTemplate.SocialMediaPlatformId).ToLowerInvariant().Replace("socialmedia", "").Replace("id", "id"))
        {
            query = sortDescending ? query.OrderByDescending(mt => mt.SocialMediaPlatformId) : query.OrderBy(mt => mt.SocialMediaPlatformId);
        }
        else
        {
            query = sortDescending
                ? query.OrderByDescending(mt => mt.MessageType).ThenByDescending(mt => mt.SocialMediaPlatformId)
                : query.OrderBy(mt => mt.MessageType).ThenBy(mt => mt.SocialMediaPlatformId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.MessageTemplate>
        {
            Items = mapper.Map<List<Domain.Models.MessageTemplate>>(dbItems),
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.MessageTemplate>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "messagetype", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.MessageTemplate> query = broadcastingContext.MessageTemplates
            .AsNoTracking()
            .Include(mt => mt.SocialMediaPlatform)
            .Where(mt => mt.CreatedByEntraOid == ownerEntraOid);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(mt => mt.MessageType.ToLower().Contains(lowerFilter));
        }

        var sortByLower = sortBy?.ToLowerInvariant();
        if (sortByLower == nameof(Models.MessageTemplate.SocialMediaPlatformId).ToLowerInvariant().Replace("socialmedia", "").Replace("id", "id"))
        {
            query = sortDescending ? query.OrderByDescending(mt => mt.SocialMediaPlatformId) : query.OrderBy(mt => mt.SocialMediaPlatformId);
        }
        else
        {
            query = sortDescending
                ? query.OrderByDescending(mt => mt.MessageType).ThenByDescending(mt => mt.SocialMediaPlatformId)
                : query.OrderBy(mt => mt.MessageType).ThenBy(mt => mt.SocialMediaPlatformId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.MessageTemplate>
        {
            Items = mapper.Map<List<Domain.Models.MessageTemplate>>(dbItems),
            TotalCount = totalCount
        };
    }
}

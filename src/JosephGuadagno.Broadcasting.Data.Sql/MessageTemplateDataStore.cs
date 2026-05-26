using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class MessageTemplateDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IMessageTemplateDataStore
{
    public Task<Domain.Models.MessageTemplate?> GetAsync(int socialMediaPlatformId, string messageType, CancellationToken cancellationToken = default)
        => GetAsync(socialMediaPlatformId, messageType, MessageTemplates.SystemOwnerEntraOid, cancellationToken);

    public async Task<Domain.Models.MessageTemplate?> GetAsync(int socialMediaPlatformId, string messageType, string ownerEntraOid, CancellationToken cancellationToken = default)
    {
        var dbMessageTemplate = await broadcastingContext.MessageTemplates
            .AsNoTracking()
            .Include(mt => mt.SocialMediaPlatform)
            .FirstOrDefaultAsync(mt =>
                mt.SocialMediaPlatformId == socialMediaPlatformId &&
                mt.MessageType == messageType &&
                mt.CreatedByEntraOid == ownerEntraOid,
                cancellationToken);
        return dbMessageTemplate is null ? null : mapper.Map<Domain.Models.MessageTemplate>(dbMessageTemplate);
    }

    public async Task<List<Domain.Models.MessageTemplate>> GetAllDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var dbItems = await broadcastingContext.MessageTemplates
            .AsNoTracking()
            .Include(mt => mt.SocialMediaPlatform)
            .Where(mt => mt.CreatedByEntraOid == MessageTemplates.SystemOwnerEntraOid)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.MessageTemplate>>(dbItems);
    }

    public async Task<Domain.Models.MessageTemplate?> CreateAsync(Domain.Models.MessageTemplate messageTemplate, CancellationToken cancellationToken = default)
    {
        var dbEntity = mapper.Map<Models.MessageTemplate>(messageTemplate);
        broadcastingContext.MessageTemplates.Add(dbEntity);
        await broadcastingContext.SaveChangesAsync(cancellationToken);
        await broadcastingContext.Entry(dbEntity).Reference(mt => mt.SocialMediaPlatform).LoadAsync(cancellationToken);
        return mapper.Map<Domain.Models.MessageTemplate>(dbEntity);
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
        var ownerOid = messageTemplate.CreatedByEntraOid;
        var existing = await broadcastingContext.MessageTemplates
            .Include(mt => mt.SocialMediaPlatform)
            .FirstOrDefaultAsync(mt =>
                mt.SocialMediaPlatformId == messageTemplate.SocialMediaPlatformId &&
                mt.MessageType == messageTemplate.MessageType &&
                mt.CreatedByEntraOid == ownerOid,
                cancellationToken);
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

        query = ApplyFilterAndSort(query, filter, sortBy, sortDescending);
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var distinctOids = dbItems.Select(mt => mt.CreatedByEntraOid).Distinct().ToList();
        var ownerNames = await broadcastingContext.ApplicationUsers
            .AsNoTracking()
            .Where(au => distinctOids.Contains(au.EntraObjectId))
            .ToDictionaryAsync(au => au.EntraObjectId, au => au.DisplayName, cancellationToken);

        var items = dbItems.Select(mt =>
        {
            var domain = mapper.Map<Domain.Models.MessageTemplate>(mt);
            domain.OwnerDisplayName = ownerNames.GetValueOrDefault(mt.CreatedByEntraOid);
            return domain;
        }).ToList();

        return new Domain.Models.PagedResult<Domain.Models.MessageTemplate>
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<Domain.Models.PagedResult<Domain.Models.MessageTemplate>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "messagetype", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Models.MessageTemplate> query = broadcastingContext.MessageTemplates
            .AsNoTracking()
            .Include(mt => mt.SocialMediaPlatform)
            .Where(mt => mt.CreatedByEntraOid == ownerEntraOid);

        query = ApplyFilterAndSort(query, filter, sortBy, sortDescending);
        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new Domain.Models.PagedResult<Domain.Models.MessageTemplate>
        {
            Items = mapper.Map<List<Domain.Models.MessageTemplate>>(dbItems),
            TotalCount = totalCount
        };
    }

    private static IQueryable<Models.MessageTemplate> ApplyFilterAndSort(
        IQueryable<Models.MessageTemplate> query,
        string? filter,
        string sortBy,
        bool sortDescending)
    {
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLowerInvariant();
            query = query.Where(mt => mt.MessageType.ToLower().Contains(lowerFilter));
        }

        query = sortBy?.ToLowerInvariant() == "socialmediaplatformid"
            ? (sortDescending ? query.OrderByDescending(mt => mt.SocialMediaPlatformId) : query.OrderBy(mt => mt.SocialMediaPlatformId))
            : (sortDescending
                ? query.OrderByDescending(mt => mt.MessageType).ThenByDescending(mt => mt.SocialMediaPlatformId)
                : query.OrderBy(mt => mt.MessageType).ThenBy(mt => mt.SocialMediaPlatformId));

        return query;
    }
}

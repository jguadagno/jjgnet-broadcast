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
            .FirstOrDefaultAsync(mt => mt.SocialMediaPlatformId == socialMediaPlatformId && mt.MessageType == messageType, cancellationToken);
        return dbMessageTemplate is null ? null : mapper.Map<Domain.Models.MessageTemplate>(dbMessageTemplate);
    }

    public async Task<List<Domain.Models.MessageTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbMessageTemplates = await broadcastingContext.MessageTemplates.AsNoTracking().ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.MessageTemplate>>(dbMessageTemplates);
    }

    public async Task<Domain.Models.MessageTemplate?> UpdateAsync(Domain.Models.MessageTemplate messageTemplate, CancellationToken cancellationToken = default)
    {
        var existing = await broadcastingContext.MessageTemplates
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
}

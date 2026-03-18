using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class MessageTemplateDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IMessageTemplateDataStore
{
    public async Task<Domain.Models.MessageTemplate?> GetAsync(string platform, string messageType)
    {
        var dbMessageTemplate = await broadcastingContext.MessageTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(mt => mt.Platform == platform && mt.MessageType == messageType);
        return dbMessageTemplate is null ? null : mapper.Map<Domain.Models.MessageTemplate>(dbMessageTemplate);
    }

    public async Task<List<Domain.Models.MessageTemplate>> GetAllAsync()
    {
        var dbMessageTemplates = await broadcastingContext.MessageTemplates.AsNoTracking().ToListAsync();
        return mapper.Map<List<Domain.Models.MessageTemplate>>(dbMessageTemplates);
    }

    public async Task<Domain.Models.MessageTemplate?> UpdateAsync(Domain.Models.MessageTemplate messageTemplate)
    {
        var existing = await broadcastingContext.MessageTemplates
            .FirstOrDefaultAsync(mt => mt.Platform == messageTemplate.Platform && mt.MessageType == messageTemplate.MessageType);
        if (existing is null) return null;

        existing.Template = messageTemplate.Template;
        existing.Description = messageTemplate.Description;
        await broadcastingContext.SaveChangesAsync();
        return mapper.Map<Domain.Models.MessageTemplate>(existing);
    }
}

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
}

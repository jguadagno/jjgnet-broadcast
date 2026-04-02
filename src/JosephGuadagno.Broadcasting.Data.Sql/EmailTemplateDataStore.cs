using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class EmailTemplateDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IEmailTemplateDataStore
{
    public async Task<Domain.Models.EmailTemplate?> GetByIdAsync(int id)
    {
        var dbEmailTemplate = await broadcastingContext.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(et => et.Id == id);
        return dbEmailTemplate is null ? null : mapper.Map<Domain.Models.EmailTemplate>(dbEmailTemplate);
    }

    public async Task<Domain.Models.EmailTemplate?> GetByNameAsync(string name)
    {
        var dbEmailTemplate = await broadcastingContext.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(et => et.Name == name);
        return dbEmailTemplate is null ? null : mapper.Map<Domain.Models.EmailTemplate>(dbEmailTemplate);
    }

    public async Task<List<Domain.Models.EmailTemplate>> GetAllAsync()
    {
        var dbEmailTemplates = await broadcastingContext.EmailTemplates
            .AsNoTracking()
            .OrderBy(et => et.Name)
            .ToListAsync();
        return mapper.Map<List<Domain.Models.EmailTemplate>>(dbEmailTemplates);
    }
}

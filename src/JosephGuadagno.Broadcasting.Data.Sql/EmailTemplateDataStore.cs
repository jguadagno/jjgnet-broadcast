using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class EmailTemplateDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : IEmailTemplateDataStore
{
    public async Task<Domain.Models.EmailTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dbEmailTemplate = await broadcastingContext.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(et => et.Id == id, cancellationToken);
        return dbEmailTemplate is null ? null : mapper.Map<Domain.Models.EmailTemplate>(dbEmailTemplate);
    }

    public async Task<Domain.Models.EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var dbEmailTemplate = await broadcastingContext.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(et => et.Name == name, cancellationToken);
        return dbEmailTemplate is null ? null : mapper.Map<Domain.Models.EmailTemplate>(dbEmailTemplate);
    }

    public async Task<List<Domain.Models.EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbEmailTemplates = await broadcastingContext.EmailTemplates
            .AsNoTracking()
            .OrderBy(et => et.Name)
            .ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.EmailTemplate>>(dbEmailTemplates);
    }
}

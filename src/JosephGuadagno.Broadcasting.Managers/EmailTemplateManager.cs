using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers;

public class EmailTemplateManager : IEmailTemplateManager
{
    private readonly IEmailTemplateDataStore _dataStore;
    private readonly ILogger<EmailTemplateManager> _logger;

    public EmailTemplateManager(IEmailTemplateDataStore dataStore, ILogger<EmailTemplateManager> logger)
    {
        _dataStore = dataStore;
        _logger = logger;
    }

    public async Task<EmailTemplate?> GetTemplateAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dataStore.GetByIdAsync(id, cancellationToken);
    }

    public async Task<EmailTemplate?> GetTemplateAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dataStore.GetByNameAsync(name, cancellationToken);
    }

    public async Task<List<EmailTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _dataStore.GetAllAsync(cancellationToken);
    }
}

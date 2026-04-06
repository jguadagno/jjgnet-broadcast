using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEmailTemplateManager
{
    Task<EmailTemplate?> GetTemplateAsync(int id, CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetTemplateAsync(string name, CancellationToken cancellationToken = default);
    Task<List<EmailTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);
}

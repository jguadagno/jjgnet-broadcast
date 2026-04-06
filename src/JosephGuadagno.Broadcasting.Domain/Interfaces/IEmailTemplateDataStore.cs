using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEmailTemplateDataStore
{
    Task<EmailTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
}

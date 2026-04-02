using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEmailTemplateDataStore
{
    Task<EmailTemplate?> GetByIdAsync(int id);
    Task<EmailTemplate?> GetByNameAsync(string name);
    Task<List<EmailTemplate>> GetAllAsync();
}

using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEmailTemplateManager
{
    Task<EmailTemplate?> GetTemplateAsync(int id);
    Task<EmailTemplate?> GetTemplateAsync(string name);
    Task<List<EmailTemplate>> GetAllTemplatesAsync();
}

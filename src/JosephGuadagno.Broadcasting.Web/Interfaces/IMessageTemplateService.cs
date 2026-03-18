using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IMessageTemplateService
{
    Task<List<MessageTemplate>?> GetAllAsync();
    Task<MessageTemplate?> GetAsync(string platform, string messageType);
    Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate);
}

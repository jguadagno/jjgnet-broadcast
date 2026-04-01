using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IMessageTemplateDataStore
{
    Task<MessageTemplate?> GetAsync(string platform, string messageType);
    Task<List<MessageTemplate>> GetAllAsync();
    Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate);
    
    Task<PagedResult<MessageTemplate>> GetAllAsync(int page, int pageSize);
}

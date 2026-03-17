using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IMessageTemplateDataStore
{
    Task<MessageTemplate?> GetAsync(string platform, string messageType);
    Task<List<MessageTemplate>> GetAllAsync();
}

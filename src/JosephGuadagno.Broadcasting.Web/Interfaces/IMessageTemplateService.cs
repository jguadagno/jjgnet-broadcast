using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IMessageTemplateService
{
    Task<List<MessageTemplate>?> GetAllAsync(int? page = 1, int? pageSize = 25);
    Task<MessageTemplate?> GetAsync(string platform, string messageType);
    Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate);
}
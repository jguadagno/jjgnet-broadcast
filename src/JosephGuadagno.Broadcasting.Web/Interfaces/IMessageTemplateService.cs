using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface IMessageTemplateService
{
    Task<List<MessageTemplate>?> GetAllAsync(int? page = Pagination.DefaultPage, int? pageSize = Pagination.DefaultPageSize);
    Task<MessageTemplate?> GetAsync(string platform, string messageType);
    Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate);
}
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IMessageTemplateDataStore
{
    Task<MessageTemplate?> GetAsync(int socialMediaPlatformId, string messageType, CancellationToken cancellationToken = default);
    Task<List<MessageTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate, CancellationToken cancellationToken = default);
    
    Task<List<MessageTemplate>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<PagedResult<MessageTemplate>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<MessageTemplate>> GetAllAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default);
}

using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IMessageTemplateDataStore
{
    /// <summary>Gets the system-default template (CreatedByEntraOid = "").</summary>
    Task<MessageTemplate?> GetAsync(int socialMediaPlatformId, string messageType, CancellationToken cancellationToken = default);
    /// <summary>Gets a template owned by a specific user (or system default when ownerEntraOid is "").</summary>
    Task<MessageTemplate?> GetAsync(int socialMediaPlatformId, string messageType, string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<List<MessageTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MessageTemplate?> UpdateAsync(MessageTemplate messageTemplate, CancellationToken cancellationToken = default);
    Task<MessageTemplate?> CreateAsync(MessageTemplate messageTemplate, CancellationToken cancellationToken = default);
    Task<List<MessageTemplate>> GetAllDefaultsAsync(CancellationToken cancellationToken = default);
    
    Task<List<MessageTemplate>> GetAllAsync(string ownerEntraOid, CancellationToken cancellationToken = default);
    Task<PagedResult<MessageTemplate>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<MessageTemplate>> GetAllAsync(string ownerEntraOid, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<MessageTemplate>> GetAllAsync(int page, int pageSize, string sortBy = "messagetype", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
    Task<PagedResult<MessageTemplate>> GetAllAsync(string ownerEntraOid, int page, int pageSize, string sortBy = "messagetype", bool sortDescending = false, string? filter = null, CancellationToken cancellationToken = default);
}

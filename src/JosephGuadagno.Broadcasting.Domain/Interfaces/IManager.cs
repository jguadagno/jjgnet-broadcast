namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IManager<T> where T : class
{
    public Task<T> GetAsync(int primaryKey, CancellationToken cancellationToken = default);
    public Task<T> SaveAsync(T entity, CancellationToken cancellationToken = default);
    public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    public Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);
    public Task<bool> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default);
}
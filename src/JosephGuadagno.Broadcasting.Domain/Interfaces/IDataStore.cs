namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IDataStore<T> where T: class
{
    public Task<T> GetAsync(int primaryKey, CancellationToken cancellationToken = default);
    public Task<OperationResult<T>> SaveAsync(T entity, CancellationToken cancellationToken = default);
    public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    public Task<OperationResult<bool>> DeleteAsync(T entity, CancellationToken cancellationToken = default);
    public Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default);
}
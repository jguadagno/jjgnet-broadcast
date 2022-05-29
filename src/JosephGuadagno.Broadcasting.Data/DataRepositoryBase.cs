using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Data;

public class DataRepositoryBase<T>: IDataRepository<T> where T: class
{
    private readonly IDataStore<T> _dataStore;
        
    public DataRepositoryBase(IDataStore<T> dataStore)
    {
        _dataStore = dataStore;
    }

    public virtual async Task<T> GetAsync(int primaryKey)
    {
        return await _dataStore.GetAsync(primaryKey);
    }

    public virtual async Task<T> SaveAsync(T entity)
    {
        return await _dataStore.SaveAsync(entity);
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        return await _dataStore.GetAllAsync();
    }

    public virtual async Task<bool> DeleteAsync(T entity)
    {
        return await _dataStore.DeleteAsync(entity);
    }

    public virtual async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _dataStore.DeleteAsync(primaryKey);
    }
}
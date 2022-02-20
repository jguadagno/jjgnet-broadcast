using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IDataStore<T> where T: class
{
    public Task<T> GetAsync(int primaryKey);
    public Task<bool> SaveAsync(T talk);
    public Task<bool> SaveAllAsync(List<T> talks);
    public Task<List<T>> GetAllAsync();
    public Task<bool> DeleteAsync(T entity);
    public Task<bool> DeleteAsync(int primaryKey);
}
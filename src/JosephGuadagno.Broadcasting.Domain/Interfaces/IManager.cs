using System.Collections.Generic;
using System.Threading.Tasks;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IManager<T> where T : class
{
    public Task<T> GetAsync(int primaryKey);
    public Task<T> SaveAsync(T entity);
    public Task<List<T>> GetAllAsync();
    public Task<bool> DeleteAsync(T entity);
    public Task<bool> DeleteAsync(int primaryKey);
}
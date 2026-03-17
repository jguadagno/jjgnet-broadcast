using System.Collections.Generic;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class TokenRefreshManager : ITokenRefreshManager
{
    private readonly ITokenRefreshDataStore _tokenRefreshDataStore;

    public TokenRefreshManager(ITokenRefreshDataStore tokenRefreshDataStore)
    {
        _tokenRefreshDataStore = tokenRefreshDataStore;
    }

    public async Task<TokenRefresh> GetAsync(int primaryKey)
    {
        return await _tokenRefreshDataStore.GetAsync(primaryKey);
    }

    public async Task<TokenRefresh> SaveAsync(TokenRefresh entity)
    {
        return await _tokenRefreshDataStore.SaveAsync(entity);
    }

    public async Task<List<TokenRefresh>> GetAllAsync()
    {
        return await _tokenRefreshDataStore.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(TokenRefresh entity)
    {
        return await _tokenRefreshDataStore.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _tokenRefreshDataStore.DeleteAsync(primaryKey);
    }

    public async Task<TokenRefresh?> GetByNameAsync(string name)
    {
        return await _tokenRefreshDataStore.GetByNameAsync(name);
    }
}
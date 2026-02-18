using System.Collections.Generic;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class TokenRefreshManager : ITokenRefreshManager
{
    private readonly ITokenRefreshRepository _tokenRefreshRepository;

    public TokenRefreshManager(ITokenRefreshRepository tokenRefreshRepository)
    {
        _tokenRefreshRepository = tokenRefreshRepository;
    }

    public async Task<TokenRefresh> GetAsync(int primaryKey)
    {
        return await _tokenRefreshRepository.GetAsync(primaryKey);
    }

    public async Task<TokenRefresh> SaveAsync(TokenRefresh entity)
    {
        return await _tokenRefreshRepository.SaveAsync(entity);
    }

    public async Task<List<TokenRefresh>> GetAllAsync()
    {
        return await _tokenRefreshRepository.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(TokenRefresh entity)
    {
        return await _tokenRefreshRepository.DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        return await _tokenRefreshRepository.DeleteAsync(primaryKey);
    }

    public async Task<TokenRefresh?> GetByNameAsync(string name)
    {
        return await _tokenRefreshRepository.GetByNameAsync(name);
    }
}
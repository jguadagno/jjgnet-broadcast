using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain;
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

    public async Task<TokenRefresh> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _tokenRefreshDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<TokenRefresh>> SaveAsync(TokenRefresh entity, CancellationToken cancellationToken = default)
    {
        return await _tokenRefreshDataStore.SaveAsync(entity, cancellationToken);
    }

    public async Task<List<TokenRefresh>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _tokenRefreshDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(TokenRefresh entity, CancellationToken cancellationToken = default)
    {
        return await _tokenRefreshDataStore.DeleteAsync(entity, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await _tokenRefreshDataStore.DeleteAsync(primaryKey, cancellationToken);
    }

    public async Task<TokenRefresh?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _tokenRefreshDataStore.GetByNameAsync(name, cancellationToken);
    }
}
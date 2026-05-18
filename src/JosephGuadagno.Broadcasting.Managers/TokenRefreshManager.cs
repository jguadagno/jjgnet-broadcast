using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Managers;

public class TokenRefreshManager(ITokenRefreshDataStore tokenRefreshDataStore) : ITokenRefreshManager
{
	public async Task<TokenRefresh> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await tokenRefreshDataStore.GetAsync(primaryKey, cancellationToken);
    }

    public async Task<OperationResult<TokenRefresh>> SaveAsync(TokenRefresh entity, CancellationToken cancellationToken = default)
    {
        return await tokenRefreshDataStore.SaveAsync(entity, cancellationToken);
    }

    public async Task<List<TokenRefresh>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await tokenRefreshDataStore.GetAllAsync(cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(TokenRefresh entity, CancellationToken cancellationToken = default)
    {
        return await tokenRefreshDataStore.DeleteAsync(entity, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        return await tokenRefreshDataStore.DeleteAsync(primaryKey, cancellationToken);
    }

    public async Task<TokenRefresh?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await tokenRefreshDataStore.GetByNameAsync(name, cancellationToken);
    }
}
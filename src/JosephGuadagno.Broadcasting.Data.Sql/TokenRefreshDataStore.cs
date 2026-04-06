using AutoMapper;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class TokenRefreshDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : ITokenRefreshDataStore
{
    public async Task<Domain.Models.TokenRefresh> GetAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        var dbTokenRefresh = await broadcastingContext.TokenRefreshes.FindAsync(new object[] { primaryKey }, cancellationToken);
        return mapper.Map<Domain.Models.TokenRefresh>(dbTokenRefresh);
    }

    public async Task<OperationResult<Domain.Models.TokenRefresh>> SaveAsync(Domain.Models.TokenRefresh entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbTokenRefresh = mapper.Map<Models.TokenRefresh>(entity);
            broadcastingContext.Entry(dbTokenRefresh).State =
                dbTokenRefresh.Id == 0 ? EntityState.Added : EntityState.Modified;

            var result = await broadcastingContext.SaveChangesAsync(cancellationToken) != 0;
            if (result)
            {
                return OperationResult<Domain.Models.TokenRefresh>.Success(mapper.Map<Domain.Models.TokenRefresh>(dbTokenRefresh));
            }
            return OperationResult<Domain.Models.TokenRefresh>.Failure("Failed to save token refresh");
        }
        catch (Exception ex)
        {
            return OperationResult<Domain.Models.TokenRefresh>.Failure("An error occurred while saving the token refresh", ex);
        }
    }

    public async Task<List<Domain.Models.TokenRefresh>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbTokenRefreshes = await broadcastingContext.TokenRefreshes.ToListAsync(cancellationToken);
        return mapper.Map<List<Domain.Models.TokenRefresh>>(dbTokenRefreshes);
    }

    public async Task<OperationResult<bool>> DeleteAsync(Domain.Models.TokenRefresh entity, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(entity.Id, cancellationToken);
    }

    public async Task<OperationResult<bool>> DeleteAsync(int primaryKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbTokenRefresh = await broadcastingContext.TokenRefreshes.FindAsync(new object[] { primaryKey }, cancellationToken);
            if (dbTokenRefresh == null) return OperationResult<bool>.Success(true);

            broadcastingContext.TokenRefreshes.Remove(dbTokenRefresh);
            await broadcastingContext.SaveChangesAsync(cancellationToken);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure("An error occurred while deleting the token refresh", ex);
        }
    }

    public async Task<Domain.Models.TokenRefresh?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var dbTokenRefresh = await broadcastingContext.TokenRefreshes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
        return dbTokenRefresh is null ? null : mapper.Map<Domain.Models.TokenRefresh>(dbTokenRefresh);
    }
}
using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JosephGuadagno.Broadcasting.Data.Sql;

public class TokenRefreshDataStore(BroadcastingContext broadcastingContext, IMapper mapper) : ITokenRefreshDataStore
{
    public async Task<Domain.Models.TokenRefresh> GetAsync(int primaryKey)
    {
        var dbTokenRefresh = await broadcastingContext.TokenRefreshes.FindAsync(primaryKey);
        return mapper.Map<Domain.Models.TokenRefresh>(dbTokenRefresh);
    }

    public async Task<Domain.Models.TokenRefresh> SaveAsync(Domain.Models.TokenRefresh entity)
    {
        var dbTokenRefresh = mapper.Map<Models.TokenRefresh>(entity);
        broadcastingContext.Entry(dbTokenRefresh).State =
            dbTokenRefresh.Id == 0 ? EntityState.Added : EntityState.Modified;

        var result = await broadcastingContext.SaveChangesAsync() != 0;
        if (result)
        {
            return mapper.Map<Domain.Models.TokenRefresh>(dbTokenRefresh);
        }

        throw new ApplicationException("Failed to save token refresh");
    }

    public async Task<List<Domain.Models.TokenRefresh>> GetAllAsync()
    {
        var dbTokenRefreshes = await broadcastingContext.TokenRefreshes.ToListAsync();
        return mapper.Map<List<Domain.Models.TokenRefresh>>(dbTokenRefreshes);
    }

    public async Task<bool> DeleteAsync(Domain.Models.TokenRefresh entity)
    {
        return await DeleteAsync(entity.Id);
    }

    public async Task<bool> DeleteAsync(int primaryKey)
    {
        var dbTokenRefresh = await broadcastingContext.TokenRefreshes.FindAsync(primaryKey);
        if (dbTokenRefresh == null)
        {
            return true;
        }

        broadcastingContext.TokenRefreshes.Remove(dbTokenRefresh);
        return await broadcastingContext.SaveChangesAsync() != 0;
    }

    public async Task<Domain.Models.TokenRefresh?> GetByNameAsync(string name)
    {
        var dbTokenRefresh = await broadcastingContext.TokenRefreshes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == name);
        return dbTokenRefresh is null ? null : mapper.Map<Domain.Models.TokenRefresh>(dbTokenRefresh);
    }
}
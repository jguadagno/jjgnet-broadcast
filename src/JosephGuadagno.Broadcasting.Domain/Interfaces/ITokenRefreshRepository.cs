using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ITokenRefreshRepository : IDataRepository<TokenRefresh>
{
    public Task<TokenRefresh?> GetByNameAsync(string name);
}

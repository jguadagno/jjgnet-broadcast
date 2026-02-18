using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ITokenRefreshManager : IManager<TokenRefresh>
{
    public Task<TokenRefresh?> GetByNameAsync(string name);
}

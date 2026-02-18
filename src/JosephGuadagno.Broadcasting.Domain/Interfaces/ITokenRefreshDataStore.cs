using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ITokenRefreshDataStore : IDataStore<Domain.Models.TokenRefresh>
{
    public Task<Domain.Models.TokenRefresh?> GetByNameAsync(string name);
}

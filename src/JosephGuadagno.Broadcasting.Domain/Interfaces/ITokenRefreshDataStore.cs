namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface ITokenRefreshDataStore : IDataStore<Models.TokenRefresh>
{
    public Task<Models.TokenRefresh?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

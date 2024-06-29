using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data.Repositories;

public class TokenRefreshRepository(string connectionString)
    : TableRepository<TokenRefreshInfo>(connectionString, Domain.Constants.Tables.TokenRefresh);
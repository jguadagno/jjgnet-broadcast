using Microsoft.Azure.Cosmos.Table;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class TokenRefreshBase: TableEntity
{
    public TokenRefreshBase()
    {
        PartitionKey = Constants.Tables.TokenRefresh;
    }
}
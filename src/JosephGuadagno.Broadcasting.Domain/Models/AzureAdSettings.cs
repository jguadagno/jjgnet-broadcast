using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class AzureAdSettings: IAzureAdSettings
{
    public string Instance { get; init; }
    public string Domain { get; init; }
    public string ClientId { get; init; }
    public string TenantId { get; init; }
    public string CallbackPath { get; init; }
    public string SignedOutCallbackPath { get; init; }
}
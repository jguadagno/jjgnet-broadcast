using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class AzureAdSettings: IAzureAdSettings
{
    public required string Instance { get; init; }
    public required string Domain { get; init; }
    public required string ClientId { get; init; }
    public required string TenantId { get; init; }
    public required string CallbackPath { get; init; }
    public required string SignedOutCallbackPath { get; init; }
}
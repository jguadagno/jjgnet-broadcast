namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IAzureAdSettings
{
    public string Instance { get; init; }
    public string Domain { get; init; }
    public string ClientId { get; init; }
    public string TenantId { get; init; }
    public string CallbackPath { get; init; }
    public string SignedOutCallbackPath { get; init; }
}
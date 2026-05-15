namespace JosephGuadagno.Broadcasting.Domain.Utilities;

/// <summary>
/// Identifies the ownership category of a Key Vault secret.
/// </summary>
public enum KeyVaultSecretOwnerType
{
    /// <summary>A secret belonging to a social-media publisher.</summary>
    Publisher = 0,

    /// <summary>A secret belonging to a content collector.</summary>
    Collector = 1,
}

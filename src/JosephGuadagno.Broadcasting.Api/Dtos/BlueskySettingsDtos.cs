namespace JosephGuadagno.Broadcasting.Api.Dtos;

/// <summary>Request DTO for creating or updating Bluesky publisher settings.</summary>
public class BlueskySettingsRequest
{
    /// <summary>Gets or sets whether Bluesky publishing is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the Bluesky handle/username.</summary>
    public string? UserName { get; set; }

    /// <summary>Gets or sets the Bluesky app password. Null or empty means keep existing value.</summary>
    public string? AppPassword { get; set; }
}

/// <summary>Response DTO for Bluesky publisher settings.</summary>
public class BlueskySettingsResponse
{
    /// <summary>Gets or sets the unique record identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the Entra Object ID of the settings owner.</summary>
    public string CreatedByEntraOid { get; set; } = string.Empty;

    /// <summary>Gets or sets whether Bluesky publishing is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the Bluesky handle/username.</summary>
    public string? UserName { get; set; }

    /// <summary>Gets or sets whether an app password is stored in Key Vault.</summary>
    public bool HasAppPassword { get; set; }

    /// <summary>Gets or sets when these settings were created.</summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>Gets or sets when these settings were last updated.</summary>
    public DateTimeOffset LastUpdatedOn { get; set; }
}

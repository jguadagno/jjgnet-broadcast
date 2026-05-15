using ISettings = JosephGuadagno.Broadcasting.Api.Interfaces.ISettings;

namespace JosephGuadagno.Broadcasting.Api.Models;

/// <summary>
/// Concrete implementation of <see cref="ISettings"/> that holds runtime configuration values
/// for the Broadcasting API, bound from application configuration at startup.
/// </summary>
public class Settings : ISettings
{
    /// <summary>
     /// The Client Id for the Azure AD Scalar App.
     /// </summary>
    public required string ScalarClientId { get; set; }
}

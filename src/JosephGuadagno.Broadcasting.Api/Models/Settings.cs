using ISettings = JosephGuadagno.Broadcasting.Api.Interfaces.ISettings;

namespace JosephGuadagno.Broadcasting.Api.Models;

public class Settings : ISettings
{
    /// <summary>
    /// The URL of the API scope used for authentication and authorization purposes.
    /// </summary>
    public required string ApiScopeUrl { get; set; }

    /// <summary>
    /// The Client Id for the Azure AD Scalar App.
    /// </summary>
    public required string ScalarClientId { get; set; }
}
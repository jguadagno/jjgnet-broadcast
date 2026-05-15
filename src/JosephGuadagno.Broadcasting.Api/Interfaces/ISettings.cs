namespace JosephGuadagno.Broadcasting.Api.Interfaces;

/// <summary>
/// Defines the application settings contract for the Broadcasting API,
/// exposing configuration values required by the API at runtime.
/// </summary>
public interface ISettings
{
    /// <summary>
    /// The Client Id for the Azure AD Scalar App.
    /// </summary>
    public string ScalarClientId { get; set; }
}

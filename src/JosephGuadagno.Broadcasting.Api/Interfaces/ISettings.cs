namespace JosephGuadagno.Broadcasting.Api.Interfaces;

public interface ISettings
{
    /// <summary>
    /// The Client Id for the Azure AD Scalar App.
    /// </summary>
    public string ScalarClientId { get; set; }
}

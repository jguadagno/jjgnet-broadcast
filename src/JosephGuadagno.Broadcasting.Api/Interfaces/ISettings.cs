namespace JosephGuadagno.Broadcasting.Api.Interfaces;

public interface ISettings
{
    /// <summary>
    /// The URL of the API scope used for authentication and authorization purposes.
    /// </summary>
    public string ApiScopeUrl { get; set; }

    /// <summary>
    /// The Client Id for the Azure AD Scalar App.
    /// </summary>
    public string ScalarClientId { get; set; }

    /// <summary>
    /// The storage account connection string.
    /// </summary>
    /// <remarks>This is used for the Azure Table Storage.</remarks>
    public string StorageAccount { get; set; }
}
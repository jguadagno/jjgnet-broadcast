namespace JosephGuadagno.Broadcasting.Web.Interfaces;

public interface ISettings
{
    /// <summary>
    /// The root URL for serving static content in the Web application.
    /// </summary>
    public string StaticContentRootUrl { get; set; }
}
namespace JosephGuadagno.Broadcasting.Web.Exceptions;

/// <summary>
/// Thrown when the Microsoft Authentication Library throws an exception that requires the User Interface to be displayed
/// </summary>
public class WebApiMsalUiRequiredException:Exception
{
    /// <summary>
    /// The exception with a message that contains the new Url
    /// </summary>
    /// <param name="message">The Url to redirect to</param>
    public WebApiMsalUiRequiredException(string message) : base(message) { }
}
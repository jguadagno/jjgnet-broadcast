using JosephGuadagno.Broadcasting.Domain.Exceptions;

namespace JosephGuadagno.Broadcasting.Managers.Facebook.Exceptions;

public class FacebookPostException : BroadcastingException
{
    public FacebookPostException(string message) : base(message) { }

    public FacebookPostException(string message, Exception innerException)
        : base(message, innerException) { }

    public FacebookPostException(string message, int? apiErrorCode, string? apiErrorMessage)
        : base(message, apiErrorCode, apiErrorMessage) { }
}

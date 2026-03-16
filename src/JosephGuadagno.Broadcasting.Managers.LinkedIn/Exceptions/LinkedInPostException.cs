using JosephGuadagno.Broadcasting.Domain.Exceptions;

namespace JosephGuadagno.Broadcasting.Managers.LinkedIn.Exceptions;

public class LinkedInPostException : BroadcastingException
{
    public LinkedInPostException(string message) : base(message) { }

    public LinkedInPostException(string message, Exception innerException)
        : base(message, innerException) { }

    public LinkedInPostException(string message, int? apiErrorCode, string? apiErrorMessage)
        : base(message, apiErrorCode, apiErrorMessage) { }
}

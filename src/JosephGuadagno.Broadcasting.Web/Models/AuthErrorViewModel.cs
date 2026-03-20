namespace JosephGuadagno.Broadcasting.Web.Models;

public class AuthErrorViewModel
{
    public string Message { get; set; } = "An error occurred during authentication.";
    public string RetryUrl { get; set; } = "/Account/SignIn";
    public string? SupportEmail { get; set; }
}

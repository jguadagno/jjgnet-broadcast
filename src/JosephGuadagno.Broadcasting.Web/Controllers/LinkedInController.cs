using System.Security.Claims;
using System.Text.Json;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Utilities;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Models.LinkedIn;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Handles the LinkedIn OAuth2 flow
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.RequireContributor)]
public class LinkedInController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly IUserOAuthTokenManager _userOAuthTokenManager;
    private readonly ISocialMediaPlatformManager _socialMediaPlatformManager;
    private readonly LinkedInSettings _linkedInSettings;
    private readonly ILogger<LinkedInController> _logger;

    const string State = "state";

    /// <summary>
    /// Works with the LinkedIn OAuth flow to acquire and store per-user tokens
    /// </summary>
    public LinkedInController(
        HttpClient httpClient,
        IUserOAuthTokenManager userOAuthTokenManager,
        ISocialMediaPlatformManager socialMediaPlatformManager,
        IOptions<LinkedInSettings> linkedInSettingsOptions,
        ILogger<LinkedInController> logger)
    {
        _httpClient = httpClient;
        _userOAuthTokenManager = userOAuthTokenManager;
        _socialMediaPlatformManager = socialMediaPlatformManager;
        _linkedInSettings = linkedInSettingsOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Shows current per-user LinkedIn token status (masked — never raw token value)
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var ownerOid = User.FindFirstValue("oid");
        if (string.IsNullOrWhiteSpace(ownerOid))
        {
            _logger.LogWarning("User OID claim is missing on LinkedIn Index");
            return View(new SavedTokenInfo { HasToken = false });
        }

        var platform = await _socialMediaPlatformManager.GetByNameAsync("LinkedIn");
        if (platform is null)
        {
            _logger.LogWarning("LinkedIn platform not found in database");
            return View(new SavedTokenInfo { HasToken = false });
        }

        var token = await _userOAuthTokenManager.GetByUserAndPlatformAsync(ownerOid, platform.Id);

        if (token is null)
        {
            return View(new SavedTokenInfo { HasToken = false, PlatformId = platform.Id });
        }

        // Mask the token — never return raw value to the view
        var masked = token.AccessToken.Length > 8
            ? $"{token.AccessToken[..4]}...{token.AccessToken[^4..]}"
            : "****";

        return View(new SavedTokenInfo
        {
            HasToken = true,
            MaskedAccessToken = masked,
            ExpiresOn = token.AccessTokenExpiresAt,
            PlatformId = platform.Id
        });
    }

    /// <summary>
    /// Starts the LinkedIn OAuth2 authorization code flow
    /// </summary>
    public async Task<IActionResult> RefreshToken()
    {
        // Build the URL to redirect the user to
        var state = Guid.NewGuid().ToString();
        HttpContext.Session.SetString(State, state);

        var callbackUrl = Url.Action("Callback", "LinkedIn", null, Request.Scheme) ?? string.Empty;
        if (string.IsNullOrEmpty(callbackUrl))
        {
            _logger.LogError("Callback URL is missing");
            return ViewBag["Message"] = "Callback URL is missing.";
        }

        var url = $"{_linkedInSettings.AuthorizationUrl}?response_type=code&client_id={_linkedInSettings.ClientId}&redirect_uri={callbackUrl}&state={state}&scope={_linkedInSettings.Scopes}";

        return await Task.FromResult(Redirect(url));
    }

    /// <summary>
    /// Handles the OAuth2 callback from LinkedIn and stores the token per-user
    /// </summary>
    public async Task<IActionResult> Callback(CancellationToken cancellationToken = default)
    {
        // Read the code and state from the query string
        var code = HttpContext.Request.Query["code"].FirstOrDefault();
        var state = HttpContext.Request.Query["state"].FirstOrDefault();

        // CSRF state validation — must be preserved
        if (state != HttpContext.Session.GetString(State))
        {
            _logger.LogError("State does not match");
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(code))
        {
            _logger.LogError("Code is missing");
            return BadRequest();
        }

        var callbackUrl = Url.Action("Callback", "LinkedIn", null, Request.Scheme) ?? string.Empty;
        if (string.IsNullOrEmpty(callbackUrl))
        {
            _logger.LogError("Callback URL is missing");
            return BadRequest();
        }

        // Exchange the code for an access token
        var headers = new Dictionary<string, string>
        {
            {"Content-Type", "application/x-www-form-urlencoded"},
            {"grant_type", "authorization_code"},
            {"code", code},
            {"redirect_uri", callbackUrl},
            {"client_id", _linkedInSettings.ClientId},
            {"client_secret", _linkedInSettings.ClientSecret}
        };

        var response = await _httpClient.PostAsync(_linkedInSettings.AccessTokenUrl, new FormUrlEncodedContent(headers), cancellationToken);

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync(cancellationToken));

        if (tokenResponse == null)
        {
            _logger.LogError("Token response is null");
            return BadRequest();
        }

        var ownerOid = User.FindFirstValue("oid")
            ?? throw new InvalidOperationException("User OID claim is missing");

        var platform = await _socialMediaPlatformManager.GetByNameAsync("LinkedIn", cancellationToken)
            ?? throw new InvalidOperationException("LinkedIn platform not found");

        var accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        DateTimeOffset? refreshTokenExpiresAt = tokenResponse.RefreshTokenExpiresIn.HasValue
            ? DateTimeOffset.UtcNow.AddSeconds(tokenResponse.RefreshTokenExpiresIn.Value)
            : null;

        await _userOAuthTokenManager.StoreOAuthCallbackTokenAsync(
            ownerOid,
            platform.Id,
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            accessTokenExpiresAt,
            refreshTokenExpiresAt,
            cancellationToken);

        _logger.LogInformation(
            "Stored OAuth token for platform {PlatformId} owner {OwnerOid} expiring {ExpiresAt}",
            platform.Id,
            LogSanitizer.Sanitize(ownerOid),
            accessTokenExpiresAt);

        return RedirectToAction("Index");
    }
}


using System.Text.Json;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models.LinkedIn;
using Microsoft.AspNetCore.Mvc;

namespace JosephGuadagno.Broadcasting.Web.Controllers;

/// <summary>
/// Handles the LinkedIn OAuth2 flow
/// </summary>
public class LinkedInController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly IKeyVault _keyVault;
    private readonly ILinkedInSettings _linkedInSettings;
    private readonly ILogger<LinkedInController> _logger;
    
    const string KeyVaultSecretName = "jjg-net-linkedin-access-token";
    const string State = "state";
    
    /// <summary>
    /// Works with the LinkedIn Api to refresh tokens
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="keyVault"></param>
    /// <param name="linkedInSettings"></param>
    /// <param name="logger"></param>
    public LinkedInController(HttpClient httpClient, IKeyVault keyVault, ILinkedInSettings linkedInSettings, ILogger<LinkedInController> logger)
    {
        _httpClient = httpClient;
        _keyVault = keyVault;
        _linkedInSettings = linkedInSettings;
        _logger = logger;
    }
    
    /// <summary>
    /// Returns the token information from Key Vault for LinkedIn
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> Index()
    {
        var keyVaultSecret = await _keyVault.GetSecretAsync(KeyVaultSecretName);
        return View(new SavedTokenInfo
        {
            AccessToken = keyVaultSecret.Value,
            KeyVaultSecretName = keyVaultSecret.Name,
            ExpiresOn = keyVaultSecret.Properties.ExpiresOn
        });
    }

    /// <summary>
    /// Starts the LinkedIn OAuth2 flow
    /// </summary>
    /// <returns></returns>
    public async Task<IActionResult> RefreshToken()
    {
        // Call the LinkedIn API to refresh the token
        // Note: This is not actually refreshing the token but reauthorizing the user
        // Documentation: https://learn.microsoft.com/en-us/linkedin/shared/authentication/authorization-code-flow?context=linkedin%2Fcontext&tabs=HTTPS1#step-2-request-an-authorization-code

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

        return Redirect(url);
        //await _httpClient.GetAsync(url);

    }
    
    /// <summary>
    /// Handles the callback from LinkedIn
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<IActionResult> Callback()
    {
        // Handle the callback from LinkedIn
        // Callback is handled in two parts
        // 1. Retrieve the authorization code
        // Documentation: https://learn.microsoft.com/en-us/linkedin/shared/authentication/authorization-code-flow?context=linkedin%2Fcontext&tabs=HTTPS1#step-2-request-an-authorization-code
        // 2. Exchange the authorization code for an access token
        // Documentation: https://learn.microsoft.com/en-us/linkedin/shared/authentication/authorization-code-flow?context=linkedin%2Fcontext&tabs=HTTPS1#step-3-exchange-authorization-code-for-an-access-token
        
        // Read the code and state from the query string
        // Compare the state to the session state
        
        var code = HttpContext.Request.Query["code"].FirstOrDefault();
        var state = HttpContext.Request.Query["state"].FirstOrDefault();
        
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
        // Build the URL to exchange the code for an access token
        var headers = new Dictionary<string, string>
        {
            {"Content-Type", "application/x-www-form-urlencoded"},
            {"grant_type", "authorization_code"},
            {"code", code},
            {"redirect_uri", callbackUrl},
            {"client_id", _linkedInSettings.ClientId},
            {"client_secret", _linkedInSettings.ClientSecret}
        };
        
        var response = await _httpClient.PostAsync(_linkedInSettings.AccessTokenUrl, new FormUrlEncodedContent(headers));
        
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync());
        
        if (tokenResponse == null) 
        {
            _logger.LogError("Token response is null");
            return BadRequest();
        }
        
        // Save the token and expiration to KeyVault
        var tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn); 
        await _keyVault.UpdateSecretValueAndPropertiesAsync(KeyVaultSecretName, tokenResponse.AccessToken, tokenExpiration);
        
        _logger.LogInformation("Saved new token 'jjg-net-linkedin-access-token' to KeyVault");

        return RedirectToAction("Index");
    }
}
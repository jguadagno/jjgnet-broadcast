using JosephGuadagno.Broadcasting.Api.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace JosephGuadagno.Broadcasting.Api;

/// <summary>
/// Transforms OpenAPI document to include XML documentation comments.
/// </summary>
public sealed class XmlDocumentTransformer(ISettings settings, IAzureAdSettings azureAdSettings) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Set document metadata
        document.Info = new OpenApiInfo()
        {
            Title = "JosephGuadagno.NET Broadcasting API",
            Version = "v2",
            Description = "The API for the JosephGuadagno.NET Broadcasting Application",
            TermsOfService = new Uri("https://example.com/terms"),
            Contact = new OpenApiContact
            {
                Name = "Joseph Guadagno",
                Email = "jguadagno@hotmail.com",
                Url = new Uri("https://www.josephguadagno.net"),
            }
        };

        var authority = $"{azureAdSettings.Instance}/{azureAdSettings.TenantId}";
        var audience = settings.ApiScopeUrl;
        var schemaKey = "OAuth2"; // This name is used as a key and could be anything as long as you are consistent.

        var scopes = Domain.Scopes.AllAccessToDictionary(audience);
        scopes.Add($"{audience}user_impersonation", "Access application on user behalf");

        var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            [schemaKey] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Scheme = "OAuth2",
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{authority}/oauth2/v2.0/authorize"),
                        TokenUrl = new Uri($"{authority}/oauth2/v2.0/token"),
                        RefreshUrl = new Uri($"{authority}/oauth2/v2.0/token"),
                        Scopes = scopes,
                    },
                },
            },
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = securitySchemes;

        var securityRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(schemaKey, document)] = [..scopes.Keys],
        };

        document.Security = [securityRequirement];

        return Task.CompletedTask;
    }
}
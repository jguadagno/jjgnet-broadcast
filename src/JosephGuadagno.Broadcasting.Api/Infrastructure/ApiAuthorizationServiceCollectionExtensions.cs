using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Managers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JosephGuadagno.Broadcasting.Api.Infrastructure;

internal static class ApiAuthorizationServiceCollectionExtensions
{
    internal static IServiceCollection AddBroadcastingApiAuthorization(this IServiceCollection services)
    {
        // Use AddScoped (not TryAddScoped) — AddMicrosoftIdentityWebApiAuthentication registers its own
        // IClaimsTransformation first, so TryAdd would silently no-op and EntraClaimsTransformation
        // would never run. AddScoped ensures our implementation is the one the pipeline uses.
        services.AddScoped<IClaimsTransformation, EntraClaimsTransformation>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicyNames.RequireSiteAdministrator, policy =>
                policy.RequireRole(RoleNames.SiteAdministrator));

            options.AddPolicy(AuthorizationPolicyNames.RequireAdministrator, policy =>
                policy.RequireRole(RoleNames.SiteAdministrator, RoleNames.Administrator));

            options.AddPolicy(AuthorizationPolicyNames.RequireContributor, policy =>
                policy.RequireRole(RoleNames.SiteAdministrator, RoleNames.Administrator, RoleNames.Contributor));

            options.AddPolicy(AuthorizationPolicyNames.RequireViewer, policy =>
                policy.RequireRole(RoleNames.SiteAdministrator, RoleNames.Administrator, RoleNames.Contributor, RoleNames.Viewer));
        });

        return services;
    }
}

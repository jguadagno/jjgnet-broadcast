using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Managers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JosephGuadagno.Broadcasting.Api.Infrastructure;

internal static class ApiAuthorizationServiceCollectionExtensions
{
    internal static IServiceCollection AddBroadcastingApiAuthorization(this IServiceCollection services)
    {
        services.TryAddScoped<IClaimsTransformation, EntraClaimsTransformation>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireSiteAdministrator", policy =>
                policy.RequireRole(RoleNames.SiteAdministrator));

            options.AddPolicy("RequireAdministrator", policy =>
                policy.RequireRole(RoleNames.SiteAdministrator, RoleNames.Administrator));

            options.AddPolicy("RequireContributor", policy =>
                policy.RequireRole(RoleNames.SiteAdministrator, RoleNames.Administrator, RoleNames.Contributor));

            options.AddPolicy("RequireViewer", policy =>
                policy.RequireRole(RoleNames.SiteAdministrator, RoleNames.Administrator, RoleNames.Contributor, RoleNames.Viewer));
        });

        return services;
    }
}

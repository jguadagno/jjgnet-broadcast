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

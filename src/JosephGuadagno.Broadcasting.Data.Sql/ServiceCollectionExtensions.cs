using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Broadcasting SQL data access services
/// </summary>
public static class BroadcastingDataSqlServiceCollectionExtensions
{
    /// <summary>
    /// Registers all SQL-backed data store implementations
    /// </summary>
    public static IServiceCollection AddSqlDataStores(this IServiceCollection services)
    {
        services.TryAddScoped<IEngagementDataStore, EngagementDataStore>();
        services.TryAddScoped<IScheduledItemDataStore, ScheduledItemDataStore>();
        services.TryAddScoped<IMessageTemplateDataStore, MessageTemplateDataStore>();
        services.TryAddScoped<ISocialMediaPlatformDataStore, SocialMediaPlatformDataStore>();
        services.TryAddScoped<IEngagementSocialMediaPlatformDataStore, EngagementSocialMediaPlatformDataStore>();
        services.TryAddScoped<IApplicationUserDataStore, ApplicationUserDataStore>();
        services.TryAddScoped<IRoleDataStore, RoleDataStore>();
        services.TryAddScoped<IUserApprovalLogDataStore, UserApprovalLogDataStore>();
        services.TryAddScoped<IEmailTemplateDataStore, EmailTemplateDataStore>();
        
        return services;
    }

    /// <summary>
    /// Configures AutoMapper to include Data.Sql mapping profiles
    /// </summary>
    public static void AddDataSqlMappingProfiles(this IMapperConfigurationExpression config)
    {
        config.AddProfile<BroadcastingProfile>();
        config.AddProfile<RbacProfile>();
    }
}
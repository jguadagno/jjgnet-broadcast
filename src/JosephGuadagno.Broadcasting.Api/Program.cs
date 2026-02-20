using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.Serilog;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Exceptions;
using ISettings = JosephGuadagno.Broadcasting.Api.Interfaces.ISettings;
using Settings = JosephGuadagno.Broadcasting.Api.Models.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpLogging(
    options => { options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All; });

var settings = new Settings
{
    ApiScopeUrl = null!,
    ScalarClientId = null!,
    StorageAccount = null!
};
builder.Configuration.Bind("Settings", settings);
builder.Services.TryAddSingleton<ISettings>(settings);
var autoMapperSettings = new AutoMapperSettings();
builder.Configuration.Bind("AutoMapper", autoMapperSettings);
builder.Services.AddSingleton<IAutoMapperSettings>(autoMapperSettings);
var azureAdSettings = new AzureAdSettings();
builder.Configuration.Bind("AzureAd", azureAdSettings);
builder.Services.TryAddSingleton<IAzureAdSettings>(azureAdSettings);

builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

builder.Services.AddSingleton<ITelemetryInitializer, AzureWebAppRoleEnvironmentTelemetryInitializer>();
builder.Services.AddApplicationInsightsTelemetry();

// Configure the logger
var fullyQualifiedLogFile = Path.Combine(builder.Environment.ContentRootPath, "logs\\logs.txt");
ConfigureLogging(builder.Configuration, builder.Services, settings, fullyQualifiedLogFile, "Api");

// Register DI services
ConfigureApplication(builder.Services);

// Add in AutoMapper
builder.Services.AddAutoMapper(config =>
{
    config.LicenseKey = autoMapperSettings.LicenseKey;
    config.AddProfile<JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles.BroadcastingProfile>();
}, typeof(Program));

// ASP.NET Core API stuff
builder.Services.AddControllers();

// Configure OpenAPI
// Learn more about configuring OpenAPI at https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview
// With help from https://hals.app/blog/dotnet-openapi-scalar-oauth2/
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<JosephGuadagno.Broadcasting.Api.XmlDocumentTransformer>();
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar", options =>
    {
        // Configure OAuth2 security
        var scopes = JosephGuadagno.Broadcasting.Domain.Scopes.AllAccessToDictionary(settings.ApiScopeUrl);
        scopes.Add($"{settings.ApiScopeUrl}user_impersonation", "Access application on user behalf");

        options
            .AddPreferredSecuritySchemes("OAuth2") // This is the schemaKey from above
            .AddImplicitFlow(
                "OAuth2", // Again: schemaKey
                flow =>
                {
                    flow.ClientId = settings.ScalarClientId;
                    // Same scopes as defined in the OpenApi transformer!
                    flow.SelectedScopes = scopes.Keys.ToArray();
                }
            );
    });
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

void ConfigureLogging(IConfigurationRoot configurationRoot, IServiceCollection services, ISettings configSettings, string logPath, string applicationName)
{
    var logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithEnvironmentName()
        .Enrich.WithAssemblyName()
        .Enrich.WithAssemblyVersion(true)
        .Enrich.WithExceptionDetails()
        .Enrich.WithProperty("Application", applicationName)
        .Destructure.ToMaximumDepth(4)
        .Destructure.ToMaximumStringLength(100)
        .Destructure.ToMaximumCollectionCount(10)
        .WriteTo.Console()
        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
        .WriteTo.AzureTableStorage(configSettings.StorageAccount, storageTableName:"Logging", keyGenerator:new SerilogKeyGenerator())
        .CreateLogger();
    services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
                config.ConnectionString =
                    configurationRoot["APPLICATIONINSIGHTS_CONNECTION_STRING"],
            configureApplicationInsightsLoggerOptions: (_) => { });
        loggingBuilder.AddSerilog(logger);
    });
}

void ConfigureApplication(IServiceCollection services)
{
    ConfigureRepositories(services);
}

void ConfigureRepositories(IServiceCollection services)
{
    builder.AddSqlServerDbContext<BroadcastingContext>("JJGNetDatabaseSqlServer");
    builder.EnrichSqlServerDbContext<BroadcastingContext>(
        configureSettings: sqlServerSettings =>
        {
            sqlServerSettings.DisableRetry = false;
            sqlServerSettings.CommandTimeout = 30; // seconds
        });

    // Engagements
    services.TryAddScoped<IEngagementDataStore, EngagementDataStore>();
    services.TryAddScoped<IEngagementRepository, EngagementRepository>();
    services.TryAddScoped<IEngagementManager, EngagementManager>();

    // ScheduledItem
    services.TryAddScoped<IScheduledItemDataStore, ScheduledItemDataStore>();
    services.TryAddScoped<IScheduledItemRepository, ScheduledItemRepository>();
    services.TryAddScoped<IScheduledItemManager, ScheduledItemManager>();
}
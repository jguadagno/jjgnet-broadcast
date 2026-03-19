using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;

using JosephGuadagno.Broadcasting.Api.Infrastructure;
using JosephGuadagno.Broadcasting.Api.Interfaces;
using JosephGuadagno.Broadcasting.Api.Models;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.Serilog;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using OpenTelemetry.Logs;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpLogging(
    options => { options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All; });

var settings = new Settings
{
    ApiScopeUrl = null!,
    LoggingStorageAccount = null!,
    ScalarClientId = null!
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

// Configure the telemetry and logging
var fullyQualifiedLogFile = Path.Combine(builder.Environment.ContentRootPath, "logs\\logs.txt");
ConfigureTelemetryAndLogging(builder.Services, settings.LoggingStorageAccount, fullyQualifiedLogFile, "Api");

// Register DI services

// Add in AutoMapper
builder.Services.AddAutoMapper(config =>
{
    config.LicenseKey = autoMapperSettings.LicenseKey;
    config.AddProfile<JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles.BroadcastingProfile>();
}, typeof(Program));

ConfigureApplication(builder.Services);

// ASP.NET Core API stuff
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
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

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "0";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

void ConfigureTelemetryAndLogging(IServiceCollection services, string logStorageAccount, string logPath, string applicationName)
{

    builder.Services.AddOpenTelemetry().UseAzureMonitor();

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
        .WriteTo.AzureTableStorage(logStorageAccount, storageTableName:"Logging", keyGenerator:new SerilogKeyGenerator())
        .WriteTo.OpenTelemetry()
        .CreateLogger();
    services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddOpenTelemetry(options =>
        {
            options.AddConsoleExporter();
        });
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
    services.TryAddScoped<IEngagementManager, EngagementManager>();

    // ScheduledItem
    services.TryAddScoped<IScheduledItemDataStore, ScheduledItemDataStore>();
    services.TryAddScoped<IScheduledItemManager, ScheduledItemManager>();

    // MessageTemplate
    services.TryAddScoped<IMessageTemplateDataStore, MessageTemplateDataStore>();
}

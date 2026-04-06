using JosephGuadagno.Broadcasting.Api.Infrastructure;
using JosephGuadagno.Broadcasting.Api.Models;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.Serilog;
using JosephGuadagno.AzureHelpers.Storage;
using JosephGuadagno.AzureHelpers.Storage.Interfaces;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using OpenTelemetry.Logs;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpLogging(
    options => { options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All; });

// Read for inline startup use
var settings = builder.Configuration.GetSection("Settings").Get<Settings>()
    ?? new Settings { ApiScopeUrl = string.Empty, ScalarClientId = string.Empty };
var autoMapperSettings = builder.Configuration.GetSection("AutoMapper").Get<AutoMapperSettings>()
    ?? new AutoMapperSettings();

// Register via IOptions
builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));
builder.Services.AddOptions<Settings>().ValidateDataAnnotations().ValidateOnStart();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddOptions<EmailSettings>().ValidateDataAnnotations().ValidateOnStart();
// Keep IEmailSettings singleton for EmailSender compatibility
var emailSettings = new EmailSettings
{
    FromAddress = string.Empty,
    FromDisplayName = string.Empty,
    ReplyToAddress = string.Empty,
    ReplyToDisplayName = string.Empty,
    AzureCommunicationsConnectionString = string.Empty
};
builder.Configuration.Bind("Email", emailSettings);
builder.Services.TryAddSingleton<IEmailSettings>(emailSettings);

builder.Services.Configure<AutoMapperSettings>(builder.Configuration.GetSection("AutoMapper"));
builder.Services.AddOptions<AutoMapperSettings>().ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton<IAutoMapperSettings>(autoMapperSettings);

builder.Services.Configure<AzureAdSettings>(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddOptions<AzureAdSettings>().ValidateDataAnnotations().ValidateOnStart();

builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

// Configure the telemetry and logging
var fullyQualifiedLogFile = Path.Combine(builder.Environment.ContentRootPath, "logs\\logs.txt");
ConfigureTelemetryAndLogging(builder.Services, fullyQualifiedLogFile, "Api");

// Register DI services
builder.AddAzureQueueServiceClient("QueueAccount");

// Add in AutoMapper
builder.Services.AddAutoMapper(config =>
{
    config.LicenseKey = autoMapperSettings.LicenseKey;
    config.AddProfile<JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles.BroadcastingProfile>();
    config.AddProfile<JosephGuadagno.Broadcasting.Api.MappingProfiles.ApiBroadcastingProfile>();
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

if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar", options =>
    {
        // Configure OAuth2 security
        var scopes = JosephGuadagno.Broadcasting.Domain.Scopes.ToDictionary(settings.ApiScopeUrl);
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
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

void ConfigureTelemetryAndLogging(IServiceCollection services, string logPath, string applicationName)
{
    var logger = new LoggerConfiguration()
        .ConfigureSerilog(applicationName, logPath)
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

    // RBAC Phase 1
    services.TryAddScoped<IApplicationUserDataStore, ApplicationUserDataStore>();
    services.TryAddScoped<IRoleDataStore, RoleDataStore>();
    services.TryAddScoped<IUserApprovalLogDataStore, UserApprovalLogDataStore>();
    services.TryAddScoped<IEmailTemplateDataStore, EmailTemplateDataStore>();
    services.TryAddScoped<IUserApprovalManager, UserApprovalManager>();

    // Email
    services.TryAddScoped<IEmailSender, EmailSender>();
    services.TryAddScoped<IEmailTemplateManager, EmailTemplateManager>();
}
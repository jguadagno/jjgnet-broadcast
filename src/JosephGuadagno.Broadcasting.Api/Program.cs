using System.Globalization;
using System.Threading.RateLimiting;
using JosephGuadagno.Broadcasting.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using JosephGuadagno.Broadcasting.Api.Models;
using Microsoft.AspNetCore.RateLimiting;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.Serilog;
using JosephGuadagno.AzureHelpers.Storage;
using JosephGuadagno.AzureHelpers.Storage.Interfaces;
using JosephGuadagno.Broadcasting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpLogging(
    options => { options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All; });

// Read for inline startup use
var settings = builder.Configuration.GetSection("Settings").Get<Settings>()
    ?? new Settings { ScalarClientId = string.Empty };
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
builder.Services.AddBroadcastingApiAuthorization();

// Configure the telemetry and logging
var fullyQualifiedLogFile = Path.Combine(builder.Environment.ContentRootPath, "logs\\logs.txt");
ConfigureTelemetryAndLogging(builder.Services, fullyQualifiedLogFile, "Api");

// Register DI services
builder.AddAzureQueueServiceClient("QueueAccount");

// Add in AutoMapper
builder.Services.AddAutoMapper(config =>
{
    config.LicenseKey = autoMapperSettings.LicenseKey;
    config.AddDataSqlMappingProfiles();
    config.AddProfile<JosephGuadagno.Broadcasting.Api.MappingProfiles.ApiBroadcastingProfile>();
}, typeof(Program));

builder.Services.AddMemoryCache();

ConfigureApplication(builder.Services);

// ASP.NET Core API stuff
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers(options =>
{
    // API uses Bearer token auth ΓÇö antiforgery tokens are not applicable
    options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
});

// Rate limiting ΓÇö 100 requests per minute (fixed window), applied globally
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(RateLimitingPolicies.FixedWindow, limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }
        else
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)TimeSpan.FromSeconds(60).TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", cancellationToken);
    };
});

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
        var scopes = GetApiScopes(builder.Configuration["AzureAd:ClientId"]);

        options
            .AddPreferredSecuritySchemes("OAuth2") // This is the schemaKey from above
            .AddImplicitFlow(
                "OAuth2", // Again: schemaKey
                flow =>
                {
                    flow.ClientId = settings.ScalarClientId;
                    flow.SelectedScopes = scopes;
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
app.UseRateLimiter();

// NOTE: Health check endpoints (e.g., app.MapHealthChecks("/health")) should be exempted
// from rate limiting via .DisableRateLimiting() when added to this project.
// MapDefaultEndpoints() (Aspire) handles its own health/liveness endpoints separately.
app.MapControllers().RequireRateLimiting(RateLimitingPolicies.FixedWindow);

app.Run();

static string[] GetApiScopes(string? clientId)
{
    if (string.IsNullOrWhiteSpace(clientId))
    {
        return [];
    }

    return [$"api://{clientId}/{Scopes.MicrosoftGraph.UserImpersonation}"];
}

void ConfigureTelemetryAndLogging(IServiceCollection services, string logPath, string applicationName)
{
    var logger = new LoggerConfiguration()
        .ConfigureSerilog(applicationName, logPath)
        .CreateLogger();
    services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddSerilog(logger);
    });
}

void ConfigureApplication(IServiceCollection services)
{
    ConfigureRepositories(services);
}

void ConfigureRepositories(IServiceCollection services)
{
    // DisableRetry = true tells Aspire to skip its own EnableRetryOnFailure() call.
    // We own the retry policy exclusively via configureDbContextOptions.
    // With DisableRetry = false, Aspire calls EnableRetryOnFailure() with its defaults (6 retries,
    // 30 s max) inside UseSqlServer, and although configureDbContextOptions runs after and should
    // override it, in practice the Aspire-default schedule (Γëê14ΓÇô20 s for 3 transient retries)
    // was still observed.  Setting DisableRetry = true removes that ambiguity entirely.
    builder.AddSqlServerDbContext<BroadcastingContext>(
        "JJGNetDatabaseSqlServer",
        configureSettings: sqlServerSettings =>
        {
            sqlServerSettings.DisableRetry = true; // Aspire must not set up its own retry
            sqlServerSettings.CommandTimeout = 30; // seconds
        },
        configureDbContextOptions: options =>
        {
            options.UseSqlServer(o => o.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null));
        });

    // Engagements
    services.TryAddScoped<IEngagementDataStore, EngagementDataStore>();
    services.TryAddScoped<IEngagementManager, EngagementManager>();

    // ScheduledItem
    services.TryAddScoped<IScheduledItemDataStore, ScheduledItemDataStore>();
    services.TryAddScoped<IScheduledItemManager, ScheduledItemManager>();

    // MessageTemplate
    services.TryAddScoped<IMessageTemplateDataStore, MessageTemplateDataStore>();

    // SocialMediaPlatform
    services.TryAddScoped<ISocialMediaPlatformDataStore, SocialMediaPlatformDataStore>();
    services.TryAddScoped<ISocialMediaPlatformManager, SocialMediaPlatformManager>();
    services.TryAddScoped<IUserPublisherSettingDataStore, UserPublisherSettingDataStore>();
    services.TryAddScoped<IUserPublisherSettingManager, UserPublisherSettingManager>();
    
    services.TryAddScoped<IEngagementSocialMediaPlatformDataStore, EngagementSocialMediaPlatformDataStore>();

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

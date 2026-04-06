using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.Serilog;
using JosephGuadagno.Broadcasting.Web;
using JosephGuadagno.Broadcasting.Web.HealthChecks;
using JosephGuadagno.Broadcasting.Web.MappingProfiles;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Services;
using JosephGuadagno.Broadcasting.Web.Interfaces;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

using OpenTelemetry.Logs;

using Serilog;
using Rocket.Surgery.Extensions.AutoMapper.NodaTime;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Conditionally add Azure Key Vault health check (Web only — Key Vault is not used by the API)
var keyVaultUri = builder.Configuration["KeyVault:vaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Services.AddHealthChecks()
        .Add(new HealthCheckRegistration(
            "azure-key-vault",
            sp => new AzureKeyVaultHealthCheck(sp.GetRequiredService<Azure.Security.KeyVault.Secrets.SecretClient>()),
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"],
            timeout: TimeSpan.FromSeconds(5)));
}

// Read for inline startup use
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

builder.Services.Configure<LinkedInSettings>(builder.Configuration.GetSection("LinkedIn"));
builder.Services.AddOptions<LinkedInSettings>().ValidateDataAnnotations().ValidateOnStart();

builder.Services.Configure<AutoMapperSettings>(builder.Configuration.GetSection("AutoMapper"));
builder.Services.AddOptions<AutoMapperSettings>().ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton<IAutoMapperSettings>(autoMapperSettings);

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

// Configure the logger
var fullyQualifiedLogFile = Path.Combine(builder.Environment.ContentRootPath, "logs\\logs.txt");
ConfigureTelemetryAndLogging(builder.Services, fullyQualifiedLogFile, "Web");

// Register BroadcastingContext for RBAC data stores
builder.AddSqlServerDbContext<BroadcastingContext>("JJGNetDatabaseSqlServer");

// Register DI services
builder.AddAzureQueueServiceClient("QueueAccount");
ConfigureApplication(builder.Services);

// Add in AutoMapper
builder.Services.AddAutoMapper(config =>
{
    config.LicenseKey = autoMapperSettings.LicenseKey;
    config.AddProfile<WebMappingProfile>();
    config.AddProfile<NodaTimeProfile>();
    config.AddProfile<JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles.BroadcastingProfile>();
    config.AddProfile<JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles.RbacProfile>();
}, typeof(Program));

// Configure Microsoft Identity
IEnumerable<string>? initialScopes = builder.Configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<IEnumerable<string>>();
IEnumerable<string>? downstreamApiScopes = builder.Configuration.GetSection("DownstreamApis:JosephGuadagnoBroadcastingApi:Scopes").Get<IEnumerable<string>>();
var allScopes = initialScopes?.Union(downstreamApiScopes!);

builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi(allScopes)
    .AddDistributedTokenCaches();
builder.Services.AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));

// Register claims transformation for RBAC
builder.Services.AddScoped<IClaimsTransformation, EntraClaimsTransformation>();

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
}).AddMicrosoftIdentityUI();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministrator", policy =>
        policy.RequireRole(RoleNames.Administrator));
    
    options.AddPolicy("RequireContributor", policy =>
        policy.RequireRole(RoleNames.Administrator, RoleNames.Contributor));
    
    options.AddPolicy("RequireViewer", policy =>
        policy.RequireRole(RoleNames.Administrator, RoleNames.Contributor, RoleNames.Viewer));
});

builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.Events = new RejectSessionCookieWhenAccountNotInCacheEvents();
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("JJGNetDatabaseSqlServer");
    options.SchemaName = "dbo";
    options.TableName = "TokenCache";
    options.DefaultSlidingExpiration = TimeSpan.FromDays(14); // Refresh cache entry on access
    options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30); // Clean up expired entries
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseUserApprovalGate();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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
    services.AddHttpClient();
    services.TryAddScoped<IEngagementService, EngagementService>();
    services.TryAddScoped<IScheduledItemService, ScheduledItemService>();
    services.TryAddScoped<IMessageTemplateService, MessageTemplateService>();

    // RBAC Phase 1
    services.TryAddScoped<IApplicationUserDataStore, ApplicationUserDataStore>();
    services.TryAddScoped<IRoleDataStore, RoleDataStore>();
    services.TryAddScoped<IUserApprovalLogDataStore, UserApprovalLogDataStore>();
    services.TryAddScoped<IEmailTemplateDataStore, EmailTemplateDataStore>();
    services.TryAddScoped<IUserApprovalManager, UserApprovalManager>();

    // Email
    services.TryAddScoped<IEmailSender, EmailSender>();
    services.TryAddScoped<IEmailTemplateManager, EmailTemplateManager>();

    ConfigureKeyVault(services);
}

void ConfigureKeyVault(IServiceCollection services)
{
    services.AddAzureClients(clientBuilder =>
    {
        clientBuilder.AddSecretClient(builder.Configuration.GetSection("KeyVault"));
    });
    services.TryAddScoped<IKeyVault, KeyVault>();
}
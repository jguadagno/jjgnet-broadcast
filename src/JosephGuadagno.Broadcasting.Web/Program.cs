using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
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
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

// Register DI services
builder.AddAzureQueueServiceClient("QueueAccount");
ConfigureApplication(builder.Services);

// Add in AutoMapper
builder.Services.AddAutoMapper(config =>
{
    config.LicenseKey = autoMapperSettings.LicenseKey;
    config.AddProfile<WebMappingProfile>();
    config.AddProfile<NodaTimeProfile>();
    config.AddDataSqlMappingProfiles();
}, typeof(Program));

// Configure Microsoft Identity
IEnumerable<string>? initialScopes = builder.Configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<IEnumerable<string>>();
IEnumerable<string>? downstreamApiScopes = builder.Configuration.GetSection("DownstreamApis:JosephGuadagnoBroadcastingApi:Scopes").Get<IEnumerable<string>>();
var allScopes = initialScopes?.Union(downstreamApiScopes!);

builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi(allScopes)
    .AddDistributedTokenCaches();
builder.Services.AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));

// Add OIDC event handlers for graceful error handling
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Events.OnRemoteFailure = context =>
    {
        // Handle AADSTS consent and authorization errors gracefully
        if (context.Failure?.Message != null)
        {
            var errorMessage = context.Failure.Message;
            
            // AADSTS650052: The app needs access to a service that your organization hasn't subscribed to or enabled
            // AADSTS65001: The user or administrator hasn't consented to use the application
            // AADSTS700016: Application not found in the directory/tenant
            // AADSTS70011: The provided value for the input parameter 'scope' is not valid
            if (errorMessage.Contains("AADSTS650052") ||
                errorMessage.Contains("AADSTS65001") ||
                errorMessage.Contains("AADSTS700016") ||
                errorMessage.Contains("AADSTS70011"))
            {
                context.Response.Redirect("/Home/AuthError?message=" + 
                    Uri.EscapeDataString("Your organization hasn't granted access to this application. " +
                    "Please contact your IT administrator to enable access."));
                context.HandleResponse();
                return Task.CompletedTask;
            }
        }
        
        // For all other errors, redirect to generic auth error page
        context.Response.Redirect("/Home/AuthError?message=" + 
            Uri.EscapeDataString("An error occurred during sign-in. Please try again or contact support."));
        context.HandleResponse();
        return Task.CompletedTask;
    };
});

// Register claims transformation for RBAC
builder.Services.AddScoped<IClaimsTransformation, EntraClaimsTransformation>();

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
    // Validate antiforgery tokens on all unsafe methods (POST/PUT/DELETE) globally
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
}).AddMicrosoftIdentityUI();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
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
    
    // Register BroadcastingContext via transitive dependency (Managers → Data.Sql)
    // Note: BroadcastingContext type is fully qualified to avoid needing "using Data.Sql"
    builder.AddSqlServerDbContext<JosephGuadagno.Broadcasting.Data.Sql.BroadcastingContext>("JJGNetDatabaseSqlServer");
    
    // Register all SQL data stores
    services.AddSqlDataStores();
    
    services.TryAddScoped<IEngagementService, EngagementService>();
    services.TryAddScoped<ISocialMediaPlatformService, SocialMediaPlatformService>();
    services.TryAddScoped<IScheduledItemService, ScheduledItemService>();
    services.TryAddScoped<IMessageTemplateService, MessageTemplateService>();
    services.TryAddScoped<ISocialMediaPlatformService, SocialMediaPlatformService>();

    // RBAC Phase 1
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
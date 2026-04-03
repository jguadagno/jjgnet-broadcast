using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Data.Sql;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers;
using JosephGuadagno.Broadcasting.Serilog;
using JosephGuadagno.Broadcasting.Web;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.MappingProfiles;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Services;
using JosephGuadagno.AzureHelpers.Storage;
using JosephGuadagno.AzureHelpers.Storage.Interfaces;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

using OpenTelemetry.Logs;

using Serilog;
using Rocket.Surgery.Extensions.AutoMapper.NodaTime;
using ISettings = JosephGuadagno.Broadcasting.Web.Interfaces.ISettings;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var settings = new Settings
{
    StaticContentRootUrl = null!,
    LoggingStorageAccount = null!,
};
builder.Configuration.Bind("Settings", settings);
builder.Services.TryAddSingleton<ISettings>(settings);
var emailSettings = new EmailSettings
{
    FromAddress = null!,
    FromDisplayName = null!,
    ReplyToAddress = null!,
    ReplyToDisplayName = null!,
    AzureCommunicationsConnectionString = null!
};
builder.Configuration.Bind("Email", emailSettings);
builder.Services.TryAddSingleton<IEmailSettings>(emailSettings);

var linkedInSettings = new LinkedInSettings();
builder.Configuration.Bind("LinkedIn", linkedInSettings);
builder.Services.TryAddSingleton<ILinkedInSettings>(linkedInSettings);
var autoMapperSettings = new AutoMapperSettings();
builder.Configuration.Bind("AutoMapper", autoMapperSettings);
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
        .ConfigureSerilog(builder.Configuration, applicationName, logPath)
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
    services.TryAddSingleton<IQueue>(s =>
    {
        var configuration = s.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("QueueStorage") ?? "UseDevelopmentStorage=true";
        return new Queue(connectionString, JosephGuadagno.Broadcasting.Domain.Constants.Queues.SendEmail);
    });
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
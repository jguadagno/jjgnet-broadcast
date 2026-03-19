using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using JosephGuadagno.Broadcasting.Data.KeyVault;
using JosephGuadagno.Broadcasting.Data.KeyVault.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Serilog;
using JosephGuadagno.Broadcasting.Web;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.MappingProfiles;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

using OpenTelemetry.Logs;

using Serilog;
using Serilog.Exceptions;
using Rocket.Surgery.Extensions.AutoMapper.NodaTime;
using ISettings = JosephGuadagno.Broadcasting.Web.Interfaces.ISettings;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var settings = new Settings
{
    ApiRootUrl = null!,
    ApiScopeUrl = null!,
    StaticContentRootUrl = null!,
    LoggingStorageAccount = null!
};
builder.Configuration.Bind("Settings", settings);
builder.Services.TryAddSingleton<ISettings>(settings);

var linkedInSettings = new LinkedInSettings();
builder.Configuration.Bind("LinkedIn", linkedInSettings);
builder.Services.TryAddSingleton<ILinkedInSettings>(linkedInSettings);
var autoMapperSettings = new AutoMapperSettings();
builder.Configuration.Bind("AutoMapper", autoMapperSettings);
builder.Services.AddSingleton<IAutoMapperSettings>(autoMapperSettings);

builder.Services.AddSession();

// Configure the logger
var fullyQualifiedLogFile = Path.Combine(builder.Environment.ContentRootPath, "logs\\logs.txt");
ConfigureTelemetryAndLogging(builder.Services, settings.LoggingStorageAccount, fullyQualifiedLogFile, "Web");

// Register DI services
ConfigureApplication(builder.Services);

// Add in AutoMapper
builder.Services.AddAutoMapper(config =>
{
    config.LicenseKey = autoMapperSettings.LicenseKey;
    config.AddProfile<WebMappingProfile>();
    config.AddProfile<NodaTimeProfile>();
}, typeof(Program));

// Configure Microsoft Identity
var scopes = JosephGuadagno.Broadcasting.Domain.Scopes.AllAccessToDictionary(settings.ApiScopeUrl);
scopes.Add($"{settings.ApiScopeUrl}user_impersonation", "Access application on user behalf");
// Token acquisition service based on MSAL.NET
// and chosen token cache implementation
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi(scopes.Select(k => k.Key))
    .AddDistributedTokenCaches();
builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,
    options => options.Events = new RejectSessionCookieWhenAccountNotInCacheEvents());

builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("JJGNetDatabaseSqlServer");
    options.SchemaName = "dbo";
    options.TableName = "Cache";
});

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
}).AddMicrosoftIdentityUI();

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

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-XSS-Protection"] = "0";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' cdn.jsdelivr.net; " +
        "style-src 'self' cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' cdn.jsdelivr.net data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

void ConfigureTelemetryAndLogging(IServiceCollection services, string logStorageAccount, string logPath, string applicationName)
{
    services.AddOpenTelemetry().UseAzureMonitor();
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
        .WriteTo.AzureTableStorage(logStorageAccount, storageTableName:"Logging", keyGenerator: new SerilogKeyGenerator())
        //.WriteTo.OpenTelemetry()
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
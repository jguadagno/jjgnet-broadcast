using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Serilog;
using JosephGuadagno.Broadcasting.Web;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.MappingProfiles;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Services;
using JosephGuadagnoNet.Broadcasting.Data.KeyVault;
using JosephGuadagnoNet.Broadcasting.Data.KeyVault.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Serilog;
using Serilog.Exceptions;
using Rocket.Surgery.Extensions.AutoMapper.NodaTime;
using ISettings = JosephGuadagno.Broadcasting.Web.Interfaces.ISettings;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.Bind("Settings", settings);
builder.Services.TryAddSingleton<ISettings>(settings);

var linkedInSettings = new LinkedInSettings();
builder.Configuration.Bind("LinkedIn", linkedInSettings);
builder.Services.TryAddSingleton<ILinkedInSettings>(linkedInSettings);

builder.Services.AddSession();
builder.Services.AddApplicationInsightsTelemetry();

// Configure the logger
var fullyQualifiedLogFile = Path.Combine(builder.Environment.ContentRootPath, "logs\\logs.txt");
ConfigureLogging(builder.Configuration, builder.Services, settings, fullyQualifiedLogFile, "Web");

// Register DI services
ConfigureApplication(builder.Services);

// Configure Microsoft Identity
var scopes = JosephGuadagno.Broadcasting.Domain.Scopes.AllAccessToDictionary(settings.ApiScopeUrl);
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
    options.ConnectionString = settings.JJGNetDatabaseSqlServer;
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
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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
        .WriteTo.AzureTableStorage(configSettings.StorageAccount, storageTableName:"Logging", keyGenerator: new SerilogKeyGenerator())
        .CreateLogger();
    services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
                config.ConnectionString =
                    configurationRoot["APPLICATIONINSIGHTS_CONNECTION_STRING"],
            configureApplicationInsightsLoggerOptions: (_) => { });loggingBuilder.AddApplicationInsights();
        loggingBuilder.AddSerilog(logger);
    });
}

void ConfigureApplication(IServiceCollection services)
{
    services.AddHttpClient();
    services.AddAutoMapper(typeof(NodaTimeProfile), typeof(WebMappingProfile));
    services.TryAddScoped<IEngagementService, EngagementService>();
    services.TryAddScoped<IScheduledItemService, ScheduledItemService>();
    ConfigureKeyVault(services);
}

void ConfigureKeyVault(IServiceCollection services)
{
    services.TryAddSingleton(s =>
    {
        var applicationSettings = s.GetService<ISettings>();
        if (applicationSettings is null)
        {
            throw new ApplicationException("Failed to get application settings from ServiceCollection");
        }
        
        return new SecretClient(new Uri(applicationSettings.AzureKeyVaultUrl), new DefaultAzureCredential());
    });
    
    services.TryAddScoped<IKeyVault, KeyVault>();
}
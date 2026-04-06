# Skill: IOptions Pattern Migration

**Category:** .NET Configuration  
**Complexity:** Intermediate  
**Tags:** #aspnetcore #ioptions #configuration #dependency-injection

## When to Use

- Migrating from manual `config.Bind()` + `TryAddSingleton()` pattern to ASP.NET Core IOptions
- Adding configuration validation at startup
- Improving testability of configuration-dependent services

## Prerequisites

- ASP.NET Core application with appsettings.json
- Settings classes (POCOs) with properties matching config sections
- Understanding of Dependency Injection

## Steps

### 1. Update Program.cs Registration

**Before:**
```csharp
var settings = new MySettings();
builder.Configuration.Bind("MySection", settings);
builder.Services.TryAddSingleton<IMySettings>(settings);
```

**After:**
```csharp
builder.Services.Configure<MySettings>(builder.Configuration.GetSection("MySection"));
builder.Services.AddOptions<MySettings>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 2. Update Consumer Constructors

**Before:**
```csharp
public class MyService
{
    private readonly IMySettings _settings;
    
    public MyService(IMySettings settings)
    {
        _settings = settings;
    }
}
```

**After:**
```csharp
using Microsoft.Extensions.Options;

public class MyService
{
    private readonly MySettings _settings;
    
    public MyService(IOptions<MySettings> settingsOptions)
    {
        _settings = settingsOptions.Value;
    }
}
```

### 3. Update Tests

**Before:**
```csharp
var mockSettings = new Mock<IMySettings>();
mockSettings.Setup(s => s.Property).Returns("value");
var sut = new MyService(mockSettings.Object);
```

**After:**
```csharp
using Microsoft.Extensions.Options;

var settings = new MySettings { Property = "value" };
var settingsOptions = Options.Create(settings);
var sut = new MyService(settingsOptions);
```

### 4. Add Validation (Optional but Recommended)

```csharp
using System.ComponentModel.DataAnnotations;

public class MySettings
{
    [Required]
    [Url]
    public required string ApiBaseUrl { get; set; }
    
    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 30;
}
```

## Common Pitfalls

### ❌ Don't: Use IOptions<T> in Razor Views
Razor views should inject `IOptions<T>` and unwrap in a code block:
```cshtml
@using Microsoft.Extensions.Options
@inject IOptions<Settings> SettingsOptions
@{
    var settings = SettingsOptions.Value;
}
<img src="@settings.StaticContentRootUrl/logo.png" />
```

### ❌ Don't: Call .Value in Constructor
If unwrapping in constructor, store the unwrapped value:
```csharp
private readonly string _apiUrl;

public MyService(IOptions<Settings> options)
{
    _apiUrl = options.Value.ApiUrl;  // ✅ Unwrap once, store value
}
```

### ❌ Don't: Mix IOptions with Manual Binding
Pick one pattern per project for consistency.

## When to Use IOptionsSnapshot

Use `IOptionsSnapshot<T>` instead of `IOptions<T>` if:
- Configuration can reload during runtime
- Service is scoped (not singleton)
- Need per-request configuration values

```csharp
builder.Services.AddScoped<IMyService, MyService>();

public MyService(IOptionsSnapshot<MySettings> settings) { }
```

## Validation Options

### 1. Data Annotations (Recommended for Simple Cases)
```csharp
builder.Services.AddOptions<MySettings>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 2. Custom Validation
```csharp
builder.Services.AddOptions<MySettings>()
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.ApiKey), 
              "ApiKey must be provided")
    .ValidateOnStart();
```

### 3. IValidateOptions<T> (Complex Validation)
```csharp
public class MySettingsValidator : IValidateOptions<MySettings>
{
    public ValidateOptionsResult Validate(string? name, MySettings options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            return ValidateOptionsResult.Fail("ApiKey is required");
        
        return ValidateOptionsResult.Success;
    }
}

builder.Services.AddSingleton<IValidateOptions<MySettings>, MySettingsValidator>();
```

## Benefits

✅ Early validation (fail-fast at startup)  
✅ Better testability (Options.Create() for tests)  
✅ Standard ASP.NET Core pattern  
✅ Supports config reloading (with IOptionsSnapshot)  
✅ Type-safe configuration access

## Related

- [Microsoft Docs: Options pattern in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- Skill: CancellationToken Propagation
- Skill: OperationResult Error Handling

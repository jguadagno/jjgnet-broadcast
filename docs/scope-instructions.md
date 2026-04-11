# Scope Instructions

This document outlines what needs to be done when we add/remove/modify the Microsoft identity platform [scopes](https://learn.microsoft.com/en-us/entra/identity-platform/scopes-oidc) to the API and application.

## Adding a new scope

### Application Changes

In the `JosephGuadagno.Broadcasting.Domain` project, there is a class called [Scopes](https://github.com/JosephGuadagno/NET-Broadcasting/blob/main/src/JosephGuadagno.Broadcasting.Domain/Scopes.cs) that contains the scopes for the API.

The scopes are defined as a static class that contains a dictionary of scopes. An example for the `SocialMediaPlatforms` class is shown below.

```csharp
/// <summary>
/// Contains the scopes for the SocialMediaPlatform API
/// </summary>
public static class SocialMediaPlatforms
{
    public static readonly string Add = "SocialMediaPlatforms.Add";
    public static readonly string All = "SocialMediaPlatforms.All";
    public static readonly string Delete = "SocialMediaPlatforms.Delete";
    public static readonly string List = "SocialMediaPlatforms.List";
    public static readonly string Modify = "SocialMediaPlatforms.Modify";
    public static readonly string View = "SocialMediaPlatforms.View";

    public static Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { Add, Add },
            { All, All },
            { Delete, Delete },
            { List, List },
            { Modify, Modify },
            { View, View }
        };
    }
}
```

Next, fine the method `public static Dictionary<string, string> ToDictionary(string scopeUrl)` because you will need to add the scopes to the dictionary.

Example for the `SocialMediaPlatforms` class:

```csharp
foreach (var scope in SocialMediaPlatforms.ToDictionary())
{
    allScopes.Add(scopeUrl + scope.Key, scope.Value);
}
```
#### API Changes

For each API controller, you will need to add the correct scope to the body of the method.  For example, for the `SocialMediaPlatformsController` we `GetAll` mehods, we will need to add the `Domain.Scopes.SocialMediaPlatforms.List` and `SocialMediaPlatforms.All` scope to the `VerifyUserHasAnyAcceptedScope` method. 

```csharp
HttpContext.VerifyUserHasAnyAcceptedScope(Domain.Scopes.SocialMediaPlatforms.List, Domain.Scopes.SocialMediaPlatforms.All);
```

### Portal Changes

You will need access to the Azure Portal to add a new scope. More specifically, you need to be able to edit the App registrations for the API and Web application.

Note: you will need to make these changes to both the test and production environments.

#### API Application Registration

- Log in to the [Azure Portal](https://portal.azure.com)
- Navigate to the App Registrations section
- Search for the API application (`JosephGuadagno.NET Broadcasting (Test) - API`)
- Click on the application 
- Click on the *Manage* tab
- Click on the *Expose an API* blade
- Click on *Add a scope*
- Enter the following values

| Field                      | Value                                    | Comments                                                                        |
|----------------------------|------------------------------------------|---------------------------------------------------------------------------------|
| Scope Name                 | *The name of the scope*                  | Typically the scopes are named *<Domain>.<Function>*. Example: `Schedules.List` |
| Who can consent?           | *Admin and users*                        |                                                                                 |
| Admin consent display name | *A value that is displayed to admins*    | The standard has been to just use the *Scope Name*                              |
| Admin consent description  | A description of the scope for an admin  |                                                                                 |
| User consent display name  | *A value that is displayed to users*     | The standard has been to just use the *Scope Name*                              |
| User consent description   | A description of the scope for users     |                                                                                 |
| State                      | *Enabled*                                |                                                                                 |

- Click **Add scope**
- Repeat for each new scope

Once you have added all the scopes, you will then need to add the scopes to the production API application (`JosephGuadagno.NET Broadcasting - API`).

#### Web Application Registration

- Log in to the [Azure Portal](https://portal.azure.com)
- Navigate to the App Registrations section
- Search for the Web application (`JosephGuadagno.NET Broadcasting (Test) - MVC Client`)
- Click on the application 
- Click on the *Manage* tab
- Click on the *API Permissions* button
- Click **Add a permission**
- Select *My APIs*
- Select the API application (`JosephGuadagno.NET Broadcasting (Test) - API`)
- Select each of the scopes you added. ***NOTE***: The for the applicatio we currently only use the  `*.All` scopes.
- Click **Add permissions**
- Click **Grant admin consent for Default Directory**

Once you have added all the scopes, you will then need to add the scopes to the production API application (`JosephGuadagno.NET Broadcasting - MVC Client`).

#### Web Application Configuration (Production)

Next we need to add the scopes to the Web application. Open up the *Environment Variables* for the Web application (`web-jjgnet-broadcast`) and add one entry for each scope that you added.  It's easiest to click on the *Advanced Edit* button and add the scopes one at a time.

Look for the last scope number for the configuration parameter `DownstreamApis__JosephGuadagnoBroadcastingApi__Scopes__` and example would be `DownstreamApis__JosephGuadagnoBroadcastingApi__Scopes__15`.  Copy and paste that configuration, increment the scope number and update the value.  

Sample value:

```json
{
    "name": "DownstreamApis__JosephGuadagnoBroadcastingApi__Scopes__14",
    "value": "api://abb01bf7-dd41-475a-a1a2-6bec32a74cd6/SocialMediaPlatforms.Delete",
    "slotSetting": false
}
```

#### Web Application Configuration (Development)

Next we need to add the scopes to the Web application. Open up the [appsettings.Development.json](../src/JosephGuadagno.Broadcasting.Web/appsettings.Development.json) file in the *JosephGuadagno.Broadcasting.Web* project and add one entry for each scope that you added.

The configuration will be in the `DownstreamApis\JosephGuadagnoBroadcastingApi\Scopes` section. You will want to add one entry for each scope that you added.

```json
"api://027edf6f-5140-44c8-9496-e7e98390d60c/SocialMediaPlatforms.All",
"api://027edf6f-5140-44c8-9496-e7e98390d60c/SocialMediaPlatforms.View",
"api://027edf6f-5140-44c8-9496-e7e98390d60c/SocialMediaPlatforms.List"
```

### Adding new scopes wrap up

Once all of these changes have been made, you will need to re-deploy the API and Web application. The next time you run the application and log in, you will need to consent to the new scopes.

## Removing a scope

TODO: Add instructions for removing a scope.

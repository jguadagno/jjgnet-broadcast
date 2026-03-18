using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Deployment = Pulumi.Deployment;

class JjgnetStack : Stack
{
    [Output] public Output<string> ResourceGroupName { get; set; } = null!;

    public JjgnetStack()
    {
        var resourceGroup = new ResourceGroup($"rg-{Deployment.Instance.ProjectName}-{Deployment.Instance.StackName}");

        ResourceGroupName = resourceGroup.Name;

        var storageAccount = new StorageAccount($"st{Deployment.Instance.ProjectName}{Deployment.Instance.StackName}", new StorageAccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new SkuArgs
            {
                Name = SkuName.Standard_LRS
            },
            Kind = Kind.StorageV2
        });

        // Single P1v3 plan shared by API, Web, and Functions apps — P1v3 natively supports deployment slots
        var webAppServicePlan = new AppServicePlan("plan-web", new AppServicePlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Kind = "Windows",
            Sku = new SkuDescriptionArgs
            {
                Tier = "PremiumV3",
                Name = "P1v3"
            }
        });

        // ── API App Service ────────────────────────────────────────────────────────
        var apiApp = new WebApp("api-jjgnet-broadcast", new WebAppArgs
        {
            Name = "api-jjgnet-broadcast",
            ResourceGroupName = resourceGroup.Name,
            ServerFarmId = webAppServicePlan.Id,
            Identity = new ManagedServiceIdentityArgs
            {
                Type = Pulumi.AzureNative.Web.ManagedServiceIdentityType.SystemAssigned
            },
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new NameValuePairArgs { Name = "ASPNETCORE_ENVIRONMENT", Value = "Production" }
                }
            }
        });

        var apiStagingSlot = new WebAppSlot("api-staging", new WebAppSlotArgs
        {
            Name = apiApp.Name,
            Slot = "staging",
            ResourceGroupName = resourceGroup.Name,
            ServerFarmId = webAppServicePlan.Id,
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new NameValuePairArgs { Name = "ASPNETCORE_ENVIRONMENT", Value = "Staging" }
                }
            }
        });

        // ── Web App Service ────────────────────────────────────────────────────────
        var webApp = new WebApp("web-jjgnet-broadcast", new WebAppArgs
        {
            Name = "web-jjgnet-broadcast",
            ResourceGroupName = resourceGroup.Name,
            ServerFarmId = webAppServicePlan.Id,
            Identity = new ManagedServiceIdentityArgs
            {
                Type = Pulumi.AzureNative.Web.ManagedServiceIdentityType.SystemAssigned
            },
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new NameValuePairArgs { Name = "ASPNETCORE_ENVIRONMENT", Value = "Production" }
                }
            }
        });

        var webStagingSlot = new WebAppSlot("web-staging", new WebAppSlotArgs
        {
            Name = webApp.Name,
            Slot = "staging",
            ResourceGroupName = resourceGroup.Name,
            ServerFarmId = webAppServicePlan.Id,
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new NameValuePairArgs { Name = "ASPNETCORE_ENVIRONMENT", Value = "Staging" }
                }
            }
        });

        // ── Functions App ──────────────────────────────────────────────────────────
        var functionApp = new WebApp("jjgnet-broadcast", new WebAppArgs
        {
            Name = "jjgnet-broadcast",
            Kind = "FunctionApp",
            ResourceGroupName = resourceGroup.Name,
            ServerFarmId = webAppServicePlan.Id,
            Identity = new ManagedServiceIdentityArgs
            {
                Type = Pulumi.AzureNative.Web.ManagedServiceIdentityType.SystemAssigned
            },
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new NameValuePairArgs { Name = "runtime",                         Value = "dotnet-isolated" },
                    new NameValuePairArgs { Name = "FUNCTIONS_WORKER_RUNTIME",        Value = "dotnet-isolated" },
                    new NameValuePairArgs { Name = "FUNCTIONS_EXTENSION_VERSION",     Value = "~4" },
                    new NameValuePairArgs { Name = "AZURE_FUNCTIONS_ENVIRONMENT",     Value = "Production" },
                    new NameValuePairArgs { Name = "AzureWebJobsStorage__accountName",Value = storageAccount.Name }
                }
            }
        });

        var functionStagingSlot = new WebAppSlot("functions-staging", new WebAppSlotArgs
        {
            Name = functionApp.Name,
            Slot = "staging",
            Kind = "FunctionApp",
            ResourceGroupName = resourceGroup.Name,
            ServerFarmId = webAppServicePlan.Id,
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new NameValuePairArgs { Name = "runtime",                         Value = "dotnet-isolated" },
                    new NameValuePairArgs { Name = "FUNCTIONS_WORKER_RUNTIME",        Value = "dotnet-isolated" },
                    new NameValuePairArgs { Name = "FUNCTIONS_EXTENSION_VERSION",     Value = "~4" },
                    new NameValuePairArgs { Name = "AZURE_FUNCTIONS_ENVIRONMENT",     Value = "Staging" },
                    new NameValuePairArgs { Name = "AzureWebJobsStorage__accountName",Value = storageAccount.Name }
                }
            }
        });

        // Give storage access to the function app (production slot identity)
        var storageBlobDataOwnerRole = new RoleAssignment("storageBlobDataOwner", new RoleAssignmentArgs
        {
            PrincipalId = functionApp.Identity.Apply(i => i.PrincipalId),
            PrincipalType = PrincipalType.ServicePrincipal,
            RoleDefinitionId = "/providers/Microsoft.Authorization/roleDefinitions/b7e6dc6d-f1e8-4753-8033-0f276bb0955b",
            Scope = storageAccount.Id
        });

        var twitterQueue = new Queue("twitter-tweets-to-send", new QueueArgs
        {
            AccountName = storageAccount.Name,
            ResourceGroupName = resourceGroup.Name,
        });

        var facebookQueue = new Queue("facebook-post-status-to-page", new QueueArgs
        {
            AccountName = storageAccount.Name,
            ResourceGroupName = resourceGroup.Name,
        });

        var linkedInPostLinkQueue = new Queue("linkedin-post-link", new QueueArgs
        {
            AccountName = storageAccount.Name,
            ResourceGroupName = resourceGroup.Name,
        });

        var linkedInPostTextQueue = new Queue("linkedin-post-text", new QueueArgs
        {
            AccountName = storageAccount.Name,
            ResourceGroupName = resourceGroup.Name,
        });

        var linkedInPostImageQueue = new Queue("linkedin-post-image", new QueueArgs
        {
            AccountName = storageAccount.Name,
            ResourceGroupName = resourceGroup.Name,
        });

        var blueskyPostToSendQueue = new Queue("bluesky-post-to-send", new QueueArgs
        {
            AccountName = storageAccount.Name,
            ResourceGroupName = resourceGroup.Name,
        });
    }
}

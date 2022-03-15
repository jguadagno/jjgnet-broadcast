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
    public JjgnetStack()
    {
        var resourceGroup = new ResourceGroup($"rg-{Deployment.Instance.ProjectName}-{Deployment.Instance.StackName}");

        var storageAccount = new StorageAccount($"st{Deployment.Instance.ProjectName}{Deployment.Instance.StackName}", new StorageAccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new SkuArgs
            {
                Name = SkuName.Standard_LRS
            },
            Kind = Kind.StorageV2
        });

        var appServicePlan = new AppServicePlan($"plan-{Deployment.Instance.ProjectName}-{Deployment.Instance.StackName}", new AppServicePlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Kind = "Windows",
            Sku = new SkuDescriptionArgs
            {
                Tier = "Dynamic",
                Name = "Y1"
            }
        });

        var functionApp = new WebApp($"func-{Deployment.Instance.ProjectName}-{Deployment.Instance.StackName}", new WebAppArgs
        {
            Kind = "FunctionApp",
            ResourceGroupName = resourceGroup.Name,
            ServerFarmId = appServicePlan.Id,
            Identity = new ManagedServiceIdentityArgs
            {
                Type = Pulumi.AzureNative.Web.ManagedServiceIdentityType.SystemAssigned
            },
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new NameValuePairArgs
                    {
                        Name = "runtime",
                        Value = "dotnet",
                    },
                    new NameValuePairArgs
                    {
                        Name = "FUNCTIONS_WORKER_RUNTIME",
                        Value = "dotnet",
                    },
                    new NameValuePairArgs
                    {
                        Name = "FUNCTIONS_EXTENSION_VERSION",
                        Value = "~3"
                    },
                    new NameValuePairArgs
                    {
                        Name = "AzureWebJobsStorage__accountName",
                        Value = storageAccount.Name
                    }
                },
            },
        });

        // Give storage access to the function app
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
    }
}

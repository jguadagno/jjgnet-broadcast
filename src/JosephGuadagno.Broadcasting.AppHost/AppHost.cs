using Azure.Provisioning;
using Azure.Provisioning.EventGrid;
using Azure.Provisioning.Storage;

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("AzureStorage");
var tableStorage = storage.AddTables("LogTable");
var queueStorage = storage.AddQueues("QueueStorage");
var blobStorage = storage.AddBlobs("BlobStorage");
storage.RunAsEmulator(azurite =>
{
    azurite.WithLifetime(ContainerLifetime.Persistent);
    azurite.WithDataVolume();
});

var sql = builder.AddSqlServer("JJGNetSqlServer")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("JJGNetSqlData");

var path = builder.AppHostDirectory;
var sqlText = string.Concat(
    File.ReadAllText(Path.Combine(path, @"../../scripts/database/database-create.sql")),
    " ",
    File.ReadAllText(Path.Combine(path, @"../../scripts/database/table-create.sql")),
    " ",
    File.ReadAllText(Path.Combine(path, @"../../scripts/database/data-create.sql")));

var db = sql.AddDatabase("JJGNet")
    .WithCreationScript(sqlText);

var api = builder.AddProject<JosephGuadagno_Broadcasting_Api>("josephguadagno-broadcasting-api")
    .WithEnvironment("ConnectionStrings__JJGNetDatabaseSqlServer", db)
    .WithEnvironment("Settings__LoggingStorageAccount", tableStorage)
    .WaitFor(blobStorage)
    .WaitFor(queueStorage)
    .WaitFor(blobStorage)
    .WaitFor(db);

// Event Grid topics — provisioned in Azure via azd; for local dev configure endpoints/keys
// via local.settings.json or user secrets (see event-grid-simulator-config.json for local setup).
// Topic keys must be set separately in Azure App Service settings or Key Vault.
var eventGridTopicNames = new[]
{
    "new-random-post",
    "new-speaking-engagement",
    "new-syndication-feed-item",
    "new-youtube-item",
    "scheduled-item-fired"
};

var eventGridTopics = eventGridTopicNames
    .Select((topicName, index) =>
    {
        var bicepName = topicName.Replace("-", string.Empty);
        var resource = builder.AddAzureInfrastructure(bicepName, infra =>
        {
            var topic = new EventGridTopic(bicepName)
            {
                Tags = { { "aspire-resource-name", topicName } }
            };
            topic.Name = topicName;
            infra.Add(topic);
            infra.Add(new ProvisioningOutput("topicEndpoint", typeof(string)) { Value = topic.Endpoint });
        });
        return (index, topicName, resource);
    })
    .ToList();

var functions = builder.AddAzureFunctionsProject<JosephGuadagno_Broadcasting_Functions>("Functions")
    .WithRoleAssignments(storage,
        // Storage Account Contributor and Storage Blob Data Owner roles are required by the Azure Functions host
        StorageBuiltInRole.StorageAccountContributor, StorageBuiltInRole.StorageBlobDataOwner,
        // Queue Data Contributor role is required to send messages to the queue
        StorageBuiltInRole.StorageQueueDataContributor)
    .WithHostStorage(storage)
    .WithReference(tableStorage)
    .WithReference(blobStorage)
    .WithReference(queueStorage)
    .WithExternalHttpEndpoints()
    .WaitFor(db)
    .WaitFor(tableStorage)
    .WaitFor(blobStorage)
    .WaitFor(queueStorage)
    .WithEnvironment("ConnectionStrings__JJGNetDatabaseSqlServer", db)
    .WithEnvironment("Settings__LoggingStorageAccount", tableStorage);

foreach (var (index, topicName, resource) in eventGridTopics)
{
    functions
        .WithEnvironment($"EventGridTopics__TopicEndpointSettings__{index}__TopicName", topicName)
        .WithEnvironment($"EventGridTopics__TopicEndpointSettings__{index}__Endpoint",
            resource.GetOutput("topicEndpoint"));
}

builder.AddProject<JosephGuadagno_Broadcasting_Web>("josephguadagno-broadcasting-web")
    .WithEnvironment("ConnectionStrings__JJGNetDatabaseSqlServer", db)
    .WithEnvironment("Settings__LoggingStorageAccount", tableStorage)
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
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
    .WithEnvironment("Settings__JJGNetDatabaseSqlServer", db)
    .WithEnvironment("Settings__StorageAccount", tableStorage)
    .WaitFor(blobStorage)
    .WaitFor(queueStorage)
    .WaitFor(blobStorage)
    .WaitFor(db);

builder.AddAzureFunctionsProject<JosephGuadagno_Broadcasting_Functions>("Functions")
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
    .WithEnvironment("Settings__JJGNetDatabaseSqlServer", db)
    .WithEnvironment("Settings__StorageAccount", tableStorage);

builder.AddProject<JosephGuadagno_Broadcasting_Web>("josephguadagno-broadcasting-web")
    .WithEnvironment("Settings__JJGNetDatabaseSqlServer", db)
    .WithEnvironment("Settings__StorageAccount", tableStorage)
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
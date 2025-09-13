var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("AzureStorage")
    .RunAsEmulator();
var table = storage.AddTables("LogTable");

var sql = builder.AddSqlServer("SqlServer")   
    .WithLifetime(ContainerLifetime.Persistent);

var path = builder.AppHostDirectory;
var sqlText = string.Concat(
    File.ReadAllText(Path.Combine(path, @"..\..\scripts\database-create.sql")), 
    " ",
    File.ReadAllText(Path.Combine(path, @"..\..\scripts\table-create.sql")),
    " ",
    File.ReadAllText(Path.Combine(path, @"..\..\scripts\data-create.sql")));

var db = sql.AddDatabase("JJGNet")
    .WithCreationScript(sqlText);

var api = builder.AddProject<Projects.JosephGuadagno_Broadcasting_Api>("josephguadagno-broadcasting-api")
    .WithEnvironment("Settings__JJGNetDatabaseSqlServer", db)
    .WithEnvironment("Settings__StorageAccount", table)
    .WaitFor(table)
    .WaitFor(db);

builder.AddProject<Projects.JosephGuadagno_Broadcasting_Web>("josephguadagno-broadcasting-web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

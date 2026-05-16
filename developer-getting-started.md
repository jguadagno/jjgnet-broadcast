# Developers Getting Started Guide

This document will cover what you need on your development machine to get started with the project. 
As well as how to set up the project for development.

## Required Software

Outside an IDE, you'll need the following software installed on your machine.

* Docker Desktop
* Microsoft Azure Storage Explorer (`winget install Microsoft.AzureStorageExplorer`)
* Microsoft LibMan (`dotnet tool install -g Microsoft.Web.LibraryManager.Cli`)
* [Azure Event Grid Simulator](https://github.com/pm7y/AzureEventGridSimulator)
  * For more information on how to run the simulator, check out the [readme](./src/JosephGuadagno.Broadcasting.Functions/readme.md) in the functions’ project.

## Setup

### Secrets

You'll need to create the User Secrets for the following projects

* JosephGuadagno.Broadcasting.Api
* JosephGuadagno.Broadcasting.Functions (local.settings.json)
* JosephGuadagno.Broadcasting.Web

Plus, if you need to generate the Facebook secrets using the [facebook-access-tokens](./src/facebook-access-tokens.http) file,
you'll need to generate a `facebook.private.env.json` file that looks like this

```json
{
  "facebook-dev": {
    "app-id": "",
    "app-secret": "",
    "page-id": "",
    "client-token": "",
    "short-lived-access-token": "",
    "long-lived-access-token": "",
    "page-access-token": ""
    }
}
```

The `page-access-token` is the secret that is needed for the *JosephGuadagno.Broadcasting.Functions* project `FacebookPageAccessToken` secret.

#### Certificate Generation

Run the following command in a local terminal

```Powershell
New-SelfSignedCertificate -KeyFriendlyName "JosephGuadagno-Broadcasting" -DnsName "josephguadagno-broadcasting.net" -CertStoreLocation "Cert:\CurrentUser\My"
```

This will generate a local certificate.  This certificate needs to be exported and registered in the Web api application registration.

* Open *Certificate Manager*
* Locate the certificate in the `Personal\Certificates` folder
* Open the certificate
* Click *Details*
* Click *Copy to File...*
* Click *Next*
* Select *No*
* Select *DER encoded binary*
* Click *Next*
* Select a file name and location
* Click *Next*
* Click *Finish*

### Database

The database is created automatically by Aspire on first run using the scripts in
`scripts/database/`. No manual setup is required for fresh environments.

#### Applying migrations to an existing database

When you pull new code that adds tables or columns, you may need to apply a migration to
your local database. Migration scripts live in `scripts/database/migrations/` and are safe
to run multiple times (all use `IF NOT EXISTS` or equivalent guards).

Connect to the local SQL Server instance and run the relevant migration script. You can find
the connection details in the Aspire dashboard under the `JJGNetSqlServer` resource. For
example, using `sqlcmd`:

```powershell
sqlcmd -S localhost,<port> -U sa -P <password> -d JJGNet -i scripts\database\migrations\<migration-file>.sql
```

Alternatively, delete the Docker volume (`JJGNetSqlData`) and restart Aspire so the database
is recreated from scratch:

```powershell
docker volume rm JJGNetSqlData
```

> **Note:** Deleting the volume destroys all local data. Use the migration approach to
> preserve any test data you have saved.

#### Legacy manual scripts (no longer needed for new environments)

These were the original setup scripts before Aspire took over database creation. They are
kept for reference only.

* [database-create.sql](./scripts/database/database-create.sql)
* [table-create.sql](./scripts/database/table-create.sql)
* [data-seed.sql](./scripts/database/data-seed.sql)

### Azure Storage

#### Tables

Create the following tables

##### Logging

* PartitionKey
* RowKey
* Timestamp
* EventTime
* Level
* MessageTemplate
* RenderedMessage
* Data
* requestId
* status
* reasonPhrase
* headers
* seconds
* EventSourceEvent
* EventId
* SourceContext
* MachineName
* ThreadId
* EnvironmentName
* AssemblyName
* AssemblyVersion
* Application
* messageId
* Category
* LogLevel
* MS_FunctionInvocationId
* MS_FunctionName
* MS_Event
* MS_HostInstanceId
* MS_TriggerDetails
* MS_OperationContext

#### Queues

Create the following queues

* `facebook-post-status-to-page`
* `twitter-tweets-to-send`
* `linkedin-post-link`
* `linkedin-post-text`
* `linkedin-post-image`
* `bluesky-post-to-send`

#### Blobs

*No blobs or containers are needed*

### Website

Restore Libman packages

```bash
cd src/JosephGuadagno.Broadcasting.Web
libman restore
```

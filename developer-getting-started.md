# Developers Getting Started Guide

This document will cover what you need on your development machine to get started with the project. 
As well as how to setup the project for development.

## Required Software

* Docker Desktop
  * Microsoft SQL Server
  * Azurite
* Microsoft Azure Storage Explorer (`winget install Microsoft.AzureStorageExplorer`)
* NPM (`winget install NPM.NodeJS`)
* Microsoft LibMan (`dotnet tool install -g Microsoft.Web.LibraryManager.Cli`)
* ngrok (`choco install ngrok`)

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

The `page-access-token` is the secret that is needed for the *JosephGuadagno.Broadcasting.Functions* project `FacebookPageAccessToken` secret..

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

Run the following scripts in the database. This is no longer needed, as the database is now created with Aspire

* [database-create.sql](./scripts/database/database-create.sql)
* [table-create.sql](./scripts/database/table-create.sql)
* [data-create.sql](./scripts/database/data-create.sql)

### Azure Storage

#### Tables

Create the following tables

##### Configuration

Columns

* PartitionKey
* RowKey
* Timestamp
* LastCheckedFeed
* LastItemAddedOrUpdated

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

##### SourceData

* PartitionKey
* RowKey
* Timestamp
* AddedOn
* Author
* PublicationDate
* ShortenedUrl
* Title
* UpdatedOnDate
* Url
* Tags

#### Queues

Create the following queues

* `facebook-post-status-to-page`
* `twitter-tweets-to-send`

#### Blobs

*No blobs or containers are needed*

### Web Site

Restore Libman packages

```bash
cd src/JosephGuadagno.Broadcasting.Web
libman restore
```

# Developers Getting Started Guide

This document will cover what you need on your development machine to get started with the project. 
As well as how to setup the project for development.

## Required Software

* Microsoft SQL Server 2019 or later
* Microsoft Azure Storage Explorer (`winget install Microsoft.AzureStorageExplorer`)
* NPM (`winget install NPM.NodeJS`)
* Azurite (`npm install -g azurite`)
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

### Database

Run the following scripts in the database

* [database-create.sql](./scripts/database-create.sql)
* [table-create.sql](./scripts/table-create.sql)

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

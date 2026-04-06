@description('Azure region for the primary storage account.')
param location string

@description('Name of the main storage account (queues, tables, blobs).')
param storageAccountName string

@description('Storage SKU / replication tier.')
@allowed(['Standard_LRS', 'Standard_GRS', 'Standard_RAGRS', 'Standard_ZRS', 'Premium_LRS'])
param storageSkuName string = 'Standard_RAGRS'

@description('Name of the Functions storage account.')
param functionsStorageAccountName string

@description('Storage SKU for the Functions storage account.')
param functionsStorageSkuName string = 'Standard_LRS'

@description('Resource tags.')
param tags object = {}

// Discovered values (production):
//   storageAccountName          = 'jjgnet'   (West US 2, Standard_RAGRS, StorageV2, allowBlobPublicAccess: true)
//   functionsStorageAccountName = 'jjgnetbeb6' (West US, Standard_LRS, Storage kind)

// Known queues on jjgnet storage account:
//   - facebook-post-status-to-page
//   - twitter-tweets-to-send
//   - linkedin-post-status-to-page
//   - linkedin-queue-post-photo

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  tags: tags
  sku: {
    name: storageSkuName
  }
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-04-01' = {
  parent: storageAccount
  name: 'default'
}

resource queueFacebook 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-04-01' = {
  parent: queueService
  name: 'facebook-post-status-to-page'
}

resource queueTwitter 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-04-01' = {
  parent: queueService
  name: 'twitter-tweets-to-send'
}

resource queueLinkedIn 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-04-01' = {
  parent: queueService
  name: 'linkedin-post-status-to-page'
}

resource queueLinkedInPhoto 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-04-01' = {
  parent: queueService
  name: 'linkedin-queue-post-photo'
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-04-01' = {
  parent: storageAccount
  name: 'default'
}

resource tableConfiguration 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-04-01' = {
  parent: tableService
  name: 'Configuration'
}

resource tableLogging 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-04-01' = {
  parent: tableService
  name: 'Logging'
}

// Functions storage account (separate account used by the Azure Functions runtime)
resource functionsStorageAccount 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: functionsStorageAccountName
  location: location
  kind: 'StorageV2'
  tags: tags
  sku: {
    name: functionsStorageSkuName
  }
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    accessTier: 'Hot'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

output storageAccountId string = storageAccount.id
output storageAccountName string = storageAccount.name
output functionsStorageAccountId string = functionsStorageAccount.id
output functionsStorageAccountName string = functionsStorageAccount.name

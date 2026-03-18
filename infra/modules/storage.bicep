// storage.bicep — Azure Storage Account for queues, tables, and function host storage

@description('Location for all resources')
param location string

@description('Name suffix / environment tag (alphanumeric, max ~8 chars)')
param environmentName string

@description('Resource tags to apply to all resources')
param tags object = {}

// Storage account used by both Functions runtime and application (queues + tables)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'stjjgnet${environmentName}'
  location: location
  kind: 'StorageV2'
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    publicNetworkAccess: 'Enabled'
  }
}

// --- Table Storage ---
resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource tableConfiguration 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: 'Configuration'
}

resource tableSourceData 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: 'SourceData'
}

resource tableLogging 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: 'Logging'
}

// --- Queue Storage ---
resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource queueTwitter 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: 'twitter-tweets-to-send'
}

resource queueFacebook 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: 'facebook-post-status-to-page'
}

resource queueLinkedInText 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: 'linkedin-post-text'
}

resource queueLinkedInLink 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: 'linkedin-post-link'
}

resource queueLinkedInImage 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: 'linkedin-post-image'
}

output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id
output storageConnectionStringSecretRef string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'

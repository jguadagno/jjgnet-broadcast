@description('Azure region for the SQL Server.')
param location string

@description('Name of the SQL Server. Discovered value: r4bv7wtt6u')
param sqlServerName string

@description('Name of the primary database.')
param databaseName string = 'JJGNet'

@description('SQL Server administrator login name.')
param administratorLogin string

@description('SQL Server administrator login password.')
@secure()
param administratorLoginPassword string

@description('Azure AD Object ID of the AAD admin (optional — set to enable AAD auth).')
param aadAdminObjectId string = ''

@description('Azure AD login name for the AAD admin (optional).')
param aadAdminLogin string = ''

@description('Database SKU / service tier.')
param databaseSku object = {
  name: 'S0'
  tier: 'Standard'
  capacity: 10
}

@description('Resource tags.')
param tags object = {}

// Discovered values (production):
//   sqlServerName             = 'r4bv7wtt6u'
//   fullyQualifiedDomainName  = 'r4bv7wtt6u.database.windows.net'
//   administratorLogin        = 'jguadagno'
//   SQL Server version        = 12.0
//   location                  = 'westus'

resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: databaseSku
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 268435456000
    zoneRedundant: false
    readScale: 'Disabled'
  }
}

// Allow Azure services to access SQL Server
resource sqlFirewallRuleAzureServices 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name
output sqlServerId string = sqlServer.id

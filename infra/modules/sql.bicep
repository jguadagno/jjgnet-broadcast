// sql.bicep — Azure SQL Server + JJGNet database

@description('Location for all resources')
param location string

@description('Name suffix / environment tag')
param environmentName string

@description('Resource tags to apply to all resources')
param tags object = {}

@description('SQL Server administrator login name')
param sqlAdminLogin string

@description('SQL Server administrator password — must be a strong password')
@secure()
param sqlAdminPassword string

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'sql-jjgnet-${environmentName}'
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services to reach the SQL server (required for App Service + Functions)
resource firewallAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'JJGNet'
  location: location
  tags: tags
  sku: {
    name: 'S1'
    tier: 'Standard'
    capacity: 20
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 268435456 // 256 MB
    zoneRedundant: false
    readScale: 'Disabled'
  }
}

output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output sqlConnectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

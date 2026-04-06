@description('Azure region for Application Insights.')
param location string

@description('Name of the Application Insights component.')
param appInsightsName string

@description('Resource ID of the Log Analytics workspace to link to.')
param logAnalyticsWorkspaceId string

@description('Application type.')
@allowed(['web', 'other'])
param applicationType string = 'web'

@description('Resource tags.')
param tags object = {}

resource appInsights 'microsoft.insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: logAnalyticsWorkspaceId
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output appInsightsId string = appInsights.id
output appInsightsName string = appInsights.name
output instrumentationKey string = appInsights.properties.InstrumentationKey
output connectionString string = appInsights.properties.ConnectionString

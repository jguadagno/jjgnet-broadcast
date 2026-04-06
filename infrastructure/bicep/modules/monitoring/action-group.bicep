@description('Name of the action group.')
param actionGroupName string

@description('Short display name for the action group (max 12 chars).')
@maxLength(12)
param groupShortName string

@description('Email receivers for alert notifications.')
param emailReceivers array = []

@description('Resource tags.')
param tags object = {}

// Production values:
// actionGroupName = 'jjgnet_broadcasting'
// groupShortName  = 'jjgnet'
// emailReceivers  = [{ name: 'Notify Joe_-EmailAction-', emailAddress: 'jguadagno@hotmail.com', useCommonAlertSchema: true }]

resource actionGroup 'microsoft.insights/actionGroups@2023-09-01-preview' = {
  name: actionGroupName
  location: 'global'
  tags: tags
  properties: {
    groupShortName: groupShortName
    enabled: true
    emailReceivers: emailReceivers
    smsReceivers: []
    webhookReceivers: []
    azureAppPushReceivers: []
    armRoleReceivers: []
  }
}

output actionGroupId string = actionGroup.id
output actionGroupName string = actionGroup.name

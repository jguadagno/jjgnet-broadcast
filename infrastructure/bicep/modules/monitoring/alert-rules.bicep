@description('Resource ID of the Action Group for alert notifications.')
param actionGroupId string

@description('Resource ID of the Application Insights instance to monitor.')
param appInsightsId string

@description('Resource tags.')
param tags object = {}

// Smart Detector Alert Rules (Failure Anomalies) — one per App Service site
// Production alert rules found in discovery:
//   - 'Failure Anomalies - api-jjgnet-broadcast'
//   - 'Failure Anomalies - web-jjgnet-broadcast'
//   - 'Failure Anomalies - jjgnet-broadcast'

resource failureAnomalyAlertsApi 'microsoft.alertsmanagement/smartDetectorAlertRules@2021-04-01' = {
  name: 'Failure Anomalies - api-jjgnet-broadcast'
  location: 'global'
  tags: tags
  properties: {
    description: 'Failure Anomalies notifies you of an unusual rise in the rate of failed HTTP requests or dependency calls.'
    state: 'Enabled'
    severity: 'Sev3'
    frequency: 'PT1M'
    detector: {
      id: 'FailureAnomaliesDetector'
    }
    scope: [
      appInsightsId
    ]
    actionGroups: {
      groupIds: [
        actionGroupId
      ]
    }
  }
}

resource failureAnomalyAlertsWeb 'microsoft.alertsmanagement/smartDetectorAlertRules@2021-04-01' = {
  name: 'Failure Anomalies - web-jjgnet-broadcast'
  location: 'global'
  tags: tags
  properties: {
    description: 'Failure Anomalies notifies you of an unusual rise in the rate of failed HTTP requests or dependency calls.'
    state: 'Enabled'
    severity: 'Sev3'
    frequency: 'PT1M'
    detector: {
      id: 'FailureAnomaliesDetector'
    }
    scope: [
      appInsightsId
    ]
    actionGroups: {
      groupIds: [
        actionGroupId
      ]
    }
  }
}

resource failureAnomalyAlertsFunctions 'microsoft.alertsmanagement/smartDetectorAlertRules@2021-04-01' = {
  name: 'Failure Anomalies - jjgnet-broadcast'
  location: 'global'
  tags: tags
  properties: {
    description: 'Failure Anomalies notifies you of an unusual rise in the rate of failed HTTP requests or dependency calls.'
    state: 'Enabled'
    severity: 'Sev3'
    frequency: 'PT1M'
    detector: {
      id: 'FailureAnomaliesDetector'
    }
    scope: [
      appInsightsId
    ]
    actionGroups: {
      groupIds: [
        actionGroupId
      ]
    }
  }
}

output alertRuleIds array = [
  failureAnomalyAlertsApi.id
  failureAnomalyAlertsWeb.id
  failureAnomalyAlertsFunctions.id
]

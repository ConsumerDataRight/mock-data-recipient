targetScope = 'subscription'

// The main bicep module to provision Azure resources.
// For a more complete walkthrough to understand how this file works with azd,
// see https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/make-azd-compatible?pivots=azd-create

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

// Optional parameters to override the default azd resource naming conventions.
// Add the following to main.parameters.json to provide values:
// "resourceGroupName": {
//      "value": "myGroupName"
// }
param resourceGroupName string = ''

@secure()
param sqlAdminPassword string
param sqlAdminLogin string

var abbrs = loadJsonContent('./abbreviations.json')

// tags that should be applied to all resources.
var tags = {
  // Tag all resources with the environment name.
  'azd-env-name': environmentName
  'force-update': 'true'
}

// Generate a unique token to be used in naming resources.
// Remove linter suppression after using.
#disable-next-line no-unused-vars
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// Add resources to be provisioned below.

//simple architecture - an app-service with some webapps, functions, a queue, database and storage, wrapped in a vnet to facilitate private endpoints.
module network 'network.bicep' = {
  name: '${deployment().name}-network'
  scope: rg
  params: {
    resourceToken: resourceToken
    location: location
    tags: tags
    vnetAbbreviation: abbrs.networkVirtualNetworks
  }
}

module apps 'application.bicep' = {
  name: '${deployment().name}-application'
  scope: rg
  params: {
    resourceToken: resourceToken
    location: location
    tags: tags
    managedIdentityAbbreviation: abbrs.managedIdentityUserAssignedIdentities
    sqlServerAbbreviation: abbrs.sqlServers
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    containerAppSubnet: network.outputs.containerAppSubnetId
    containerEnvAbbrv: 'cenv'
    logAnalyticsAbbr: 'loga'
  }
}

module functions 'functions.bicep' = {
  name: '${deployment().name}-functions'
  scope: rg
  params: {
    resourceToken: resourceToken
    location: location
    tags: tags
    applicationUamiName: apps.outputs.applicationUamiName
    sqlServerName: apps.outputs.sqlServerName
    dataRecipientDbName: apps.outputs.dataRecipientDbName
    functionAppAbbrv: abbrs.webSitesFunctions
    registerHostName: apps.outputs.registerHostName
    storageApprv: abbrs.storageStorageAccounts
    appInsightsConnectionString: apps.outputs.appInsightsConnectionString
    dataRecipientHostName: apps.outputs.dataRecipientHostName
  }
}

// Add outputs from the deployment here, if needed.
//
// This allows the outputs to be referenced by other bicep deployments in the deployment pipeline,
// or by the local machine as a way to reference created resources in Azure for local development.
// Secrets should not be added here.
//
// Outputs are automatically saved in the local azd environment .env file.
// To see these outputs, run `azd env get-values`,  or `azd env get-values --output json` for json output.
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name

targetScope = 'resourceGroup'

param resourceToken string
param location string = resourceGroup().location
param tags object = {}
param functionAppAbbrv string
param storageApprv string
param registerHostName string
param dataRecipientHostName string
param sqlServerName string
param dataRecipientDbName string
param applicationUamiName string
param appInsightsConnectionString string

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' existing = {
  name: sqlServerName

  resource dataRecipientDb 'databases' existing = {
    name: dataRecipientDbName
  }
}

resource applicationUami 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' existing = {
  name: applicationUamiName
}

var consumerRightsDataRecipientDatabaseConnectionString = 'Server=tcp:${sqlServer.name}${environment().suffixes.sqlServerHostname},1433; Authentication=Active Directory MSI; Encrypt=True; User Id=${applicationUami.properties.clientId}; Database=${sqlServer::dataRecipientDb.name}'

resource functionAppStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: '${storageApprv}${resourceToken}fcnstg'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }

  resource blob 'blobServices' = {
    name: 'default'
    properties: {}
  }
}

resource functionAppServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: '${functionAppAbbrv}${resourceToken}plan'
  location: location
  kind: 'linux'
  sku: {
    name: 'S1'
    capacity: 1
  }
  properties: {
    reserved: true
    zoneRedundant: false
  }
}

resource discoverDataHoldersFunction 'Microsoft.Web/sites@2024-04-01' = {
  name: '${functionAppAbbrv}${resourceToken}ddh'
  location: location
  tags: union(tags, { 'azd-service-name': 'discover-data-holders' })
  kind: 'functionapp,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${applicationUami.id}': {}
    }
  }
  properties: {
    serverFarmId: functionAppServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      alwaysOn: true

      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionAppStorage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${functionAppStorage.listKeys().keys[0].value}'
        }
        {
          name: 'StorageConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionAppStorage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${functionAppStorage.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'DataRecipient_DB_ConnectionString'
          value: consumerRightsDataRecipientDatabaseConnectionString
        }
        {
          name: 'DataRecipient_Logging_DB_ConnectionString'
          value: consumerRightsDataRecipientDatabaseConnectionString
        }
        {
          name: 'Register_Token_Endpoint'
          value: '${registerHostName}/idp/connect/token'
        }
        {
          name: 'Register_Get_DH_Brands'
          value: '${registerHostName}/cdr-register/v1/all/data-holders/brands'
        }
        {
          name: 'Register_Get_DH_Brands_XV'
          value: '2'
        }
        {
          name: 'Software_Product_Id'
          value: 'c6327f87-687a-4369-99a4-eaacd3bb8210'
        }
        {
          name: 'Client_Certificate'
          value: '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'
        }
        {
          name: 'Client_Certificate_Password'
          value: '#M0ckDataRecipient#'
        }
        {
          name: 'Signing_Certificate'
          value: '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'
        }
        {
          name: 'Signing_Certificate_Password'
          value: '#M0ckDataRecipient#'
        }
        {
          name: 'Ignore_Server_Certificate_Errors'
          value: 'true'
        }
        {
          name: 'QueueName'
          value: 'dynamicclientregistration'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'Schedule'
          value: '*/5 * * * *'
        }
      ]
    }
  }
}


resource dcrFunction 'Microsoft.Web/sites@2024-04-01' = {
  name: '${functionAppAbbrv}${resourceToken}dcr'
  location: location
  tags: union(tags, { 'azd-service-name': 'dcr' })
  kind: 'functionapp,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${applicationUami.id}': {}
    }
  }
  properties: {
    serverFarmId: functionAppServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      alwaysOn: true

      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionAppStorage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${functionAppStorage.listKeys().keys[0].value}'
        }
        {
          name: 'StorageConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionAppStorage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${functionAppStorage.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'DataRecipient_DB_ConnectionString'
          value: consumerRightsDataRecipientDatabaseConnectionString
        }
        {
          name: 'DataRecipient_Logging_DB_ConnectionString'
          value: consumerRightsDataRecipientDatabaseConnectionString
        }
        {
          name: 'Register_Token_Endpoint'
          value: '${registerHostName}/idp/connect/token'
        }
        {
          name: 'Register_Get_SSA_Endpoint'
          value: '${registerHostName}/cdr-register/v1/all/data-recipients/brands/'
        }
        {
          name: 'Register_Get_SSA_XV'
          value: '3'
        }
        {
          name: 'Brand_Id'
          value: 'ffb1c8ba-279e-44d8-96f0-1bc34a6b436f'
        }
        {
          name: 'Software_Product_Id'
          value: 'c6327f87-687a-4369-99a4-eaacd3bb8210'
        }
        {
          name: 'Client_Certificate'
          value: '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'
        }
        {
          name: 'Client_Certificate_Password'
          value: '#M0ckDataRecipient#'
        }
        {
          name: 'Signing_Certificate'
          value: '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'
        }
        {
          name: 'Signing_Certificate_Password'
          value: '#M0ckDataRecipient#'
        }
        {
          name: 'Redirect_Uris'
          value: '${dataRecipientHostName}/consent/callback'
        }
        {
          name: 'Retries'
          value: '3'
        }
        {
          name: 'Ignore_Server_Certificate_Errors'
          value: 'true'
        }
        {
          name: 'QueueName'
          value: 'dynamicclientregistration'
        }
        {
          name: 'Schedule'
          value: '*/5 * * * *'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
      ]
    }
  }
}

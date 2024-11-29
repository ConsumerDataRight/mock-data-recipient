targetScope = 'resourceGroup'

param resourceToken string
param location string = resourceGroup().location
param tags object = {}
param managedIdentityAbbreviation string
param sqlServerAbbreviation string
param containerEnvAbbrv string
param containerAppSubnet string
param logAnalyticsAbbr string

@secure()
param sqlAdminPassword string
param sqlAdminLogin string

resource lanalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${logAnalyticsAbbr}${resourceToken}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  kind: 'web'
  location: location
  name: '${logAnalyticsAbbr}${resourceToken}-appi'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'BlueField'
    WorkspaceResourceId: lanalytics.id
    RetentionInDays: 30
  }
}


resource applicationUami 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' = {
  name: '${managedIdentityAbbreviation}${resourceToken}-app'
  location: location
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${sqlServerAbbreviation}${resourceToken}-consumerdata'
  location: location
  tags: tags
  properties: {
    version: '12.0'
    restrictOutboundNetworkAccess: 'Enabled'
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: false
      principalType: 'Application'
      tenantId: subscription().tenantId
      login: applicationUami.name
      sid: applicationUami.properties.principalId
    }
    publicNetworkAccess: 'Enabled'
  }

  resource allowAzure 'firewallRules' = {
    name: 'AllowAllWindowsAzureIps'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }

  resource dataHolderDb 'databases' = {
    name: 'DateHolder'
    location: location
    tags: tags
    sku: {
      name: 'S0'
    }
  }

  resource dataHolderAuthServerDb 'databases' = {
    name: 'DataHolderAuthServer'
    location: location
    tags: tags
    sku: {
      name: 'S0'
    }
  }

  resource dataRecipientDb 'databases' = {
    name: 'Recipient'
    location: location
    tags: tags
    sku: {
      name: 'S0'
    }
  }

  resource registerDb 'databases' = {
    name: 'Register'
    location: location
    tags: tags
    sku: {
      name: 'S0'
    }
  }
}

var consumerRightsDataHolderAuthServerDatabaseConnectionString = 'Server=tcp:${sqlServer.name}${environment().suffixes.sqlServerHostname},1433; Authentication=Active Directory MSI; Encrypt=True; User Id=${applicationUami.properties.clientId}; Database=${sqlServer::dataHolderAuthServerDb.name}'
var consumerRightsDataHolderDatabaseConnectionString = 'Server=tcp:${sqlServer.name}${environment().suffixes.sqlServerHostname},1433; Authentication=Active Directory MSI; Encrypt=True; User Id=${applicationUami.properties.clientId}; Database=${sqlServer::dataHolderDb.name}'
var consumerRightsDataRecipientDatabaseConnectionString = 'Server=tcp:${sqlServer.name}${environment().suffixes.sqlServerHostname},1433; Authentication=Active Directory MSI; Encrypt=True; User Id=${applicationUami.properties.clientId}; Database=${sqlServer::dataRecipientDb.name}'
var registerDatabaseConnectionString = 'Server=tcp:${sqlServer.name}${environment().suffixes.sqlServerHostname},1433; Authentication=Active Directory MSI; Encrypt=True; User Id=${applicationUami.properties.clientId}; Database=${sqlServer::registerDb.name}'

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${containerEnvAbbrv}-${resourceToken}'
  location: location
  tags: tags
  properties: {
    vnetConfiguration: {
      infrastructureSubnetId: containerAppSubnet
    }
  }
}

var registerAppName = 'mockregister'
var registerHostName = '${registerAppName}.${containerAppEnvironment.properties.defaultDomain}'

resource registerContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: registerAppName
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${applicationUami.id}': {}
    }
  }
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        transport: 'tcp'
        exposedPort: 7000
        targetPort: 7000
        additionalPortMappings: [
          {
            external: true
            exposedPort: 7001
            targetPort: 7001
          }
          {
            external: true
            exposedPort: 7006
            targetPort: 7006
          }
        ]
      }
      activeRevisionsMode: 'Single'
    }
    template: {
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      containers: [
        {
          name: 'mock-register'
          image: 'consumerdataright/mock-register:latest'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Release'
            }
            {
              name: 'ConnectionStrings__Register_DB'
              value: registerDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__Register_Logging_DB'
              value: registerDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__Register_RequestResponse_Logging_DB'
              value: registerDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__Register_DBO'
              value: registerDatabaseConnectionString
            }

            {
              name: 'PublicHostName'
              value: 'https://${registerHostName}:7000'
            }
            {
              name: 'SecureHostName'
              value: 'https://${registerHostName}:7001'
            }
            {
              name: 'OidcMetadataAddress'
              value: 'https://localhost:7002/idp/.well-known/openid-configuration'
            }
            {
              name: 'IdentityServerTokenUri'
              value: 'https://${registerHostName}:7001/idp/connect/token'
            }
          ]
        }
      ]
    }
  }
}

var dataHolderContainerAppName = 'mock-data-holder'
var dataHolderContainerHostName = '${dataHolderContainerAppName}.${containerAppEnvironment.properties.defaultDomain}'

resource dataHolderContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: dataHolderContainerAppName
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${applicationUami.id}': {}
    }
  }
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        transport: 'tcp'
        exposedPort: 8000
        targetPort: 8000
        additionalPortMappings: [
          {
            external: true
            exposedPort: 8001
            targetPort: 8001
          }
          {
            external: true
            exposedPort: 8002
            targetPort: 8002
          }
          {
            external: true
            exposedPort: 8005
            targetPort: 8005
          }
          {
            external: true
            exposedPort: 8081
            targetPort: 8081
          }
          {
            external: true
            exposedPort: 3000
            targetPort: 3000
          }
        ]
      }
      activeRevisionsMode: 'Single'
    }
    template: {
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      containers: [
        {
          name: 'mock-data-holder'
          image: 'consumerdataright/mock-data-holder:latest'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Release'
            }
            {
              name: 'ConnectionStrings__DataHolder_DB'
              value: consumerRightsDataHolderDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__DataHolder_Logging_DB'
              value: consumerRightsDataHolderDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__DataHolder_RequestResponse_Logging_DB'
              value: consumerRightsDataHolderDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__DataHolder_Migrations_DB'
              value: consumerRightsDataHolderDatabaseConnectionString
            }


            {
              name: 'ConnectionStrings__CDR_Auth_Server_RW'
              value: consumerRightsDataHolderAuthServerDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__CDR_Auth_Server_Migrations_DBO'
              value: consumerRightsDataHolderAuthServerDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__CDR_Auth_Server_Logging_DBO'
              value: consumerRightsDataHolderAuthServerDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__CDR_Auth_Server_RequestResponse_Logging_DBO'
              value: consumerRightsDataHolderAuthServerDatabaseConnectionString
            }

            {
              name: 'CdrAuthServer__Issuer'
              value: 'https://${dataHolderContainerHostName}:8001'
            }
            {
              name: 'CdrAuthServer__BaseUri'
              value: 'https://${dataHolderContainerHostName}:8001'
            }
            {
              name: 'CdrAuthServer__SecureBaseUri'
              value: 'https://${dataHolderContainerHostName}:8082'
            }
            {
              name: 'CdrAuthServer__CdrRegister__SsaJwksUri'
              value: 'https://${registerHostName}:7000/cdr-register/v1/jwks'
            }
            {
              name: 'CdrAuthServer__CdrRegister__GetDataRecipientsEndpoint'
              value: 'https://${registerHostName}:7000/cdr-register/v1/all/data-recipients'
            }
            {
              name: 'CdrAuthServer__AuthUiBaseUri'
              value: 'http://${dataHolderContainerHostName}:3000'
            }
            {
              name: 'AccessTokenIntrospectionEndpoint'
              value: 'https://${dataHolderContainerHostName}:8081/connect/introspect-internal'
            }
            {
              name: 'IdentityServerIssuerUri'
              value: 'https://${dataHolderContainerHostName}:8001'
            }
            {
              name: 'IdentityServerUrl'
              value: 'https://${dataHolderContainerHostName}:8001'
            }
            {
              name: 'ResourceBaseUri'
              value: 'https://${dataHolderContainerHostName}:8002'
            }
            {
              name: 'AdminBaseUri'
              value: 'https://${dataHolderContainerHostName}:8002'
            }
            {
              name: 'DataHolderJwksUri'
              value: 'https://${dataHolderContainerHostName}:8001/.well-known/openid-configuration/jwks'
            }
            {
              name: 'DataHolderIssuer'
              value: 'https://${dataHolderContainerHostName}:8001'
            }
            {
              name: 'RegisterJwksUri'
              value: 'https://${registerHostName}:7000/cdr-register/v1/jwks'
            }

            {
              name: 'AdminBaseUri'
              value: 'https://${dataHolderContainerHostName}:8002'
            }
            {
              name: 'DataHolderJwksUri'
              value: 'https://${dataHolderContainerHostName}:8001/.well-known/openid-configuration/jwks'
            }
            {
              name: 'DataHolderIssuer'
              value: 'https://${dataHolderContainerHostName}:8001'
            }
            {
              name: 'RegisterJwksUri'
              value: 'https://${registerHostName}:7000/cdr-register/v1/jwks'
            }
            {
              name: 'Domain'
              value: '${dataHolderContainerHostName}:8000'
            }
          ]
        }
      ]
    }
  }
}

var dataRecipientAppName = 'mock-data-recipient'
var dataRecipientHostName = '${dataRecipientAppName}.${containerAppEnvironment.properties.defaultDomain}'

resource dataRecipientApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: dataRecipientAppName
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${applicationUami.id}': {}
    }
  }
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        transport: 'tcp'
        exposedPort: 9001
        targetPort: 9001
      }
      activeRevisionsMode: 'Single'
    }
    template: {
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      containers: [
        {
          name: 'mock-data-recipient'
          image: 'consumerdataright/mock-data-recipient:latest'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Release'
            }
            {
              name: 'ConnectionStrings__DataRecipient_DB'
              value: consumerRightsDataRecipientDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__DataRecipient_Logging_DB'
              value: consumerRightsDataRecipientDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__DataRecipient_Migrations_DBO'
              value: consumerRightsDataRecipientDatabaseConnectionString
            }
            {
              name: 'ConnectionStrings__DataRecipient_RequestResponse_Logging_DB'
              value: consumerRightsDataRecipientDatabaseConnectionString
            }

            {
              name: 'MockDataRecipient__Register__tlsBaseUri'
              value: 'https://${registerHostName}:7000'
            }
            {
              name: 'MockDataRecipient__Register__mtlsBaseUri'
              value: 'https://${registerHostName}:7001'
            }
            {
              name: 'MockDataRecipient__Register__oidcDiscoveryUri'
              value: 'https://${registerHostName}:7000/idp/.well-known/openid-configuration'
            }
            {
              name: 'MockDataRecipient__SoftwareProduct__jwksUri'
              value: 'https://${dataRecipientHostName}:9001/jwks'
            }
            {
              name: 'MockDataRecipient__SoftwareProduct__redirectUris'
              value: 'https://${dataRecipientHostName}:9001/consent/callback'
            }
            {
              name: 'MockDataRecipient__SoftwareProduct__recipientBaseUri'
              value: 'https://${dataRecipientHostName}:9001'
            }
          ]
        }
      ]
    }
  }
}

output registerHostName string = 'https://${registerHostName}:7001'
output dataRecipientHostName string = 'https://${registerHostName}:9001'
output applicationUamiName string = applicationUami.name
output sqlServerName string = sqlServer.name
output dataRecipientDbName string = sqlServer::dataRecipientDb.name
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output dataHolderHostName string = 'https://${dataHolderContainerHostName}:8001'

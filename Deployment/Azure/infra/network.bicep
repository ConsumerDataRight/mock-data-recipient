targetScope = 'resourceGroup'

param resourceToken string
param location string = resourceGroup().location
param tags object = {}
param vnetAbbreviation string

var networkAddressPrefix = '10.0.0.0/16'

resource vnet 'Microsoft.Network/virtualNetworks@2024-01-01' = {
  name: '${vnetAbbreviation}${resourceToken}'
  tags: tags
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        networkAddressPrefix
      ]
    }
    subnets: [
      {
        name: 'privateendpoints'
        properties: {
          addressPrefix: cidrSubnet(networkAddressPrefix, 24, 0)
        }
      }
      {
        name: 'containerapps'
        properties: {
          addressPrefix: cidrSubnet(networkAddressPrefix, 23, 1)
        }
      }
    ]
  }
}

output vnetId string = vnet.id
output vnetName string = vnet.name
output privateEndpointsSubnetId string = vnet.properties.subnets[0].id
output containerAppSubnetId string = vnet.properties.subnets[1].id

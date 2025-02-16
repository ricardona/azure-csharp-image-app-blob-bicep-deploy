@description('Location for all resources.')
param location string = resourceGroup().location

param storageAccountType string = 'Standard_LRS'

@description('Name for the container group')
param name string = 'photogalleryaci'

@description('Container image to deploy.')
param image string = 'ghcr.io/ricardona/photogallery:latest'

@description('Port to open on the container and the public IP address.')
param port int = 80

@description('DNS Name')
param dnsNameLabel string = 'dnsName'

@description('The number of CPU cores to allocate to the container.')
param cpuCores int = 1

@description('The amount of memory to allocate to the container in gigabytes.')
param memoryInGb int = 1

@description('The behavior of Azure runtime if container has stopped.')
@allowed([
  'Always'
  'Never'
  'OnFailure'
])
param restartPolicy string = 'OnFailure'

var storageAccountName = '${name}stoaci'
resource photoGalleryAciStorageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageAccountType
  }
  kind: 'Storage'
  properties: {
    allowBlobPublicAccess: true
    publicNetworkAccess: 'Enabled'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2022-05-01' = {
  parent: photoGalleryAciStorageAccount
  name: 'default'
}

resource blobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-05-01' = {
  parent: blobService
  name: 'imagecontainer'
  properties: {
    publicAccess: 'Container'
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${name}-workspace'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${name}-appinsights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
  }
}

@description('The storage account connection string')
#disable-next-line use-resource-symbol-reference
var blobStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${photoGalleryAciStorageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(photoGalleryAciStorageAccount.id, photoGalleryAciStorageAccount.apiVersion).keys[0].value}'

resource containerGroup 'Microsoft.ContainerInstance/containerGroups@2021-10-01' = {
  name: name
  location: location
  properties: {
    containers: [
      {
        name: name
        properties: {
          image: image
          ports: [
            {
              port: port
              protocol: 'TCP'
            }
          ]
          resources: {
            requests: {
              cpu: cpuCores
              memoryInGB: memoryInGb
            }
          }
          environmentVariables: [
            {
              name: 'StorageConnectionString'
              secureValue: blobStorageConnectionString
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              secureValue: applicationInsights.properties.ConnectionString
            }
          ]
        }
      }
    ]
    osType: 'Linux'
    restartPolicy: restartPolicy
    ipAddress: {
      type: 'Public'
      ports: [
        {
          port: port
          protocol: 'TCP'
        }
      ]
      dnsNameLabel: dnsNameLabel
    }
  }
}

output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.properties.customerId
output containerIPv4Address string = containerGroup.properties.ipAddress.ip

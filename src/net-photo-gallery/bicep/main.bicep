@description('Location for all resources.')
param location string = resourceGroup().location

var storageAccountName = '${uniqueString(resourceGroup().id)}aci'
param storageAccountType string = 'Standard_LRS'

resource photoGalleryAciStorageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageAccountType
  }
  kind: 'Storage'
}

@description('Name for the container group')
param name string = 'photo-gallery-aci'

@description('Container image to deploy.')
param image string = 'docker.io/zimelemon/photogallery'

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

var blobStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${photoGalleryAciStorageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${photoGalleryAciStorageAccount.listKeys().keys[0].value}'

// Add Log Analytics Workspace
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

resource photoGalleryAci 'Microsoft.ContainerInstance/containerGroups@2021-10-01' = {
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
          environmentVariables:[
            {
              name: 'StorageConnectionString'
              secureValue: blobStorageConnectionString
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
    diagnostics: {
      logAnalytics: {
        workspaceId: logAnalyticsWorkspace.properties.customerId
        workspaceKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

// Add Log Analytics outputs
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.properties.customerId
output containerIPv4Address string = photoGalleryAci.properties.ipAddress.ip

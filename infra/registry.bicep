// Step 1 of the deploy (see deploy.ps1): the container registry on its own, so images can be built
// into it before main.bicep creates Container Apps that reference those images.

@description('Short lowercase prefix used in resource names.')
param prefix string = 'beam'

param location string = resourceGroup().location

// Registry names are global, alphanumeric only.
var registryName = '${replace(prefix, '-', '')}acr${uniqueString(resourceGroup().id)}'

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    // Container Apps pull with the admin credential (see main.bicep). A managed identity with
    // AcrPull is the stricter option, but role assignments need Owner rights and can take minutes
    // to propagate, which makes the first deploy flaky -- not worth it for a single-team setup.
    adminUserEnabled: true
  }
}

output registryName string = registry.name
output loginServer string = registry.properties.loginServer

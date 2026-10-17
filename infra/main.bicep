// Step 2 of the deploy (see deploy.ps1): Container Apps environment, Postgres, and the api/web apps.
// Files live in Supabase Storage (S3-compatible), so there is no storage account here.

@description('Short lowercase prefix used in resource names.')
param prefix string = 'beam'

param location string = resourceGroup().location

@description('Region for Postgres. Burstable SKUs are not offered everywhere; override if the deploy fails with a SKU/region error.')
param postgresLocation string = location

@description('Registry created by registry.bicep.')
param registryName string

@description('Image tag pushed by deploy.ps1 for both beam-api and beam-web.')
param imageTag string

// ---- Postgres ----
param postgresAdminUser string = 'beamadmin'

@secure()
@description('Avoid ; and \' -- the password is embedded in an Npgsql connection string.')
param postgresAdminPassword string

// ---- Supabase Storage (S3 protocol) ----
@description('e.g. https://<project-ref>.supabase.co/storage/v1/s3')
param storageServiceUrl string

@description('Supabase project region, e.g. eu-central-1 (used for S3 request signing).')
param storageRegion string

@secure()
param storageAccessKey string

@secure()
param storageSecretKey string

param storageBucketName string = 'beam-assets'

// ---- Optional integrations (left out of the app entirely when empty) ----
@secure()
param anthropicApiKey string = ''

param twitchClientId string = ''

@secure()
param twitchClientSecret string = ''

param youTubeClientId string = ''

@secure()
param youTubeClientSecret string = ''

@description('CIDRs whose X-Forwarded-* headers the API trusts. Container Apps ingress reaches the app from private address space; public clients never do, so trusting the private ranges cannot be spoofed from outside.')
param trustedProxyCidrs array = [
  '10.0.0.0/8'
  '100.64.0.0/10'
  '172.16.0.0/12'
  '192.168.0.0/16'
]

@description('0 lets the web app scale to zero when idle (cheaper, first page load is slower). The API is always pinned to exactly 1.')
@minValue(0)
param webMinReplicas int = 0

var suffix = uniqueString(resourceGroup().id)
var apiAppName = '${prefix}-api'
var webAppName = '${prefix}-web'

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: registryName
}

resource logs 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${prefix}-logs-${suffix}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${prefix}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: '${prefix}-pg-${suffix}'
  location: postgresLocation
  sku: {
    // Burstable B1ms is the size covered by the Azure free account's 12-month Postgres allowance.
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: postgresAdminUser
    administratorLoginPassword: postgresAdminPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

resource postgresDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: 'beam'
}

// Container Apps on the Consumption plan have no fixed outbound IPs, so allow Azure-hosted
// callers (the special 0.0.0.0 rule). The server still requires TLS and the admin password.
resource postgresAllowAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgres
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

var connectionString = 'Host=${postgres.properties.fullyQualifiedDomainName};Port=5432;Database=${postgresDb.name};Username=${postgresAdminUser};Password=${postgresAdminPassword};Ssl Mode=Require'

var registryPassword = registry.listCredentials().passwords[0].value

var baseApiSecrets = [
  { name: 'registry-password', value: registryPassword }
  { name: 'db-connection', value: connectionString }
  { name: 'storage-access-key', value: storageAccessKey }
  { name: 'storage-secret-key', value: storageSecretKey }
]

var optionalApiSecrets = concat(
  empty(anthropicApiKey) ? [] : [{ name: 'anthropic-api-key', value: anthropicApiKey }],
  empty(twitchClientSecret) ? [] : [{ name: 'twitch-client-secret', value: twitchClientSecret }],
  empty(youTubeClientSecret) ? [] : [{ name: 'youtube-client-secret', value: youTubeClientSecret }]
)

var baseApiEnv = [
  { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
  { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
  { name: 'ConnectionStrings__BeamDb', secretRef: 'db-connection' }
  { name: 'Storage__ServiceUrl', value: storageServiceUrl }
  { name: 'Storage__Region', value: storageRegion }
  { name: 'Storage__AccessKey', secretRef: 'storage-access-key' }
  { name: 'Storage__SecretKey', secretRef: 'storage-secret-key' }
  { name: 'Storage__BucketName', value: storageBucketName }
  // Single replica, so applying migrations on startup is safe (see Program.cs).
  { name: 'Startup__ApplyMigrations', value: 'true' }
]

var optionalApiEnv = concat(
  empty(anthropicApiKey) ? [] : [{ name: 'AiGeneration__AnthropicApiKey', secretRef: 'anthropic-api-key' }],
  empty(twitchClientId) ? [] : [{ name: 'Integrations__Twitch__ClientId', value: twitchClientId }],
  empty(twitchClientSecret) ? [] : [{ name: 'Integrations__Twitch__ClientSecret', secretRef: 'twitch-client-secret' }],
  empty(youTubeClientId) ? [] : [{ name: 'Integrations__YouTube__ClientId', value: youTubeClientId }],
  empty(youTubeClientSecret) ? [] : [{ name: 'Integrations__YouTube__ClientSecret', secretRef: 'youtube-client-secret' }]
)

var proxyEnv = [for (cidr, i) in trustedProxyCidrs: {
  name: 'ForwardedHeaders__KnownNetworks__${i}'
  value: cidr
}]

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        // 'auto' negotiates HTTP/1.1 upgrades, which SignalR's WebSocket transport needs.
        transport: 'auto'
        // The web app's /api/* proxy calls http://beam-api inside the environment (private hop,
        // so X-Forwarded-For still carries the real client IP). With allowInsecure=false that
        // call would be redirected to HTTPS. Browsers are only ever given the https:// URL, and
        // the auth cookie is Secure-only, so it is never sent over a plain-HTTP request anyway.
        allowInsecure: true
      }
      registries: [
        {
          server: registry.properties.loginServer
          username: registry.listCredentials().username
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: concat(baseApiSecrets, optionalApiSecrets)
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${registry.properties.loginServer}/beam-api:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: concat(baseApiEnv, optionalApiEnv, proxyEnv)
          probes: [
            {
              type: 'Readiness'
              httpGet: {
                path: '/api/health/ready'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 15
            }
          ]
        }
      ]
      scale: {
        // Exactly one: live overrides (InMemoryLiveStateStore) and SignalR groups are per-process,
        // so a second replica would split viewers across disconnected state. Never scale to zero
        // either -- OBS overlays hold a live connection and can't tolerate a cold start.
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

resource web 'Microsoft.App/containerApps@2024-03-01' = {
  name: webAppName
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 3000
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: registry.properties.loginServer
          username: registry.listCredentials().username
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: [
        { name: 'registry-password', value: registryPassword }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: '${registry.properties.loginServer}/beam-web:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            // Read at runtime by /config and handed to overlay/control pages, which call the API
            // directly from the browser. (The /api/* proxy target is baked in at image build time
            // instead -- see deploy.ps1.)
            { name: 'BEAM_API_PUBLIC_URL', value: 'https://${api.properties.configuration.ingress.fqdn}' }
            { name: 'HOSTNAME', value: '0.0.0.0' }
            { name: 'PORT', value: '3000' }
          ]
        }
      ]
      scale: {
        minReplicas: webMinReplicas
        maxReplicas: 2
      }
    }
  }
}

output webUrl string = 'https://${web.properties.configuration.ingress.fqdn}'
output apiUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
output postgresHost string = postgres.properties.fullyQualifiedDomainName

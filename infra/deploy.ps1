<#
.SYNOPSIS
    Builds Beam's api/web images in Azure Container Registry and deploys them to Azure Container Apps.

.DESCRIPTION
    Re-runnable: the first run creates everything, later runs build new images and roll out a new
    revision. Needs the Azure CLI (az login first). Docker is NOT needed locally -- images are built
    in the cloud with `az acr build`.

    1. registry.bicep  -> container registry
    2. az acr build    -> beam-api and beam-web images, tagged <git sha>-<timestamp>
    3. main.bicep      -> Container Apps environment, Postgres, api + web apps

.PARAMETER EnvFile
    Settings file (see azure.env.example). Defaults to infra/azure.env.
#>
param(
    [string]$EnvFile = (Join-Path $PSScriptRoot 'azure.env')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent

function Assert-AzSucceeded([string]$what) {
    # az is a native exe: a failure doesn't throw on its own, so check the exit code explicitly.
    if ($LASTEXITCODE -ne 0) { throw "$what failed (az exit code $LASTEXITCODE)." }
}

function Invoke-Az {
    $output = & az @args
    Assert-AzSucceeded "az $($args[0]) $($args[1])"
    return $output
}

if (-not (Test-Path $EnvFile)) {
    throw "Settings file not found: $EnvFile. Copy infra/azure.env.example to infra/azure.env and fill it in."
}

$settings = @{}
foreach ($line in Get-Content $EnvFile) {
    $trimmed = $line.Trim()
    if ($trimmed -eq '' -or $trimmed.StartsWith('#')) { continue }
    $eq = $trimmed.IndexOf('=')
    if ($eq -lt 1) { continue }
    $settings[$trimmed.Substring(0, $eq).Trim()] = $trimmed.Substring($eq + 1).Trim()
}

function Get-Setting([string]$name, [string]$default = '') {
    if ($settings.ContainsKey($name) -and $settings[$name] -ne '') { return $settings[$name] }
    return $default
}

$required = 'AZURE_RESOURCE_GROUP', 'AZURE_LOCATION', 'POSTGRES_ADMIN_PASSWORD', 'STORAGE_SERVICE_URL',
    'STORAGE_REGION', 'STORAGE_ACCESS_KEY', 'STORAGE_SECRET_KEY', 'SIXLABORS_LICENSE_KEY'
$missing = $required | Where-Object { (Get-Setting $_) -eq '' -or (Get-Setting $_) -like '<*' }
if ($missing) { throw "Missing values in ${EnvFile}: $($missing -join ', ')" }

$resourceGroup = Get-Setting 'AZURE_RESOURCE_GROUP'
$location = Get-Setting 'AZURE_LOCATION'
$prefix = Get-Setting 'PREFIX' 'beam'

Invoke-Az account show --query name -o tsv | ForEach-Object { Write-Host "Subscription: $_" }

Write-Host "`n==> Resource group $resourceGroup ($location)"
Invoke-Az group create --name $resourceGroup --location $location --output none

Write-Host "`n==> Container registry"
$registryOutputs = Invoke-Az deployment group create --resource-group $resourceGroup `
    --template-file (Join-Path $PSScriptRoot 'registry.bicep') `
    --parameters prefix=$prefix location=$location `
    --query properties.outputs --output json | Out-String | ConvertFrom-Json
$registryName = $registryOutputs.registryName.value

$sha = (& git -C $repoRoot rev-parse --short HEAD 2>$null)
if (-not $sha) { $sha = 'local' }
$imageTag = "$sha-$(Get-Date -Format 'yyyyMMddHHmmss')"

Write-Host "`n==> Building beam-api:$imageTag in $registryName"
# Called directly (not via Invoke-Az) so the build log streams instead of arriving at the end.
& az acr build --registry $registryName --image "beam-api:$imageTag" `
    --file (Join-Path $repoRoot 'src/Beam.Api/Dockerfile') `
    --secret-build-arg "SIXLABORS_LICENSE_KEY=$(Get-Setting 'SIXLABORS_LICENSE_KEY')" `
    $repoRoot
Assert-AzSucceeded 'beam-api image build'

# next.config.ts freezes the /api/* rewrite target at build time. Inside a Container Apps
# environment every app is reachable by its name, so the address is known before deploying.
Write-Host "`n==> Building beam-web:$imageTag in $registryName"
& az acr build --registry $registryName --image "beam-web:$imageTag" `
    --build-arg "BEAM_API_INTERNAL_URL=http://$prefix-api" `
    (Join-Path $repoRoot 'web')
Assert-AzSucceeded 'beam-web image build'

# Pass parameters through a temp file rather than the command line, so secrets don't show up in
# the process list and PowerShell doesn't mangle special characters in them.
$parameters = @{
    prefix                = @{ value = $prefix }
    location              = @{ value = $location }
    postgresLocation      = @{ value = (Get-Setting 'POSTGRES_LOCATION' $location) }
    registryName          = @{ value = $registryName }
    imageTag              = @{ value = $imageTag }
    postgresAdminPassword = @{ value = (Get-Setting 'POSTGRES_ADMIN_PASSWORD') }
    storageServiceUrl     = @{ value = (Get-Setting 'STORAGE_SERVICE_URL') }
    storageRegion         = @{ value = (Get-Setting 'STORAGE_REGION') }
    storageAccessKey      = @{ value = (Get-Setting 'STORAGE_ACCESS_KEY') }
    storageSecretKey      = @{ value = (Get-Setting 'STORAGE_SECRET_KEY') }
    storageBucketName     = @{ value = (Get-Setting 'STORAGE_BUCKET_NAME' 'beam-assets') }
    anthropicApiKey       = @{ value = (Get-Setting 'ANTHROPIC_API_KEY') }
    twitchClientId        = @{ value = (Get-Setting 'TWITCH_CLIENT_ID') }
    twitchClientSecret    = @{ value = (Get-Setting 'TWITCH_CLIENT_SECRET') }
    youTubeClientId       = @{ value = (Get-Setting 'YOUTUBE_CLIENT_ID') }
    youTubeClientSecret   = @{ value = (Get-Setting 'YOUTUBE_CLIENT_SECRET') }
    webMinReplicas        = @{ value = [int](Get-Setting 'WEB_MIN_REPLICAS' '0') }
}
$parametersFile = Join-Path ([IO.Path]::GetTempPath()) "beam-deploy-$([guid]::NewGuid()).json"
@{
    '$schema'      = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
    contentVersion = '1.0.0.0'
    parameters     = $parameters
} | ConvertTo-Json -Depth 5 | ForEach-Object {
    # UTF-8 without a BOM (Windows PowerShell's -Encoding utf8 would add one).
    [IO.File]::WriteAllText($parametersFile, $_, (New-Object System.Text.UTF8Encoding $false))
}

try {
    Write-Host "`n==> Deploying Container Apps + Postgres (first run takes ~10 minutes)"
    $outputs = Invoke-Az deployment group create --resource-group $resourceGroup `
        --template-file (Join-Path $PSScriptRoot 'main.bicep') `
        --parameters "@$parametersFile" `
        --query properties.outputs --output json | Out-String | ConvertFrom-Json
}
finally {
    Remove-Item $parametersFile -Force -ErrorAction SilentlyContinue
}

Write-Host "`nDeployed $imageTag"
Write-Host "  Web: $($outputs.webUrl.value)"
Write-Host "  API: $($outputs.apiUrl.value)  (health: $($outputs.apiUrl.value)/api/health/ready)"

# deploy.ps1 — JJGNet Broadcasting infrastructure deployment script
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Bicep CLI available (az bicep install)
#   - Sufficient permissions on the target subscription
#
# Usage:
#   .\infra\deploy.ps1 -Environment prod -ResourceGroup rg-jjgnet-prod -SqlAdminPassword "YourP@ssw0rd"
#   .\infra\deploy.ps1 -Environment dev  -ResourceGroup rg-jjgnet-dev  -SqlAdminPassword "YourP@ssw0rd"

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment,

    [Parameter(Mandatory)]
    [string]$ResourceGroup,

    [Parameter(Mandatory)]
    [securestring]$SqlAdminPassword,

    [string]$Location = 'westus2',

    [string]$AdminPrincipalObjectId = ''
)

$ErrorActionPreference = 'Stop'

# Resolve paths relative to repo root regardless of where the script is called from
$repoRoot   = Split-Path -Parent $PSScriptRoot
$template   = Join-Path $repoRoot "infra\main.bicep"
$paramsFile = Join-Path $repoRoot "infra\parameters\$Environment.bicepparam"

Write-Host "=== JJGNet Broadcasting — Bicep deployment ===" -ForegroundColor Cyan
Write-Host "  Environment    : $Environment"
Write-Host "  Resource Group : $ResourceGroup"
Write-Host "  Location       : $Location"
Write-Host "  Template       : $template"
Write-Host "  Parameters     : $paramsFile"
Write-Host ""

# Ensure the resource group exists
$rgExists = az group show --name $ResourceGroup --query name -o tsv 2>$null
if (-not $rgExists) {
    Write-Host "Creating resource group '$ResourceGroup' in '$Location'..." -ForegroundColor Yellow
    az group create --name $ResourceGroup --location $Location | Out-Null
}

# Convert SecureString to plain text for the CLI (passed via --parameters, not written to disk)
$sqlPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
)

# Build CLI arguments
$deployArgs = @(
    'deployment', 'group', 'create',
    '--resource-group', $ResourceGroup,
    '--template-file', $template,
    '--parameters', $paramsFile,
    '--parameters', "sqlAdminPassword=$sqlPasswordPlain",
    '--name', "jjgnet-infra-$(Get-Date -Format 'yyyyMMddHHmmss')"
)

if ($AdminPrincipalObjectId) {
    $deployArgs += '--parameters'
    $deployArgs += "adminPrincipalObjectId=$AdminPrincipalObjectId"
}

Write-Host "Starting deployment..." -ForegroundColor Green
az @deployArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "=== Deployment complete ===" -ForegroundColor Green

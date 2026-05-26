$ErrorActionPreference = "Stop"

$certDir = Join-Path $PSScriptRoot "..\.cert"
$certDir = [System.IO.Path]::GetFullPath($certDir)
New-Item -ItemType Directory -Force -Path $certDir | Out-Null

$certPath = Join-Path $certDir "dev-cert.pem"
$keyPathPem = Join-Path $certDir "dev-key.pem"
$keyPathKey = Join-Path $certDir "dev-cert.key"

Write-Host "Exporting HTTPS development certificate to $certDir ..."
dotnet dev-certs https --export-path $certPath --format Pem --no-password | Out-Null

$resolvedKeyPath = $null
if (Test-Path $keyPathPem) {
  $resolvedKeyPath = $keyPathPem
} elseif (Test-Path $keyPathKey) {
  $resolvedKeyPath = $keyPathKey
}

if (!(Test-Path $certPath) -or $null -eq $resolvedKeyPath) {
  throw "Cannot find exported cert/key files: $certPath / ($keyPathPem or $keyPathKey)"
}

Write-Host "Done. Files created:"
Write-Host "- $certPath"
Write-Host "- $resolvedKeyPath"
Write-Host "Now run: npm run dev:https"
Write-Host "If phone still blocks camera, trust cert manually by running: dotnet dev-certs https --trust"

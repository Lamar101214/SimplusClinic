param([switch]$Publish)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
 dotnet restore SimplusClinic.slnx
 if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
 dotnet build SimplusClinic.slnx -c Release --no-restore
 if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
 dotnet run --project tests/SimplusClinic.Tests -c Release --no-build
 if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
 if ($Publish) {
  dotnet publish src/SimplusClinic.WinForms -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish
  if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
 }
} finally { Pop-Location }

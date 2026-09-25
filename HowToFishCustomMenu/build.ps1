param([Parameter(Mandatory=$true)][string]$GameDir)
dotnet build "$PSScriptRoot/HowToFishCustomMenu.csproj" -c Release "-p:GameDir=$GameDir"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Compiled DLL: $PSScriptRoot/bin/Release/netstandard2.1/HowToFishCustomMenu.dll"

<#
.SYNOPSIS
    Builds the shipped Jumbee.Console NuGet packages.

.DESCRIPTION
    Packs the three shipped libraries into .nupkg files:
      * Jumbee.Console          - self-contained core (bundles the ext/ forks + Styles)
      * Jumbee.Console.Documents - Markdown/AsciiDoc/Mermaid viewers (add-on)
      * Jumbee.Console.Snapshot  - headless snapshot testing (add-on)
    The other src/ projects are not packable and are skipped.

.PARAMETER Configuration
    Build configuration to pack. Default: Release.

.PARAMETER Output
    Directory to write the .nupkg files to. Default: <repo>/artifacts.

.PARAMETER PackageVersion
    Optional version override (e.g. "0.1.0-preview.1" for a prerelease). When omitted, the version comes from
    ProjectAssemblyVersion in src/Directory.Build.props.

.EXAMPLE
    powershell -File pack.ps1
    powershell -File pack.ps1 -PackageVersion 0.1.0-preview.1
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $Output,
    [string] $PackageVersion
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
if (-not $Output) { $Output = Join-Path $repoRoot 'artifacts' }

$projects = @(
    'src/Jumbee.Console/Jumbee.Console.csproj',
    'src/Jumbee.Console.Documents/Jumbee.Console.Documents.csproj',
    'src/Jumbee.Console.Snapshot/Jumbee.Console.Snapshot.csproj'
)

# Start from a clean output folder so stale packages don't linger.
if (Test-Path $Output) { Remove-Item -Path (Join-Path $Output '*.nupkg') -Force -ErrorAction SilentlyContinue }
else { New-Item -ItemType Directory -Path $Output | Out-Null }

$packArgs = @('-c', $Configuration, '-o', $Output, '--nologo')
if ($PackageVersion) { $packArgs += "-p:PackageVersion=$PackageVersion" }

foreach ($proj in $projects) {
    $full = Join-Path $repoRoot $proj
    Write-Host "Packing $proj ..." -ForegroundColor Cyan
    & dotnet pack $full @packArgs
    if ($LASTEXITCODE -ne 0) { throw "pack failed for $proj (exit $LASTEXITCODE)." }
}

Write-Host ''
Write-Host 'Produced packages:' -ForegroundColor Green
Get-ChildItem -Path $Output -Filter *.nupkg | ForEach-Object { Write-Host ("  {0}  ({1:N0} KB)" -f $_.Name, ($_.Length / 1KB)) }
Write-Host ''
Write-Host ("Output: {0}" -f $Output) -ForegroundColor Green
Write-Host 'To publish (do this manually): dotnet nuget push "<Output>\*.nupkg" --source <feed> --api-key <key>' -ForegroundColor DarkGray

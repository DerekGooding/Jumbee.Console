<#
.SYNOPSIS
    NativeAOT variant of the Jumbee.Console NuGet package smoke test.

.DESCRIPTION
    Publishes the smoke-test project (Program.cs) as a NativeAOT native single-file binary, restoring
    Jumbee.Console and its bundled ext/ fork assemblies purely from nuget.org, then runs the produced
    binary headlessly and asserts exit code 0. This proves the *published* package is AOT- and
    trim-clean when consumed from the feed - a stronger guarantee than the source build gives, since the
    package could ship without the trim metadata the local ProjectReference build carries implicitly.

    Unlike the fast `dotnet run` smoke test, this REQUIRES a native toolchain (on Windows: VS Build Tools
    with the Desktop C++ workload / linker). It is therefore an opt-in step, not part of the default run.

.PARAMETER Version
    The published package version to test. Omit to test the LATEST stable release on nuget.org (the floating
    '*' default in the .csproj); pass a value to pin a specific release.

.PARAMETER Rid
    The runtime identifier to publish for. Defaults to win-x64.

.EXAMPLE
    ./publish-aot.ps1
    ./publish-aot.ps1 -Version 0.1.2
    ./publish-aot.ps1 -Version 0.1.2 -Rid linux-x64
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$Rid = "win-x64"
)

$ErrorActionPreference = "Stop"
$projectDir = $PSScriptRoot
$project = Join-Path $projectDir "Jumbee.Console.Tests.NuGet.csproj"
$publishDir = Join-Path $projectDir "bin\aot\$Rid"

Write-Host "Jumbee.Console NuGet package AOT smoke test" -ForegroundColor Cyan
Write-Host "==========================================="
Write-Host "  RID:     $Rid"
Write-Host "  Version: $(if ($Version) { $Version } else { '* (latest stable on nuget.org)' })"
Write-Host "  Output:  $publishDir"
Write-Host ""

# The ILC native-link step shells out to vswhere.exe to locate the MSVC linker, but vswhere is not on PATH in
# a plain shell (only inside a VS Developer prompt). Make it resolvable so this runs stand-alone on Windows.
if ($Rid -like "win-*" -and -not (Get-Command vswhere.exe -ErrorAction SilentlyContinue)) {
    $installerDir = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer"
    if (Test-Path (Join-Path $installerDir "vswhere.exe")) {
        $env:PATH = "$installerDir;$env:PATH"
    } else {
        Write-Host "WARNING: vswhere.exe not found; the AOT native-link step may fail. Run from a" -ForegroundColor Yellow
        Write-Host "         'Developer PowerShell for VS' or install the VS C++ build tools." -ForegroundColor Yellow
    }
}

$publishArgs = @(
    "publish", $project,
    "-c", "Release",
    "-r", $Rid,
    "-p:PublishAot=true",
    "-o", $publishDir
)
if ($Version) { $publishArgs += "-p:JumbeeVersion=$Version" }

Write-Host "> dotnet $($publishArgs -join ' ')" -ForegroundColor DarkGray
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "AOT publish FAILED (dotnet exit $LASTEXITCODE)." -ForegroundColor Red
    exit 1
}

# NativeAOT emits a single native binary named after the assembly (.exe on Windows, no extension elsewhere).
$exeName = if ($Rid -like "win-*") { "Jumbee.Console.Tests.NuGet.exe" } else { "Jumbee.Console.Tests.NuGet" }
$exe = Join-Path $publishDir $exeName
if (-not (Test-Path $exe)) {
    Write-Host "Published binary not found at $exe" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Running AOT binary: $exe" -ForegroundColor Cyan
$runArgs = @()
if ($Version) { $runArgs += @("--version", $Version) }
& $exe @runArgs
$code = $LASTEXITCODE

Write-Host ""
if ($code -eq 0) {
    Write-Host "AOT SMOKE TEST PASSED - the published package publishes and runs under NativeAOT." -ForegroundColor Green
} else {
    Write-Host "AOT SMOKE TEST FAILED - $code check(s) failed in the native binary." -ForegroundColor Red
}
exit $code

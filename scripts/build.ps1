param(
    [ValidateSet("Debug", "Release", "Portable", "Centennial", "uiAccessRelease")]
    [string]$Configuration = "Debug",
    [string]$Platform = "Any CPU",
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot "GestureSign.sln"
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

if (-not (Test-Path $vswhere)) {
    throw "vswhere.exe was not found. Install Visual Studio 2022 or Build Tools 2022."
}

$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1

if (-not $msbuild) {
    throw "MSBuild.exe was not found. Install the MSBuild component from Visual Studio."
}

Push-Location $repoRoot

try {
    if (-not $SkipRestore) {
        & $msbuild $solution "/t:Restore" "/p:RestorePackagesConfig=true" "/v:m"
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    & $msbuild $solution "/m:1" "/nr:false" "/p:UseSharedCompilation=false" "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/v:m"
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}

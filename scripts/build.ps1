param(
    [ValidateSet("Debug", "Release", "Portable", "Centennial", "uiAccessRelease")]
    [string]$Configuration = "Debug",
    [string]$Platform = "Any CPU",
    [switch]$SkipRestore,
    [switch]$PreferNativeArm64
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
    if ($PreferNativeArm64 -and $Platform -notin @("Any CPU", "AnyCPU")) {
        throw "PreferNativeArm64 requires the Any CPU platform."
    }

    if (-not $SkipRestore) {
        & $msbuild $solution "/t:Restore" "/p:RestorePackagesConfig=true" "/v:m"
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    $buildArgs = @(
        $solution,
        "/m:1",
        "/nr:false",
        "/p:UseSharedCompilation=false",
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform"
    )
    if ($PreferNativeArm64) {
        $buildArgs += "/p:PreferNativeArm64=true"
    }
    $buildArgs += "/v:m"

    & $msbuild @buildArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}

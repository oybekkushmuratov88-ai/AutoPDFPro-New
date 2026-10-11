<#
.SYNOPSIS
    Builds AutoPDF Pro, runs the unit tests and packages the bundle.

.DESCRIPTION
    Output:
      artifacts\AutoPDFPro.bundle          ready-to-copy AutoCAD bundle (R24 = 2021-2024, R25 = 2025-2026)
      artifacts\ZWCAD\<year>               ZWCAD 2024 / 2025 / 2026 builds
      dist\AutoPDFPro-<ver>.zip            bundle + installer scripts + README (manual installation)
      dist\AutoPDFPro-<ver>-Setup.exe      single-file installer; needs NSIS 3 (makensis.exe):
                                           https://nsis.sourceforge.io  or  winget install NSIS.NSIS

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build\build.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build\build.ps1 -Configuration Debug -SkipTests
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repo
try {
    # The version lives in Directory.Build.props (also used for the DLLs and the installer).
    $version = [regex]::Match((Get-Content (Join-Path $repo 'Directory.Build.props') -Raw), '<Version>([^<]+)</Version>').Groups[1].Value
    if (-not $version) { throw "Version not found in Directory.Build.props." }
    $bundle = Join-Path $repo 'artifacts\AutoPDFPro.bundle'
    $dist = Join-Path $repo 'dist'

    # Start from clean staging folders so no stale files (e.g. Debug .pdb) are shipped.
    if (Test-Path $bundle) { Remove-Item $bundle -Recurse -Force }
    $zwcad = Join-Path $repo 'artifacts\ZWCAD'
    if (Test-Path $zwcad) { Remove-Item $zwcad -Recurse -Force }

    dotnet build AutoPDFPro.sln -c $Configuration -nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }

    if (-not $SkipTests) {
        dotnet test tests\AutoPDF.Core.Tests\AutoPDF.Core.Tests.csproj -c $Configuration -nologo --no-build
        if ($LASTEXITCODE -ne 0) { throw "Unit tests failed." }
        if (Get-Command node -ErrorAction SilentlyContinue) {
            node --test (Get-ChildItem server\licensing\test\*.test.js).FullName
            if ($LASTEXITCODE -ne 0) { throw "Licence server tests failed." }
        }
    }

    # Package: zip with the bundle, installer scripts and docs.
    New-Item -ItemType Directory -Path $dist -Force | Out-Null
    $stage = Join-Path $dist "AutoPDFPro-$version"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Path $stage | Out-Null
    Copy-Item $bundle -Destination (Join-Path $stage 'AutoPDFPro.bundle') -Recurse
    Copy-Item $zwcad -Destination (Join-Path $stage 'ZWCAD') -Recurse
    Copy-Item (Join-Path $repo 'installer\install.ps1'), (Join-Path $repo 'installer\install.bat'),
              (Join-Path $repo 'installer\install-peruser.bat'), (Join-Path $repo 'installer\uninstall.ps1'),
              (Join-Path $repo 'installer\uninstall.bat'), (Join-Path $repo 'README.md') -Destination $stage
    Copy-Item (Join-Path $repo 'docs') -Destination (Join-Path $stage 'docs') -Recurse

    $zip = Join-Path $dist "AutoPDFPro-$version.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
    Write-Host "Package: $zip" -ForegroundColor Green

    # Single-file installer (installer\AutoPDFPro.nsi, NSIS 3).
    $makensis = @(
        "${env:ProgramFiles(x86)}\NSIS\makensis.exe",
        "$env:ProgramFiles\NSIS\makensis.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $makensis) {
        $cmd = Get-Command makensis -ErrorAction SilentlyContinue
        if ($cmd) { $makensis = $cmd.Source }
    }
    if ($makensis) {
        & $makensis /V2 "/DVERSION=$version" (Join-Path $repo 'installer\AutoPDFPro.nsi')
        if ($LASTEXITCODE -ne 0) { throw "makensis failed." }
        Write-Host "Setup:   $(Join-Path $dist "AutoPDFPro-$version-Setup.exe")" -ForegroundColor Green
    } else {
        Write-Host "NSIS (makensis.exe) not found - Setup.exe skipped. Install NSIS 3 (winget install NSIS.NSIS) and run again." -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}

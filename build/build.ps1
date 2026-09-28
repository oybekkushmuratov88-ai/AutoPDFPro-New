<#
.SYNOPSIS
    Builds AutoPDF Pro, runs the unit tests and packages the bundle.

.DESCRIPTION
    Output:
      artifacts\AutoPDFPro.bundle          ready-to-copy AutoCAD bundle
      dist\AutoPDFPro-<ver>.zip            bundle + installer scripts + README
      dist\AutoPDFPro-<ver>-Setup.exe      only if Inno Setup 6 (ISCC.exe) is installed

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
    $version = '2.0.0'
    $bundle = Join-Path $repo 'artifacts\AutoPDFPro.bundle'
    $dist = Join-Path $repo 'dist'

    # Start from a clean bundle so no stale files (e.g. Debug .pdb) are shipped.
    if (Test-Path $bundle) { Remove-Item $bundle -Recurse -Force }

    dotnet build AutoPDFPro.sln -c $Configuration -nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }

    if (-not $SkipTests) {
        dotnet test tests\AutoPDF.Core.Tests\AutoPDF.Core.Tests.csproj -c $Configuration -nologo --no-build
        if ($LASTEXITCODE -ne 0) { throw "Unit tests failed." }
    }

    # Package: zip with the bundle, installer scripts and docs.
    New-Item -ItemType Directory -Path $dist -Force | Out-Null
    $stage = Join-Path $dist "AutoPDFPro-$version"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Path $stage | Out-Null
    Copy-Item $bundle -Destination (Join-Path $stage 'AutoPDFPro.bundle') -Recurse
    Copy-Item (Join-Path $repo 'installer\install.ps1'), (Join-Path $repo 'installer\install.bat'),
              (Join-Path $repo 'installer\install-peruser.bat'), (Join-Path $repo 'installer\uninstall.ps1'),
              (Join-Path $repo 'installer\uninstall.bat'), (Join-Path $repo 'README.md') -Destination $stage
    Copy-Item (Join-Path $repo 'docs') -Destination (Join-Path $stage 'docs') -Recurse

    $zip = Join-Path $dist "AutoPDFPro-$version.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
    Write-Host "Package: $zip" -ForegroundColor Green

    $iscc = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($iscc) {
        & $iscc (Join-Path $repo 'installer\AutoPDFPro.iss')
        if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed." }
    } else {
        Write-Host "Inno Setup 6 not found - Setup.exe skipped (zip + install.ps1 are enough)." -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}

<#
.SYNOPSIS
    Installs the AutoPDF Pro bundle for AutoCAD 2021-2026 (manual alternative to Setup.exe; ZWCAD needs Setup.exe or NETLOAD, see docs\COMPATIBILITY.md).

.DESCRIPTION
    Copies AutoPDFPro.bundle to an Autodesk ApplicationPlugins folder, unblocks the files
    (downloaded DLLs are otherwise refused by .NET) and moves older AutoPDF Pro bundles to a
    backup folder so the AUTOPDF command is not defined twice.

    Default target (all users, trusted by AutoCAD's SECURELOAD):
        %ProgramFiles%\Autodesk\ApplicationPlugins
    -ProgramData : %ProgramData%\Autodesk\ApplicationPlugins (all users)
    -PerUser     : %AppData%\Autodesk\ApplicationPlugins     (no admin rights needed;
                   AutoCAD may ask once whether to load the DLL - choose "Always load")

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File install.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File install.ps1 -PerUser
#>
[CmdletBinding()]
param(
    [switch]$PerUser,
    [switch]$ProgramData,
    [string]$Source,
    [switch]$NoPrompt
)

$ErrorActionPreference = 'Stop'
$BundleName = 'AutoPDFPro.bundle'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal $id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# ---- Locate the bundle ------------------------------------------------------------------
if (-not $Source) {
    $candidates = @(
        (Join-Path $PSScriptRoot $BundleName),
        (Join-Path $PSScriptRoot "..\$BundleName"),
        (Join-Path $PSScriptRoot "..\artifacts\$BundleName")
    )
    $Source = $candidates | Where-Object { Test-Path (Join-Path $_ 'PackageContents.xml') } | Select-Object -First 1
}
if (-not $Source -or -not (Test-Path (Join-Path $Source 'PackageContents.xml'))) {
    throw "AutoPDFPro.bundle topilmadi. Avval build.ps1 ni ishga tushiring yoki -Source bilan bundle papkasini ko'rsating."
}
$Source = (Resolve-Path $Source).Path
if (-not (Test-Path (Join-Path $Source 'Contents\Windows\R24\AutoPDF.AutoCAD.dll'))) {
    throw "Bundle to'liq emas: Contents\Windows\R24\AutoPDF.AutoCAD.dll yo'q ($Source)."
}

# ---- Target folder ----------------------------------------------------------------------
if ($PerUser) {
    $root = Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins'
} elseif ($ProgramData) {
    $root = Join-Path $env:ProgramData 'Autodesk\ApplicationPlugins'
} else {
    $root = Join-Path $env:ProgramFiles 'Autodesk\ApplicationPlugins'
}

if (-not $PerUser -and -not (Test-Admin)) {
    Write-Host "Administrator huquqi kerak - qayta ishga tushirilmoqda..." -ForegroundColor Yellow
    $args = @('-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"", '-Source', "`"$Source`"")
    if ($ProgramData) { $args += '-ProgramData' }
    if ($NoPrompt) { $args += '-NoPrompt' }
    Start-Process -FilePath 'powershell.exe' -ArgumentList $args -Verb RunAs -Wait
    exit
}

# ---- AutoCAD must not be running (it locks the DLLs) -------------------------------------
while (Get-Process -Name 'acad' -ErrorAction SilentlyContinue) {
    if ($NoPrompt) { throw "AutoCAD ishlayapti. Uni yoping va qayta urinib ko'ring." }
    Read-Host "AutoCAD ishlayapti. Uni yoping va ENTER bosing"
}

# ---- Move older AutoPDF Pro bundles out of the way ----------------------------------------
$backupRoot = Join-Path $env:LOCALAPPDATA ("AutoPDFPro\Backup\" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$pluginRoots = @(
    (Join-Path $env:ProgramFiles 'Autodesk\ApplicationPlugins'),
    (Join-Path $env:ProgramData 'Autodesk\ApplicationPlugins'),
    (Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins')
) | Select-Object -Unique

$destination = Join-Path $root $BundleName
foreach ($pluginRoot in $pluginRoots) {
    if (-not (Test-Path $pluginRoot)) { continue }
    Get-ChildItem -Path $pluginRoot -Directory -Filter '*.bundle' | ForEach-Object {
        $manifest = Join-Path $_.FullName 'PackageContents.xml'
        if (-not (Test-Path $manifest)) { return }
        $text = Get-Content -Path $manifest -Raw
        if ($text -notmatch 'AutoPDF') { return }
        if ($_.FullName -eq $destination) { return }   # replaced below
        try {
            New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
            Move-Item -Path $_.FullName -Destination (Join-Path $backupRoot $_.Name) -Force
            Write-Host "Eski versiya zaxiraga ko'chirildi: $($_.FullName) -> $backupRoot" -ForegroundColor Yellow
        } catch {
            Write-Warning "Eski bundle ko'chirilmadi ($($_.Exception.Message)): $($_.FullName). Uni qo'lda o'chiring - aks holda AUTOPDF buyrug'i ikki marta aniqlanadi."
        }
    }
}

# ---- Copy -------------------------------------------------------------------------------
New-Item -ItemType Directory -Path $root -Force | Out-Null
if (Test-Path $destination) {
    Remove-Item -Path $destination -Recurse -Force
}
Copy-Item -Path $Source -Destination $destination -Recurse -Force
Get-ChildItem -Path $destination -Recurse -File | Unblock-File

Write-Host ""
Write-Host "AutoPDF Pro o'rnatildi: $destination" -ForegroundColor Green
Write-Host "AutoCAD (2021-2026) ni ishga tushiring va buyruq qatoriga AUTOPDF yozing."
if ($PerUser) {
    Write-Host "Agar AutoCAD DLL ni yuklash haqida so'rasa - 'Всегда загружать' (Always load) ni tanlang."
}

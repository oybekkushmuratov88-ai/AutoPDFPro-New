<#
.SYNOPSIS
    Removes the AutoPDF Pro bundle (all install locations). Settings and logs are kept unless -Purge.
#>
[CmdletBinding()]
param(
    [switch]$Purge,
    [switch]$NoPrompt
)

$ErrorActionPreference = 'Stop'

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal $id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$targets = @(
    (Join-Path $env:ProgramFiles 'Autodesk\ApplicationPlugins\AutoPDFPro.bundle'),
    (Join-Path $env:ProgramData 'Autodesk\ApplicationPlugins\AutoPDFPro.bundle'),
    (Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\AutoPDFPro.bundle')
) | Where-Object { Test-Path $_ }

$needsAdmin = $targets | Where-Object { -not $_.StartsWith($env:APPDATA) }
if ($needsAdmin -and -not (Test-Admin)) {
    $args = @('-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    if ($Purge) { $args += '-Purge' }
    if ($NoPrompt) { $args += '-NoPrompt' }
    Start-Process -FilePath 'powershell.exe' -ArgumentList $args -Verb RunAs -Wait
    exit
}

while (Get-Process -Name 'acad' -ErrorAction SilentlyContinue) {
    if ($NoPrompt) { throw "AutoCAD ishlayapti. Uni yoping va qayta urinib ko'ring." }
    Read-Host "AutoCAD ishlayapti. Uni yoping va ENTER bosing"
}

foreach ($t in $targets) {
    Remove-Item -Path $t -Recurse -Force
    Write-Host "O'chirildi: $t"
}
if (-not $targets) { Write-Host "AutoPDF Pro bundle topilmadi." }

if ($Purge) {
    foreach ($p in @((Join-Path $env:APPDATA 'AutoPDFPro'), (Join-Path $env:LOCALAPPDATA 'AutoPDFPro'))) {
        if (Test-Path $p) { Remove-Item -Path $p -Recurse -Force; Write-Host "O'chirildi: $p" }
    }
}

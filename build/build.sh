#!/usr/bin/env bash
# Builds AutoPDF Pro on Linux or macOS: the plug-in (reference assemblies come from NuGet, AutoCAD is not
# needed), the unit tests and the single-file installer dist/AutoPDFPro-<version>-Setup.exe.
#
#   build/build.sh [Release|Debug]
#
# Needs the .NET 8 SDK and NSIS 3 (Debian/Ubuntu: apt install nsis, macOS: brew install makensis).
# On Windows use build\build.ps1, which also produces the zip for manual installation.
set -euo pipefail

cd "$(dirname "$0")/.."
configuration="${1:-Release}"
version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)"
[ -n "$version" ] || { echo "Version not found in Directory.Build.props" >&2; exit 1; }

# Start from a clean bundle so no stale files are packed.
rm -rf artifacts/AutoPDFPro.bundle

dotnet build AutoPDFPro.sln -c "$configuration" -nologo
dotnet test tests/AutoPDF.Core.Tests/AutoPDF.Core.Tests.csproj -c "$configuration" -nologo --no-build

mkdir -p dist
makensis -V2 -DVERSION="$version" installer/AutoPDFPro.nsi
echo "Setup: dist/AutoPDFPro-$version-Setup.exe"

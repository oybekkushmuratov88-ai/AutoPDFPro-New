#!/usr/bin/env bash
# Builds AutoPDF Pro on Linux or macOS: every CAD build (AutoCAD R24 + R25, ZWCAD 2024/2025/2026; the CAD
# reference assemblies come from NuGet, no CAD is needed), the tests (.NET and, when Node.js is installed,
# the licence server's Apps Script tests) and the single-file installer dist/AutoPDFPro-<version>-Setup.exe.
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

# Start from clean staging folders so no stale files are packed.
rm -rf artifacts/AutoPDFPro.bundle artifacts/ZWCAD

dotnet build AutoPDFPro.sln -c "$configuration" -nologo
dotnet test tests/AutoPDF.Core.Tests/AutoPDF.Core.Tests.csproj -c "$configuration" -nologo --no-build
if command -v node >/dev/null 2>&1; then
  node --test server/licensing/test/*.test.js
fi

mkdir -p dist
makensis -V2 -DVERSION="$version" installer/AutoPDFPro.nsi
echo "Setup: dist/AutoPDFPro-$version-Setup.exe"

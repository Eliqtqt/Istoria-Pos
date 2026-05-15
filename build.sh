#!/usr/bin/env bash
# Render/Nixpacks build script — force .NET 8, publish to ./out
set -euo pipefail

# Always start from the directory this script lives in
cd "$(cd "$(dirname "$0")" && pwd)"

echo "=== Render .NET 8 Build ==="
echo "Working dir: $(pwd)"

# Show .NET info
dotnet --info
dotnet --version

# Restore
echo "--- Restoring ---"
dotnet restore CafeWebsite.csproj --verbosity minimal

# Publish
echo "--- Publishing ---"
dotnet publish CafeWebsite.csproj -c Release -o out --no-restore --verbosity minimal

echo "=== Build complete ==="
ls -la ./out

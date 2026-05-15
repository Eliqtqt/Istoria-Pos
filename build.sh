#!/bin/bash
# Force .NET 8 build when using Nixpacks/auto-build
dotnet restore CafeWebsite.csproj
dotnet publish CafeWebsite.csproj -c Release -o out

#!/bin/bash
# Nixpacks build script — force .NET 8, publish to ./out
dotnet restore CafeWebsite.csproj
dotnet publish CafeWebsite.csproj -c Release -o out --no-restore

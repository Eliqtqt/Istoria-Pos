# Build arg to invalidate cache - 2026-05-19T03:21Z
ARG CACHE_BUST=2026-05-19T03:21Z

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CafeWebsite.csproj", "./"]
RUN dotnet restore "CafeWebsite.csproj" --verbosity minimal

COPY . .
RUN dotnet publish "CafeWebsite.csproj" -c Release -o /app/publish --no-restore --verbosity minimal

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CafeWebsite.dll"]

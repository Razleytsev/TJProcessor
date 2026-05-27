# syntax=docker/dockerfile:1.7
# Build context expected to be the solution root.

# ── Build stage ─────────────────────────────────────────────────────────────
# SDK 9 (not 8) — matches the Web image and stays forward-compatible with
# anything else in the solution. Target framework stays net8.0.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files first to maximise restore-layer cache hits
COPY TJConnector.Api/TJConnector.Api.csproj                       TJConnector.Api/
COPY TJConnector.Postgres/TJConnector.Postgres.csproj             TJConnector.Postgres/
COPY TJConnector.StateSystem/TJConnector.StateSystem.csproj       TJConnector.StateSystem/
COPY SharedLibrary/TJConnector.SharedLibrary.csproj               SharedLibrary/

RUN dotnet restore TJConnector.Api/TJConnector.Api.csproj

# Copy the rest of the sources and publish a release build
COPY TJConnector.Api/         TJConnector.Api/
COPY TJConnector.Postgres/    TJConnector.Postgres/
COPY TJConnector.StateSystem/ TJConnector.StateSystem/
COPY SharedLibrary/           SharedLibrary/

RUN dotnet publish TJConnector.Api/TJConnector.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ── Runtime stage ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Serilog writes a daily rolling file under %ProgramData%\TJConnectorAPI on
# Windows. In Linux containers Environment.GetFolderPath(CommonApplicationData)
# resolves to /usr/share. Pre-create the directory so the file sink can write.
RUN mkdir -p /usr/share/TJConnectorAPI

ENV ASPNETCORE_URLS=http://+:5166 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 5166
ENTRYPOINT ["dotnet", "TJConnector.Api.dll"]

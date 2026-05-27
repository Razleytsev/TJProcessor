# syntax=docker/dockerfile:1.7
# Build context expected to be the solution root.

# ── Build stage ─────────────────────────────────────────────────────────────
# SDK 9 (not 8) — fixes a Razor parser bug with C# 9 relational patterns
# (`< 0` in @code switch expressions). Target framework stays net8.0.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY TJConnector.Web/TJConnector.Web.csproj                       TJConnector.Web/
COPY TJConnector.Postgres/TJConnector.Postgres.csproj             TJConnector.Postgres/
COPY TJConnector.StateSystem/TJConnector.StateSystem.csproj       TJConnector.StateSystem/
COPY SharedLibrary/TJConnector.SharedLibrary.csproj               SharedLibrary/

RUN dotnet restore TJConnector.Web/TJConnector.Web.csproj

COPY TJConnector.Web/         TJConnector.Web/
COPY TJConnector.Postgres/    TJConnector.Postgres/
COPY TJConnector.StateSystem/ TJConnector.StateSystem/
COPY SharedLibrary/           SharedLibrary/

RUN dotnet publish TJConnector.Web/TJConnector.Web.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ── Runtime stage ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:5113 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 5113
ENTRYPOINT ["dotnet", "TJConnector.Web.dll"]

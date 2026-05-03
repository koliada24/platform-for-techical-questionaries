# syntax=docker/dockerfile:1.7

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (leverage layer cache)
COPY Api.csproj ./
RUN dotnet restore Api.csproj

# Build & publish
COPY . ./
RUN dotnet publish Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Persisted SQLite location
RUN mkdir -p /app/data
VOLUME ["/app/data"]

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Data Source=/app/data/app.db"

EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Api.dll"]

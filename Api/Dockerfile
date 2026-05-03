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
# We use the SDK image at runtime because csharp-ls (Roslyn-based LSP) needs the
# .NET SDK to load student workspaces (MSBuildWorkspace + reference assemblies).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS runtime
WORKDIR /app

# csharp-ls is published targeting an older .NET, so install the .NET 8 runtime
# alongside the SDK 10 already in the image. They co-exist in /usr/share/dotnet.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl ca-certificates \
 && curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
 && chmod +x /tmp/dotnet-install.sh \
 && /tmp/dotnet-install.sh --runtime dotnet --channel 8.0 --install-dir /usr/share/dotnet \
 && rm -rf /var/lib/apt/lists/* /tmp/dotnet-install.sh

# Install csharp-ls globally as a dotnet tool. It is invoked per WebSocket session
# by Api.Controllers.LspController.
ENV PATH="/root/.dotnet/tools:${PATH}"
RUN dotnet tool install --global csharp-ls

# Persisted SQLite location
RUN mkdir -p /app/data
VOLUME ["/app/data"]

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Data Source=/app/data/app.db"

EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Api.dll"]

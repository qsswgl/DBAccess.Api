# Multi-stage build for DBAccess.Api microservice
# Base on Ubuntu with .NET 8.0 (LTS)

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-ubuntu AS build
WORKDIR /src

# Copy csproj and restore dependencies (cached layer)
COPY DBAccess.Api.csproj ./
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore --self-contained false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-ubuntu AS runtime

# Install additional packages if needed
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r dbaccess && useradd -r -g dbaccess dbaccess

WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R dbaccess:dbaccess /app

# Switch to non-root user
USER dbaccess

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_EnableDiagnostics=0

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/api/dbaccess/ping?db=master || exit 1

# Entry point
ENTRYPOINT ["dotnet", "DBAccess.Api.dll"]
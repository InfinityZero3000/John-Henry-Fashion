# ===========================
# Multi-stage build optimized for Render
# ===========================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (layer cached)
COPY ["JohnHenryFashionWeb.csproj", "./"]
RUN dotnet restore "JohnHenryFashionWeb.csproj" --runtime linux-x64

# Copy source code
COPY . .

# Build and publish in one step (faster)
RUN dotnet publish "JohnHenryFashionWeb.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --runtime linux-x64 \
    --self-contained false \
    /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Install curl for healthcheck
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    mkdir -p /app/wwwroot/uploads /app/logs && \
    groupadd -r appuser && \
    useradd -r -g appuser appuser && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "JohnHenryFashionWeb.dll"]

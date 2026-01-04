#!/bin/bash

# ===========================
# RENDER DEPLOYMENT SCRIPT
# Simplified version - no migrations on startup
# ===========================

set -e  # Exit on error

echo "🚀 Starting John Henry Fashion..."
echo "📦 Environment: $ASPNETCORE_ENVIRONMENT"
echo "🌐 Listening on: $ASPNETCORE_URLS"

# Start the application
exec dotnet JohnHenryFashionWeb.dll

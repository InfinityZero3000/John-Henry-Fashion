#!/bin/bash

# ===========================
# RENDER DEPLOYMENT SCRIPT
# Auto-run database migrations on deployment
# ===========================

set -e  # Exit on error

echo "🚀 Starting John Henry Fashion deployment..."

# 1. Run database migrations
echo "📊 Running database migrations..."
dotnet ef database update --no-build

echo "✅ Migrations completed successfully!"

# 2. Start the application
echo "🌐 Starting web application..."
exec dotnet JohnHenryFashionWeb.dll

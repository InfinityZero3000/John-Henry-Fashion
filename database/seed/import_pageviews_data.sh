#!/bin/bash

# Script to insert sample PageViews data for dashboard analytics
# This will create visitor tracking data for the last 7 days

echo "🔄 Inserting sample PageViews and UserSessions data..."

# Get database connection string from appsettings
DB_NAME="johnhenry_db"
DB_USER="johnhenry_user"
DB_HOST="localhost"
DB_PORT="5432"

# Check if database exists
echo "📊 Checking database connection..."
if ! psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c '\q' 2>/dev/null; then
    echo "❌ Cannot connect to database. Please check your PostgreSQL server."
    echo "   Host: $DB_HOST"
    echo "   Port: $DB_PORT"
    echo "   Database: $DB_NAME"
    echo "   User: $DB_USER"
    exit 1
fi

echo "✅ Database connection successful"

# Run the SQL file
echo "📥 Inserting PageViews data..."
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f "$(dirname "$0")/insert_sample_pageviews.sql"

if [ $? -eq 0 ]; then
    echo "✅ Sample PageViews data inserted successfully!"
    echo ""
    echo "📊 Dashboard analytics data is now available:"
    echo "   - User sessions created: ~500"
    echo "   - Page views for today: ~100"
    echo "   - Page views for yesterday: ~90"
    echo "   - Historical data: Last 7 days"
    echo ""
    echo "🔄 Refresh your dashboard to see the updated visitor metrics!"
else
    echo "❌ Error inserting data. Please check the error messages above."
    exit 1
fi

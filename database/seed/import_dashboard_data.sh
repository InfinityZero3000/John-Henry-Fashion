#!/bin/bash

# Import Dashboard Sample Data
# This script imports sample data for dashboard charts and analytics

echo "========================================="
echo "Import Dashboard Sample Data"
echo "========================================="

# Check if database connection info is available
if [ -z "$DB_HOST" ]; then
    echo "Using default Supabase connection..."
    DB_HOST="aws-0-ap-southeast-1.pooler.supabase.com"
    DB_PORT="6543"
    DB_NAME="postgres"
    DB_USER="postgres.vgjgkftlvhpuecaqrjds"
fi

# Import the sample data
echo "Importing sample dashboard data from insert_sample_dashboard_data_v2.sql..."
PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -d "$DB_NAME" -U "$DB_USER" -f "insert_sample_dashboard_data_v2.sql"

if [ $? -eq 0 ]; then
    echo "✓ Sample data imported successfully!"
    echo ""
    echo "You can now:"
    echo "1. Visit /admin/dashboard to see the revenue charts"
    echo "2. The charts will display 30 days of sample revenue data"
    echo "3. Check logs for 'Using X SalesReports records' to confirm data is loaded"
else
    echo "✗ Error importing sample data"
    echo "Please check your database connection and try again"
    exit 1
fi

echo "========================================="
echo "Import Complete!"
echo "========================================="

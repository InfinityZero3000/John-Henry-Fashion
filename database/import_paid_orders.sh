#!/bin/bash

# Import Paid Orders Sample Data
# This script creates sample PAID orders for dashboard revenue display

echo "========================================="
echo "Import Paid Orders Sample Data"
echo "========================================="

# Get database connection from appsettings.json
echo "Reading database connection from appsettings.json..."

# Extract connection string
DB_CONNECTION=$(grep -A 5 '"DefaultConnection"' ../appsettings.json | grep -o 'Host=[^;]*' | sed 's/Host=//')
DB_PORT=$(grep -A 5 '"DefaultConnection"' ../appsettings.json | grep -o 'Port=[^;]*' | sed 's/Port=//' | sed 's/;.*//')
DB_NAME=$(grep -A 5 '"DefaultConnection"' ../appsettings.json | grep -o 'Database=[^;]*' | sed 's/Database=//' | sed 's/;.*//')
DB_USER=$(grep -A 5 '"DefaultConnection"' ../appsettings.json | grep -o 'Username=[^;]*' | sed 's/Username=//' | sed 's/;.*//')
DB_PASSWORD=$(grep -A 5 '"DefaultConnection"' ../appsettings.json | grep -o 'Password=[^;]*' | sed 's/Password=//' | sed 's/;.*//')

echo "Database: $DB_NAME"
echo "Host: $DB_CONNECTION"
echo "Port: $DB_PORT"
echo "User: $DB_USER"
echo ""

# Import the sample paid orders
echo "Creating 30 paid orders for the last 30 days..."
PGPASSWORD="$DB_PASSWORD" psql -h "$DB_CONNECTION" -p "$DB_PORT" -d "$DB_NAME" -U "$DB_USER" -f "insert_paid_orders_sample.sql"

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Sample paid orders created successfully!"
    echo ""
    echo "Summary:"
    echo "- Created 30 orders with PaymentStatus = 'paid'"
    echo "- Orders span the last 30 days"
    echo "- Revenue ranges from 500,000đ to 2,500,000đ per order"
    echo ""
    echo "You can now:"
    echo "1. Restart the application"
    echo "2. Visit /admin/dashboard"
    echo "3. You should see revenue data displayed"
    echo ""
else
    echo "✗ Error creating sample data"
    echo "Please check your database connection and try again"
    exit 1
fi

echo "========================================="
echo "Import Complete!"
echo "========================================="

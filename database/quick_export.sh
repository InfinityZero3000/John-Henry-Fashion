#!/bin/bash

# Quick script to export important data tables
# Sử dụng: ./quick_export.sh

echo "=== Quick Data Export ==="
echo ""

# Create backups directory
mkdir -p ./backups

# Timestamp for filename
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Local database credentials
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="johnhenry_db"
DB_USER="johnhenry_user"

echo "Exporting data from: $DB_HOST:$DB_PORT/$DB_NAME"
echo "Output: ./backups/data_export_$TIMESTAMP.sql"
echo ""

# Export with password prompt
pg_dump -h "$DB_HOST" \
        -p "$DB_PORT" \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --data-only \
        --inserts \
        --column-inserts \
        -t "Products" \
        -t "Categories" \
        -t "Brands" \
        -t "ProductReviews" \
        -t "Orders" \
        -t "OrderItems" \
        -t "ShoppingCartItems" \
        -t "Addresses" \
        -t "Coupons" \
        -t "ShippingMethods" \
        -t "PaymentMethods" \
        -t "BlogPosts" \
        -t "FAQ" \
        -t "SupportTickets" \
        -t "Wishlists" \
        -f "./backups/data_export_$TIMESTAMP.sql"

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Export successful!"
    echo "File: ./backups/data_export_$TIMESTAMP.sql"
    echo "Size: $(du -h ./backups/data_export_$TIMESTAMP.sql | cut -f1)"
    echo ""
    echo "To import to production, run:"
    echo "psql -h YOUR_PROD_HOST -p 5432 -U YOUR_PROD_USER -d johnhenry_db -f ./backups/data_export_$TIMESTAMP.sql"
else
    echo ""
    echo "✗ Export failed!"
    exit 1
fi

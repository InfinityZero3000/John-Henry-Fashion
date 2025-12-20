#!/bin/bash
# Script backup database PostgreSQL tự động

set -e

echo "=========================================="
echo "PostgreSQL Database Backup Tool"
echo "=========================================="
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Paths
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
BACKUP_DIR="$SCRIPT_DIR/backups"

# Create backup directory if not exists
mkdir -p "$BACKUP_DIR"

# Read connection from appsettings.json
APPSETTINGS="$PROJECT_ROOT/appsettings.json"

if [ ! -f "$APPSETTINGS" ]; then
    echo "Error: appsettings.json not found!"
    exit 1
fi

echo "Reading database connection..."

CONN_STRING=$(grep -A 2 "DefaultConnection" "$APPSETTINGS" | grep -o '".*"' | tr -d '"' | tail -1)

# Parse connection string
DB_HOST=$(echo "$CONN_STRING" | grep -o 'Host=[^;]*' | cut -d'=' -f2)
DB_PORT=$(echo "$CONN_STRING" | grep -o 'Port=[^;]*' | cut -d'=' -f2)
DB_NAME=$(echo "$CONN_STRING" | grep -o 'Database=[^;]*' | cut -d'=' -f2)
DB_USER=$(echo "$CONN_STRING" | grep -o 'Username=[^;]*' | cut -d'=' -f2)
DB_PASS=$(echo "$CONN_STRING" | grep -o 'Password=[^;]*' | cut -d'=' -f2)

# Defaults - use current user if not specified
DB_HOST=${DB_HOST:-localhost}
DB_PORT=${DB_PORT:-5432}
DB_NAME=${DB_NAME:-johnhenry_db}
DB_USER=${DB_USER:-$(whoami)}

echo "✓ Database: $DB_NAME"
echo "✓ Host: $DB_HOST:$DB_PORT"
echo ""

# Generate backup filename with timestamp
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/backup_${DB_NAME}_${TIMESTAMP}.sql"

echo "Creating backup..."
echo "   File: $(basename "$BACKUP_FILE")"
echo ""

# Export password for pg_dump (only if password is set)
if [ -n "$DB_PASS" ]; then
    PGPASSWORD="$DB_PASS"
    export PGPASSWORD
fi

# Create backup
if pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
    --format=plain \
    --no-owner \
    --no-privileges \
    --verbose \
    > "$BACKUP_FILE" 2>&1; then
    
    # Get file size
    BACKUP_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
    
    echo ""
    echo -e "${GREEN}Backup completed successfully!${NC}"
    echo ""
    echo "Backup Information:"
    echo "File: $(basename "$BACKUP_FILE")"
    echo "Size: $BACKUP_SIZE"
    echo "Path: $BACKUP_FILE"
    echo ""
    
    # Count records
    echo "Database Statistics:"
    
    PRODUCT_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        -t -c 'SELECT COUNT(*) FROM "Products";' 2>/dev/null | tr -d ' ' || echo "N/A")
    echo "   Products: $PRODUCT_COUNT"
    
    CATEGORY_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        -t -c 'SELECT COUNT(*) FROM "Categories";' 2>/dev/null | tr -d ' ' || echo "N/A")
    echo "   Categories: $CATEGORY_COUNT"
    
    BRAND_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        -t -c 'SELECT COUNT(*) FROM "Brands";' 2>/dev/null | tr -d ' ' || echo "N/A")
    echo "   Brands: $BRAND_COUNT"
    
    ORDER_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        -t -c 'SELECT COUNT(*) FROM "Orders";' 2>/dev/null | tr -d ' ' || echo "N/A")
    echo "   Orders: $ORDER_COUNT"
    
    USER_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        -t -c 'SELECT COUNT(*) FROM "AspNetUsers";' 2>/dev/null | tr -d ' ' || echo "N/A")
    echo "   Users: $USER_COUNT"
    
    echo ""
    echo "To restore this backup:"
    echo "   psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME < $BACKUP_FILE"
    echo ""
    
    # List all backups
    echo "All backups in $BACKUP_DIR:"
    ls -lh "$BACKUP_DIR"/*.sql 2>/dev/null | awk '{print "   " $9 " (" $5 ")"}'
    
else
    echo ""
    echo "Backup failed!"
    exit 1
fi

unset PGPASSWORD

echo ""
echo "=========================================="
echo "Done!"
echo "=========================================="

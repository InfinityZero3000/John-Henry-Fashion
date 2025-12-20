#!/bin/bash
# Script restore database từ backup

set -e

echo "=========================================="
echo "PostgreSQL Database Restore Tool"
echo "=========================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Paths
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
BACKUP_DIR="$SCRIPT_DIR/backups"

# Check if backup directory exists
if [ ! -d "$BACKUP_DIR" ]; then
    echo "Backup directory not found: $BACKUP_DIR"
    exit 1
fi

# List available backups
echo "Available backups:"
echo ""
BACKUPS=($(ls -t "$BACKUP_DIR"/*.sql 2>/dev/null))

if [ ${#BACKUPS[@]} -eq 0 ]; then
    echo "No backup files found in $BACKUP_DIR"
    exit 1
fi

# Display backups with numbers
for i in "${!BACKUPS[@]}"; do
    SIZE=$(du -h "${BACKUPS[$i]}" | cut -f1)
    FILENAME=$(basename "${BACKUPS[$i]}")
    echo "   [$((i+1))] $FILENAME ($SIZE)"
done

echo ""
read -p "Select backup number to restore (or 0 to cancel): " CHOICE

if [ "$CHOICE" -eq 0 ]; then
    echo "Cancelled."
    exit 0
fi

if [ "$CHOICE" -lt 1 ] || [ "$CHOICE" -gt ${#BACKUPS[@]} ]; then
    echo "Invalid choice!"
    exit 1
fi

BACKUP_FILE="${BACKUPS[$((CHOICE-1))]}"

echo ""
echo "Selected backup: $(basename "$BACKUP_FILE")"
echo ""

# Read connection from appsettings.json
APPSETTINGS="$PROJECT_ROOT/appsettings.json"

if [ ! -f "$APPSETTINGS" ]; then
    echo "Error: appsettings.json not found!"
    exit 1
fi

CONN_STRING=$(grep -A 2 "DefaultConnection" "$APPSETTINGS" | grep -o '".*"' | tr -d '"' | tail -1)

# Parse connection string
DB_HOST=$(echo "$CONN_STRING" | grep -o 'Host=[^;]*' | cut -d'=' -f2)
DB_PORT=$(echo "$CONN_STRING" | grep -o 'Port=[^;]*' | cut -d'=' -f2)
DB_NAME=$(echo "$CONN_STRING" | grep -o 'Database=[^;]*' | cut -d'=' -f2)
DB_USER=$(echo "$CONN_STRING" | grep -o 'Username=[^;]*' | cut -d'=' -f2)
DB_PASS=$(echo "$CONN_STRING" | grep -o 'Password=[^;]*' | cut -d'=' -f2)

# Defaults - use current user
DB_HOST=${DB_HOST:-localhost}
DB_PORT=${DB_PORT:-5432}
DB_NAME=${DB_NAME:-johnhenry_db}
DB_USER=${DB_USER:-$(whoami)}

echo "Target Database:"
echo "   Database: $DB_NAME"
echo "   Host: $DB_HOST:$DB_PORT"
echo ""

# Warning
echo -e "${RED} WARNING: This will DELETE all current data and restore from backup!${NC}"
echo ""
read -p "Are you sure? Type 'YES' to confirm: " CONFIRM

if [ "$CONFIRM" != "YES" ]; then
    echo "Cancelled."
    exit 0
fi

echo ""
echo "Restoring database..."
echo ""

# Export password (only if set)
if [ -n "$DB_PASS" ]; then
    PGPASSWORD="$DB_PASS"
    export PGPASSWORD
fi

# Drop all tables (safer than dropping database)
echo "1. Dropping existing tables..."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "
DO \$\$ 
DECLARE
    r RECORD;
BEGIN
    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
        EXECUTE 'DROP TABLE IF EXISTS \"' || r.tablename || '\" CASCADE';
    END LOOP;
END \$\$;
" 2>&1

echo ""
echo "2. Restoring from backup..."

# Restore
if psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" < "$BACKUP_FILE" 2>&1; then
    echo ""
    echo -e "${GREEN}Restore completed successfully!${NC}"
    echo ""
    
    # Verify
    echo "Restored Database Statistics:"
    
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
    
else
    echo ""
    echo -e "${RED}Restore failed!${NC}"
    exit 1
fi

unset PGPASSWORD

echo ""
echo "=========================================="
echo "Done!"
echo "=========================================="

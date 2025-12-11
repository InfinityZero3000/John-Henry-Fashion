#!/bin/bash

# Script để export data từ local và import lên production
# Sử dụng: ./export_and_sync_data.sh

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== John Henry Database Sync Script ===${NC}\n"

# Local database info
LOCAL_HOST="localhost"
LOCAL_PORT="5432"
LOCAL_DB="johnhenry_db"
LOCAL_USER="johnhenry_user"

# Production database info (cần điền)
echo -e "${YELLOW}Nhập thông tin database production:${NC}"
read -p "Production Host (e.g., your-server.com): " PROD_HOST
read -p "Production Port (default 5432): " PROD_PORT
PROD_PORT=${PROD_PORT:-5432}
read -p "Production Database Name (default johnhenry_db): " PROD_DB
PROD_DB=${PROD_DB:-johnhenry_db}
read -p "Production Username (default johnhenry_user): " PROD_USER
PROD_USER=${PROD_USER:-johnhenry_user}
read -sp "Production Password: " PROD_PASSWORD
echo ""

# Export file
EXPORT_FILE="local_data_export_$(date +%Y%m%d_%H%M%S).sql"
BACKUP_DIR="./backups"

echo -e "\n${GREEN}Step 1: Creating backup directory...${NC}"
mkdir -p "$BACKUP_DIR"

echo -e "${GREEN}Step 2: Exporting data from LOCAL database...${NC}"
echo "Database: $LOCAL_HOST:$LOCAL_PORT/$LOCAL_DB"

# Export data only (with INSERT statements for compatibility)
PGPASSWORD="$LOCAL_PASSWORD" pg_dump \
    -h "$LOCAL_HOST" \
    -p "$LOCAL_PORT" \
    -U "$LOCAL_USER" \
    -d "$LOCAL_DB" \
    --data-only \
    --inserts \
    --disable-triggers \
    -f "$BACKUP_DIR/$EXPORT_FILE"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Export successful: $BACKUP_DIR/$EXPORT_FILE${NC}"
    echo -e "File size: $(du -h "$BACKUP_DIR/$EXPORT_FILE" | cut -f1)"
else
    echo -e "${RED}✗ Export failed!${NC}"
    exit 1
fi

echo -e "\n${YELLOW}Step 3: Ready to import to PRODUCTION database${NC}"
echo "Target: $PROD_HOST:$PROD_PORT/$PROD_DB"
read -p "Continue with import? (y/n): " CONFIRM

if [ "$CONFIRM" != "y" ]; then
    echo -e "${YELLOW}Import cancelled. Export file saved at: $BACKUP_DIR/$EXPORT_FILE${NC}"
    exit 0
fi

echo -e "${GREEN}Step 4: Importing data to PRODUCTION database...${NC}"

# Import to production
PGPASSWORD="$PROD_PASSWORD" psql \
    -h "$PROD_HOST" \
    -p "$PROD_PORT" \
    -U "$PROD_USER" \
    -d "$PROD_DB" \
    -f "$BACKUP_DIR/$EXPORT_FILE"

if [ $? -eq 0 ]; then
    echo -e "\n${GREEN}✓✓✓ Data sync completed successfully! ✓✓✓${NC}"
    echo -e "Export file: $BACKUP_DIR/$EXPORT_FILE"
else
    echo -e "\n${RED}✗ Import failed! Please check the error messages above.${NC}"
    echo -e "Export file is safe at: $BACKUP_DIR/$EXPORT_FILE"
    exit 1
fi

echo -e "\n${GREEN}=== Sync Summary ===${NC}"
echo "Local DB: $LOCAL_HOST:$LOCAL_PORT/$LOCAL_DB"
echo "Production DB: $PROD_HOST:$PROD_PORT/$PROD_DB"
echo "Export file: $BACKUP_DIR/$EXPORT_FILE"
echo -e "\n${GREEN}Done!${NC}"

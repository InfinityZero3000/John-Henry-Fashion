#!/bin/bash

# Export current data from local database
# This script exports data from the local PostgreSQL database running on localhost:5432

set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}=== Exporting Data from Local Database ===${NC}"
echo ""

# Timestamp for backup file
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="./backups/local_data_export_${TIMESTAMP}.sql"

# Local database credentials
LOCAL_HOST="localhost"
LOCAL_PORT="5432"
LOCAL_DB="johnhenry_db"
LOCAL_USER="johnhenry_user"

echo -e "${YELLOW}Database:${NC} ${LOCAL_DB}"
echo -e "${YELLOW}Backup file:${NC} ${BACKUP_FILE}"
echo ""

# Export data from all tables
echo -e "${YELLOW}Exporting data...${NC}"

PGPASSWORD="John@Henry2024" pg_dump \
  -h ${LOCAL_HOST} \
  -p ${LOCAL_PORT} \
  -U ${LOCAL_USER} \
  -d ${LOCAL_DB} \
  --data-only \
  --column-inserts \
  --disable-triggers \
  --exclude-table-data='__EFMigrationsHistory' \
  --exclude-table-data='AspNetRoleClaims' \
  --exclude-table-data='AspNetUserClaims' \
  --exclude-table-data='AspNetUserLogins' \
  --exclude-table-data='AspNetUserRoles' \
  --exclude-table-data='AspNetUserTokens' \
  > ${BACKUP_FILE}

if [ $? -eq 0 ]; then
  echo -e "${GREEN}✓ Export successful!${NC}"
  echo -e "${GREEN}File: ${BACKUP_FILE}${NC}"
  echo ""
  
  # Get file size
  FILE_SIZE=$(du -h ${BACKUP_FILE} | cut -f1)
  echo -e "${YELLOW}File size:${NC} ${FILE_SIZE}"
  
  # Count lines
  LINE_COUNT=$(wc -l < ${BACKUP_FILE})
  echo -e "${YELLOW}Lines:${NC} ${LINE_COUNT}"
  echo ""
  
  echo -e "${YELLOW}=== Next Steps ===${NC}"
  echo -e "1. Mở pgAdmin tại: ${YELLOW}http://localhost:8080/browser/${NC}"
  echo -e "2. Kết nối đến production database server"
  echo -e "3. Right-click vào database → ${YELLOW}Restore${NC}"
  echo -e "4. Hoặc chạy Query Tool và paste nội dung file: ${YELLOW}${BACKUP_FILE}${NC}"
  echo ""
  echo -e "${GREEN}=== Hoặc import bằng psql ===${NC}"
  echo -e "Nếu production database có connection string, chạy:"
  echo -e "${YELLOW}psql -h YOUR_PRODUCTION_HOST -p 5432 -U johnhenry_user -d johnhenry_db -f ${BACKUP_FILE}${NC}"
  echo ""
else
  echo -e "${RED}✗ Export failed!${NC}"
  exit 1
fi

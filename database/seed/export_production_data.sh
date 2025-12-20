#!/bin/bash
# Export local database and import to production

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${YELLOW}=== Export Local Database ===${NC}"

# Local database credentials
LOCAL_HOST="localhost"
LOCAL_PORT="5432"
LOCAL_DB="johnhenry_db"
LOCAL_USER="johnhenry_user"
LOCAL_PASS="***"

# Export file
EXPORT_FILE="johnhenry_backup_$(date +%Y%m%d_%H%M%S).sql"

echo -e "${GREEN}Exporting local database to ${EXPORT_FILE}...${NC}"

# Export full database
PGPASSWORD=$LOCAL_PASS pg_dump \
  -h $LOCAL_HOST \
  -p $LOCAL_PORT \
  -U $LOCAL_USER \
  -d $LOCAL_DB \
  -F p \
  -f "$EXPORT_FILE"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Export successful!${NC}"
    echo -e "${GREEN}File size: $(ls -lh $EXPORT_FILE | awk '{print $5}')${NC}"
else
    echo -e "${RED}✗ Export failed!${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}=== Next Steps ===${NC}"
echo "1. Get your production database URL from Railway/Render"
echo "2. Run: psql \$PRODUCTION_DATABASE_URL -f $EXPORT_FILE"
echo ""
echo "Or if you want to import specific tables only:"
echo "   psql \$PRODUCTION_DATABASE_URL -c \"TRUNCATE TABLE \\\"Payments\\\" CASCADE;\""
echo "   psql \$PRODUCTION_DATABASE_URL -f $EXPORT_FILE"

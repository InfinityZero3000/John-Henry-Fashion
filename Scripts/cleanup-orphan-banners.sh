#!/bin/bash

# Script to clean up orphan banner images
# Run this script from the project root directory

echo "==================================="
echo "Banner Image Cleanup Script"
echo "==================================="

# Set paths
BANNER_DIR="wwwroot/images/banners"
DB_CONNECTION="Host=localhost;Port=5432;Database=johnhenry_db;Username=johnhenry_user;Password=JohnHenry@2025!"

# Check if banner directory exists
if [ ! -d "$BANNER_DIR" ]; then
    echo "Error: Banner directory not found: $BANNER_DIR"
    exit 1
fi

echo ""
echo "Step 1: Finding all banner files..."
find "$BANNER_DIR" -type f \( -name "banner_*" -o -name "banner_mobile_*" \) | wc -l
echo "Total banner files found."

echo ""
echo "Step 2: Fetching banner URLs from database..."

# Create temporary SQL file
cat > /tmp/get_banner_urls.sql << 'EOF'
\t
\a
SELECT DISTINCT image_url FROM marketing_banners WHERE image_url IS NOT NULL
UNION
SELECT DISTINCT mobile_image_url FROM marketing_banners WHERE mobile_image_url IS NOT NULL;
EOF

# Execute SQL and save to temp file
psql "$DB_CONNECTION" -f /tmp/get_banner_urls.sql > /tmp/db_banner_urls.txt 2>/dev/null

# Count URLs from database
DB_URLS_COUNT=$(grep -c "^/images/banners/" /tmp/db_banner_urls.txt 2>/dev/null || echo "0")
echo "Found $DB_URLS_COUNT unique banner URLs in database"

echo ""
echo "Step 3: Finding orphan files (files not in database)..."

ORPHAN_COUNT=0
ORPHAN_SIZE=0

# Loop through all banner files
while IFS= read -r filepath; do
    filename=$(basename "$filepath")
    relative_path="/images/banners/$filename"
    
    # Check if this URL exists in database
    if ! grep -q "$relative_path" /tmp/db_banner_urls.txt 2>/dev/null; then
        filesize=$(stat -f%z "$filepath" 2>/dev/null || stat -c%s "$filepath" 2>/dev/null)
        ORPHAN_COUNT=$((ORPHAN_COUNT + 1))
        ORPHAN_SIZE=$((ORPHAN_SIZE + filesize))
        echo "Orphan: $filename ($(numfmt --to=iec-i --suffix=B $filesize 2>/dev/null || echo "${filesize}B"))"
    fi
done < <(find "$BANNER_DIR" -type f \( -name "banner_*" -o -name "banner_mobile_*" \))

echo ""
echo "==================================="
echo "Summary:"
echo "==================================="
echo "Total orphan files: $ORPHAN_COUNT"
echo "Total orphan size: $(numfmt --to=iec-i --suffix=B $ORPHAN_SIZE 2>/dev/null || echo "${ORPHAN_SIZE}B")"

if [ $ORPHAN_COUNT -gt 0 ]; then
    echo ""
    read -p "Do you want to DELETE these orphan files? (yes/no): " confirm
    if [ "$confirm" = "yes" ]; then
        echo ""
        echo "Deleting orphan files..."
        DELETED_COUNT=0
        
        while IFS= read -r filepath; do
            filename=$(basename "$filepath")
            relative_path="/images/banners/$filename"
            
            if ! grep -q "$relative_path" /tmp/db_banner_urls.txt 2>/dev/null; then
                rm -f "$filepath"
                DELETED_COUNT=$((DELETED_COUNT + 1))
            fi
        done < <(find "$BANNER_DIR" -type f \( -name "banner_*" -o -name "banner_mobile_*" \))
        
        echo "Deleted $DELETED_COUNT orphan files"
        echo "Cleanup completed!"
    else
        echo "Cleanup cancelled."
    fi
else
    echo "No orphan files found. Nothing to clean up!"
fi

# Cleanup temp files
rm -f /tmp/get_banner_urls.sql /tmp/db_banner_urls.txt

echo ""
echo "Done!"

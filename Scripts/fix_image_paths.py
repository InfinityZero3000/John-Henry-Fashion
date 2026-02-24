#!/usr/bin/env python3
"""
Scans wwwroot/images subfolders, builds SKU -> image path mapping from ACTUAL files,
then generates and runs SQL UPDATE statements to fix FeaturedImageUrl in the DB.

Run from project root:
    python3 Scripts/fix_image_paths.py
"""

import os
import re
import subprocess

# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------
IMAGES_ROOT   = "wwwroot/images"
PLACEHOLDER   = "/images/placeholder.jpg"

# Map each image subfolder to an image path prefix
FOLDERS = [
    "ao-nam",
    "ao-nu",
    "quan-nam",
    "quan-nu",
    "dam-nu",
    "chan-vay-nu",
    "phu-kien-nam",
    "phu-kien-nu",
    "Uniform",
]

# ---------------------------------------------------------------------------
# Step 1: Build SKU → web path lookup from all files on disk
# ---------------------------------------------------------------------------
# Primary:  exact match  sku.jpg        → /images/{folder}/{sku}.jpg
# file format: {SKU}.jpg  (stem == SKU, no extra suffix)
sku_to_path: dict[str, str] = {}

for folder in FOLDERS:
    folder_path = os.path.join(IMAGES_ROOT, folder)
    if not os.path.isdir(folder_path):
        continue
    for fname in sorted(os.listdir(folder_path)):
        if not fname.lower().endswith(".jpg"):
            continue
        sku = os.path.splitext(fname)[0]          # strip .jpg
        web_path = f"/images/{folder}/{fname}"
        # First hit wins (a SKU should only exist in ONE folder)
        if sku not in sku_to_path:
            sku_to_path[sku] = web_path

print(f"✅ Scanned {len(sku_to_path)} distinct SKU image files across {len(FOLDERS)} folders")

# ---------------------------------------------------------------------------
# Step 2: Query the DB for all products and their current FeaturedImageUrl
# ---------------------------------------------------------------------------
# Connect to the Postgres container
PGHOST   = "localhost"
PGPORT   = "5433"
PGDB     = "johnhenry_db"
PGUSER   = "johnhenry_user"
PGPASS   = "JohnHenry@2025!"

def psql(sql: str) -> str:
    env = os.environ.copy()
    env["PGPASSWORD"] = PGPASS
    result = subprocess.run(
        ["psql", "-h", PGHOST, "-p", PGPORT, "-U", PGUSER, "-d", PGDB,
         "-t", "-A", "-F", "|", "-c", sql],
        capture_output=True, text=True, env=env
    )
    if result.returncode != 0:
        raise RuntimeError(f"psql error: {result.stderr.strip()}")
    return result.stdout.strip()

print("\n📊 Querying products from database...")
rows = psql('SELECT "Id", "SKU", "FeaturedImageUrl" FROM "Products" WHERE "IsActive" = true')

if not rows:
    print("⚠️  No active products found.")
    exit(0)

products = []
for line in rows.splitlines():
    parts = line.split("|")
    if len(parts) >= 2:
        pid  = parts[0].strip()
        sku  = parts[1].strip()
        curr = parts[2].strip() if len(parts) > 2 else ""
        products.append((pid, sku, curr))

print(f"   Found {len(products)} active products")

# ---------------------------------------------------------------------------
# Step 3: For each product find the correct image path
# ---------------------------------------------------------------------------
updates: list[tuple[str, str, str]] = []   # (id, sku, new_path)
no_match: list[str] = []

for pid, sku, curr_path in products:
    # Exact match
    if sku in sku_to_path:
        new_path = sku_to_path[sku]
        if new_path != curr_path:
            updates.append((pid, sku, new_path))
    else:
        no_match.append(sku)

print(f"\n📝 Results:")
print(f"   ✅ SKUs with exact image match : {len(products) - len(no_match)}")
print(f"   ❌ SKUs with no matching image : {len(no_match)}")
print(f"   🔄 Updates needed (path wrong) : {len(updates)}")

if no_match:
    print(f"\n⚠️  Sample SKUs with no image file ({min(10,len(no_match))}/{len(no_match)}):")
    for s in no_match[:10]:
        print(f"   - {s}")

# ---------------------------------------------------------------------------
# Step 4: Apply SQL UPDATEs
# ---------------------------------------------------------------------------
if not updates:
    print("\n✅ All image paths are already correct — nothing to update.")
else:
    print(f"\n⏳ Applying {len(updates)} UPDATE statements...")
    batch_sql = "BEGIN;\n"
    for pid, sku, new_path in updates:
        safe_path = new_path.replace("'", "''")
        batch_sql += f'UPDATE "Products" SET "FeaturedImageUrl" = \'{safe_path}\' WHERE "Id" = {pid};\n'
    batch_sql += "COMMIT;"

    env = os.environ.copy()
    env["PGPASSWORD"] = PGPASS
    result = subprocess.run(
        ["psql", "-h", PGHOST, "-p", PGPORT, "-U", PGUSER, "-d", PGDB, "-c", batch_sql],
        capture_output=True, text=True, env=env
    )
    if result.returncode != 0:
        print(f"❌ SQL error: {result.stderr.strip()}")
    else:
        print("✅ Image paths updated successfully!")

    # Verify
    after = psql('SELECT COUNT(*) FROM "Products" WHERE "FeaturedImageUrl" LIKE \'/images/%\'')
    print(f"   Products with valid image paths: {after}")

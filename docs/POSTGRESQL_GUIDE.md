# 📘 HƯỚNG DẪN SỬ DỤNG POSTGRESQL

## 📋 Mục Lục

1. [Giới thiệu PostgreSQL](#giới-thiệu-postgresql)
2. [Cài đặt PostgreSQL](#cài-đặt-postgresql)
3. [Kết nối Database](#kết-nối-database)
4. [Các lệnh cơ bản](#các-lệnh-cơ-bản)
5. [Quản lý Database](#quản-lý-database)
6. [Quản lý Tables](#quản-lý-tables)
7. [Truy vấn dữ liệu](#truy-vấn-dữ-liệu)
8. [Backup & Restore](#backup--restore)
9. [Performance & Optimization](#performance--optimization)
10. [Troubleshooting](#troubleshooting)

---

## 🎯 Giới thiệu PostgreSQL

PostgreSQL là hệ quản trị cơ sở dữ liệu quan hệ mã nguồn mở (RDBMS) mạnh mẽ, hỗ trợ:
- ✅ ACID compliance (Atomicity, Consistency, Isolation, Durability)
- ✅ Hỗ trợ JSON/JSONB
- ✅ Full-text search
- ✅ Transactions phức tạp
- ✅ Triggers, Functions, Stored Procedures
- ✅ Foreign Keys, Constraints
- ✅ Indexing cao cấp

**Dự án John Henry Fashion sử dụng:**
- PostgreSQL 15+
- ASP.NET Core 9.0
- Entity Framework Core với Npgsql

---

## 💻 Cài đặt PostgreSQL

### macOS

```bash
# Sử dụng Homebrew
brew install postgresql@15

# Khởi động service
brew services start postgresql@15

# Kiểm tra version
psql --version
```

### Windows

1. Tải installer từ: https://www.postgresql.org/download/windows/
2. Chạy installer và làm theo hướng dẫn
3. Nhớ password cho user `postgres`
4. Thêm PostgreSQL vào PATH

```cmd
# Kiểm tra cài đặt
psql --version
```

### Linux (Ubuntu/Debian)

```bash
# Cập nhật package list
sudo apt update

# Cài đặt PostgreSQL
sudo apt install postgresql postgresql-contrib

# Khởi động service
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Kiểm tra trạng thái
sudo systemctl status postgresql
```

### Docker (Recommended cho Development)

```bash
# Pull PostgreSQL image
docker pull postgres:15

# Chạy container
docker run --name johnhenry-postgres \
  -e POSTGRES_PASSWORD=your_password \
  -e POSTGRES_DB=johnhenry_db \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  -d postgres:15

# Kết nối vào container
docker exec -it johnhenry-postgres psql -U postgres
```

---

## 🔌 Kết nối Database

### 1. Kết nối qua Terminal (psql)

```bash
# Kết nối với user postgres (default)
psql -U postgres

# Kết nối với database cụ thể
psql -U postgres -d johnhenry_db

# Kết nối với host và port cụ thể
psql -h localhost -p 5432 -U postgres -d johnhenry_db

# Kết nối với connection string
psql "postgresql://postgres:password@localhost:5432/johnhenry_db"
```

### 2. Kết nối từ ASP.NET Core

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=johnhenry_db;Username=postgres;Password=your_password"
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### 3. Kết nối từ GUI Tools

**pgAdmin 4** (Official GUI)
- Download: https://www.pgadmin.org/
- Tạo Server connection mới
- Nhập thông tin: Host, Port, Database, Username, Password

**DBeaver** (Universal Database Tool)
- Download: https://dbeaver.io/
- New Connection → PostgreSQL
- Cấu hình connection parameters

**DataGrip** (JetBrains)
- Professional database IDE
- Hỗ trợ code completion và refactoring

---

## 🛠️ Các lệnh cơ bản

### Lệnh Meta (bắt đầu bằng `\`)

```sql
-- Liệt kê tất cả databases
\l
\list

-- Kết nối đến database khác
\c database_name
\connect database_name

-- Liệt kê tất cả tables trong database hiện tại
\dt
\dt+                -- Với thông tin chi tiết

-- Liệt kê tất cả schemas
\dn

-- Mô tả cấu trúc của table
\d table_name
\d+ table_name      -- Với thông tin chi tiết

-- Liệt kê tất cả views
\dv

-- Liệt kê tất cả functions
\df

-- Liệt kê tất cả users/roles
\du

-- Liệt kê tất cả indexes
\di

-- Xem lịch sử commands
\s

-- Thực thi SQL file
\i /path/to/file.sql

-- Xuất kết quả ra file
\o output.txt
SELECT * FROM users;
\o  -- Tắt output file

-- Bật/tắt timing
\timing

-- Xem các settings hiện tại
\set

-- Clear screen
\! clear           -- macOS/Linux
\! cls             -- Windows

-- Thoát psql
\q
quit
exit
```

### Lệnh SQL cơ bản

```sql
-- Xem database hiện tại
SELECT current_database();

-- Xem user hiện tại
SELECT current_user;

-- Xem version PostgreSQL
SELECT version();

-- Xem thời gian hiện tại
SELECT NOW();

-- Liệt kê tất cả tables (SQL)
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public';

-- Xem kích thước database
SELECT pg_size_pretty(pg_database_size('johnhenry_db'));

-- Xem kích thước table
SELECT pg_size_pretty(pg_total_relation_size('users'));

-- Đếm số records trong table
SELECT COUNT(*) FROM users;
```

---

## 🗄️ Quản lý Database

### Tạo Database

```sql
-- Tạo database đơn giản
CREATE DATABASE johnhenry_db;

-- Tạo với options
CREATE DATABASE johnhenry_db
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TEMPLATE = template0
    CONNECTION LIMIT = -1;

-- Tạo với comment
CREATE DATABASE johnhenry_db;
COMMENT ON DATABASE johnhenry_db IS 'John Henry Fashion E-Commerce Database';
```

### Xóa Database

```sql
-- Ngắt tất cả connections trước
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'johnhenry_db' AND pid <> pg_backend_pid();

-- Xóa database
DROP DATABASE johnhenry_db;

-- Xóa nếu tồn tại
DROP DATABASE IF EXISTS johnhenry_db;
```

### Đổi tên Database

```sql
ALTER DATABASE johnhenry_db RENAME TO johnhenry_production;
```

### Quản lý Users/Roles

```sql
-- Tạo user mới
CREATE USER app_user WITH PASSWORD 'secure_password';

-- Tạo role
CREATE ROLE readonly_role;

-- Gán quyền cho user
GRANT CONNECT ON DATABASE johnhenry_db TO app_user;
GRANT USAGE ON SCHEMA public TO app_user;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO app_user;

-- Gán quyền đầy đủ
GRANT ALL PRIVILEGES ON DATABASE johnhenry_db TO app_user;

-- Xem quyền của user
\du app_user

-- Xóa user
DROP USER app_user;

-- Đổi password
ALTER USER postgres WITH PASSWORD 'new_password';
```

### Schemas

```sql
-- Tạo schema
CREATE SCHEMA IF NOT EXISTS app_schema;

-- Set default schema
SET search_path TO app_schema, public;

-- Liệt kê schemas
SELECT schema_name 
FROM information_schema.schemata;

-- Xóa schema
DROP SCHEMA app_schema CASCADE;
```

---

## 📊 Quản lý Tables

### Tạo Table

```sql
-- Table đơn giản
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Table với constraints phức tạp
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    order_number VARCHAR(20) UNIQUE NOT NULL,
    total_amount DECIMAL(10,2) NOT NULL CHECK (total_amount >= 0),
    status VARCHAR(20) DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT valid_status CHECK (status IN ('pending', 'confirmed', 'shipped', 'delivered', 'cancelled'))
);

-- Table với JSON column
CREATE TABLE product_metadata (
    id SERIAL PRIMARY KEY,
    product_id INTEGER NOT NULL,
    metadata JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Xem cấu trúc Table

```sql
-- Sử dụng meta command
\d users
\d+ users

-- Sử dụng SQL
SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'users'
ORDER BY ordinal_position;
```

### Sửa đổi Table (ALTER)

```sql
-- Thêm column
ALTER TABLE users ADD COLUMN phone VARCHAR(20);
ALTER TABLE users ADD COLUMN is_active BOOLEAN DEFAULT true;

-- Xóa column
ALTER TABLE users DROP COLUMN phone;

-- Đổi tên column
ALTER TABLE users RENAME COLUMN username TO user_name;

-- Thay đổi data type
ALTER TABLE users ALTER COLUMN phone TYPE VARCHAR(30);

-- Thêm constraint
ALTER TABLE users ADD CONSTRAINT email_format CHECK (email LIKE '%@%');
ALTER TABLE users ADD CONSTRAINT unique_email UNIQUE (email);

-- Xóa constraint
ALTER TABLE users DROP CONSTRAINT email_format;

-- Set default value
ALTER TABLE users ALTER COLUMN is_active SET DEFAULT true;

-- Remove default value
ALTER TABLE users ALTER COLUMN is_active DROP DEFAULT;

-- Set NOT NULL
ALTER TABLE users ALTER COLUMN email SET NOT NULL;

-- Remove NOT NULL
ALTER TABLE users ALTER COLUMN phone DROP NOT NULL;
```

### Xóa Table

```sql
-- Xóa table
DROP TABLE users;

-- Xóa nếu tồn tại
DROP TABLE IF EXISTS users;

-- Xóa nhiều tables cùng lúc
DROP TABLE IF EXISTS users, orders, products CASCADE;
```

### Indexes

```sql
-- Tạo index
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_products_name ON products(name);

-- Unique index
CREATE UNIQUE INDEX idx_users_username ON users(username);

-- Composite index
CREATE INDEX idx_orders_user_status ON orders(user_id, status);

-- Partial index (điều kiện)
CREATE INDEX idx_active_users ON users(email) WHERE is_active = true;

-- Index cho text search
CREATE INDEX idx_products_name_gin ON products USING GIN(to_tsvector('english', name));

-- Index cho JSONB
CREATE INDEX idx_metadata_gin ON product_metadata USING GIN(metadata);

-- Xem tất cả indexes của table
\di+ users

SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'users';

-- Xóa index
DROP INDEX idx_users_email;

-- Rebuild index
REINDEX INDEX idx_users_email;
REINDEX TABLE users;
```

---

## 🔍 Truy vấn dữ liệu

### SELECT cơ bản

```sql
-- Select tất cả
SELECT * FROM users;

-- Select columns cụ thể
SELECT id, username, email FROM users;

-- Với điều kiện WHERE
SELECT * FROM users WHERE is_active = true;
SELECT * FROM users WHERE created_at > '2025-01-01';

-- LIKE pattern matching
SELECT * FROM users WHERE email LIKE '%@gmail.com';
SELECT * FROM users WHERE username ILIKE 'john%';  -- Case-insensitive

-- IN clause
SELECT * FROM orders WHERE status IN ('pending', 'confirmed');

-- BETWEEN
SELECT * FROM products WHERE price BETWEEN 100000 AND 500000;

-- IS NULL / IS NOT NULL
SELECT * FROM users WHERE phone IS NULL;
SELECT * FROM users WHERE phone IS NOT NULL;

-- ORDER BY
SELECT * FROM products ORDER BY price DESC;
SELECT * FROM users ORDER BY created_at DESC, username ASC;

-- LIMIT và OFFSET (pagination)
SELECT * FROM products LIMIT 10 OFFSET 0;     -- Page 1
SELECT * FROM products LIMIT 10 OFFSET 10;    -- Page 2

-- DISTINCT
SELECT DISTINCT status FROM orders;
SELECT DISTINCT user_id FROM orders;
```

### Aggregate Functions

```sql
-- COUNT
SELECT COUNT(*) FROM users;
SELECT COUNT(DISTINCT user_id) FROM orders;

-- SUM
SELECT SUM(total_amount) FROM orders;
SELECT SUM(total_amount) FROM orders WHERE status = 'delivered';

-- AVG
SELECT AVG(price) FROM products;
SELECT AVG(total_amount) FROM orders;

-- MIN / MAX
SELECT MIN(price), MAX(price) FROM products;
SELECT MIN(created_at), MAX(created_at) FROM orders;

-- GROUP BY
SELECT status, COUNT(*) as count 
FROM orders 
GROUP BY status;

SELECT user_id, COUNT(*) as order_count, SUM(total_amount) as total_spent
FROM orders
GROUP BY user_id
ORDER BY total_spent DESC;

-- HAVING (filter sau khi GROUP BY)
SELECT user_id, COUNT(*) as order_count
FROM orders
GROUP BY user_id
HAVING COUNT(*) > 5;
```

### JOIN Operations

```sql
-- INNER JOIN
SELECT u.username, o.order_number, o.total_amount
FROM users u
INNER JOIN orders o ON u.id = o.user_id;

-- LEFT JOIN (lấy tất cả users kể cả không có order)
SELECT u.username, COUNT(o.id) as order_count
FROM users u
LEFT JOIN orders o ON u.id = o.user_id
GROUP BY u.id, u.username;

-- RIGHT JOIN
SELECT u.username, o.order_number
FROM users u
RIGHT JOIN orders o ON u.id = o.user_id;

-- FULL OUTER JOIN
SELECT u.username, o.order_number
FROM users u
FULL OUTER JOIN orders o ON u.id = o.user_id;

-- Multiple JOINs
SELECT 
    u.username,
    o.order_number,
    p.name as product_name,
    oi.quantity
FROM users u
INNER JOIN orders o ON u.id = o.user_id
INNER JOIN order_items oi ON o.id = oi.order_id
INNER JOIN products p ON oi.product_id = p.id;

-- Self JOIN
SELECT 
    e.name as employee,
    m.name as manager
FROM employees e
LEFT JOIN employees m ON e.manager_id = m.id;
```

### Subqueries

```sql
-- Subquery trong WHERE
SELECT * FROM products 
WHERE price > (SELECT AVG(price) FROM products);

-- Subquery trong FROM
SELECT avg_price.category, avg_price.average
FROM (
    SELECT category, AVG(price) as average
    FROM products
    GROUP BY category
) as avg_price
WHERE avg_price.average > 100000;

-- Subquery với IN
SELECT * FROM users
WHERE id IN (
    SELECT DISTINCT user_id 
    FROM orders 
    WHERE status = 'delivered'
);

-- EXISTS
SELECT * FROM users u
WHERE EXISTS (
    SELECT 1 FROM orders o 
    WHERE o.user_id = u.id 
    AND o.status = 'delivered'
);
```

### Common Table Expressions (CTE)

```sql
-- CTE cơ bản
WITH active_users AS (
    SELECT * FROM users WHERE is_active = true
)
SELECT au.username, COUNT(o.id) as order_count
FROM active_users au
LEFT JOIN orders o ON au.id = o.user_id
GROUP BY au.username;

-- Multiple CTEs
WITH 
    total_orders AS (
        SELECT user_id, COUNT(*) as order_count
        FROM orders
        GROUP BY user_id
    ),
    total_spent AS (
        SELECT user_id, SUM(total_amount) as total_amount
        FROM orders
        WHERE status = 'delivered'
        GROUP BY user_id
    )
SELECT 
    u.username,
    COALESCE(to.order_count, 0) as orders,
    COALESCE(ts.total_amount, 0) as spent
FROM users u
LEFT JOIN total_orders to ON u.id = to.user_id
LEFT JOIN total_spent ts ON u.id = ts.user_id;

-- Recursive CTE (ví dụ: category tree)
WITH RECURSIVE category_tree AS (
    -- Base case
    SELECT id, name, parent_id, 0 as level
    FROM categories
    WHERE parent_id IS NULL
    
    UNION ALL
    
    -- Recursive case
    SELECT c.id, c.name, c.parent_id, ct.level + 1
    FROM categories c
    INNER JOIN category_tree ct ON c.parent_id = ct.id
)
SELECT * FROM category_tree ORDER BY level, name;
```

### Window Functions

```sql
-- ROW_NUMBER (đánh số thứ tự)
SELECT 
    username,
    email,
    ROW_NUMBER() OVER (ORDER BY created_at) as row_num
FROM users;

-- RANK (xếp hạng với gaps)
SELECT 
    name,
    price,
    RANK() OVER (ORDER BY price DESC) as price_rank
FROM products;

-- DENSE_RANK (xếp hạng không gaps)
SELECT 
    name,
    price,
    DENSE_RANK() OVER (ORDER BY price DESC) as price_rank
FROM products;

-- PARTITION BY
SELECT 
    category,
    name,
    price,
    RANK() OVER (PARTITION BY category ORDER BY price DESC) as rank_in_category
FROM products;

-- LAG / LEAD (giá trị trước/sau)
SELECT 
    date,
    revenue,
    LAG(revenue) OVER (ORDER BY date) as previous_day,
    LEAD(revenue) OVER (ORDER BY date) as next_day
FROM daily_sales;

-- SUM OVER (running total)
SELECT 
    date,
    revenue,
    SUM(revenue) OVER (ORDER BY date) as cumulative_revenue
FROM daily_sales;
```

---

## 📝 INSERT, UPDATE, DELETE

### INSERT

```sql
-- Insert 1 record
INSERT INTO users (username, email, password_hash)
VALUES ('john_doe', 'john@example.com', 'hashed_password');

-- Insert nhiều records
INSERT INTO users (username, email, password_hash) VALUES
    ('jane_doe', 'jane@example.com', 'hash1'),
    ('bob_smith', 'bob@example.com', 'hash2'),
    ('alice_wong', 'alice@example.com', 'hash3');

-- Insert và return data
INSERT INTO users (username, email, password_hash)
VALUES ('new_user', 'new@example.com', 'hash')
RETURNING id, username, created_at;

-- Insert from SELECT
INSERT INTO archived_orders
SELECT * FROM orders WHERE created_at < '2024-01-01';

-- ON CONFLICT (Upsert)
INSERT INTO products (sku, name, price)
VALUES ('SKU001', 'Product Name', 100000)
ON CONFLICT (sku) 
DO UPDATE SET 
    name = EXCLUDED.name,
    price = EXCLUDED.price,
    updated_at = CURRENT_TIMESTAMP;
```

### UPDATE

```sql
-- Update 1 field
UPDATE users SET is_active = false WHERE id = 1;

-- Update nhiều fields
UPDATE users 
SET 
    email = 'newemail@example.com',
    updated_at = CURRENT_TIMESTAMP
WHERE id = 1;

-- Update với condition phức tạp
UPDATE products 
SET price = price * 1.1
WHERE category = 'Electronics' AND stock > 0;

-- Update với subquery
UPDATE products
SET category_name = (
    SELECT name FROM categories 
    WHERE categories.id = products.category_id
);

-- Update và return
UPDATE users 
SET is_active = true 
WHERE id = 1
RETURNING id, username, is_active;

-- Update từ JOIN
UPDATE products p
SET stock = stock - oi.quantity
FROM order_items oi
WHERE p.id = oi.product_id AND oi.order_id = 123;
```

### DELETE

```sql
-- Delete với điều kiện
DELETE FROM users WHERE is_active = false;

-- Delete tất cả (cẩn thận!)
DELETE FROM temp_table;

-- Delete với subquery
DELETE FROM orders 
WHERE user_id IN (
    SELECT id FROM users WHERE is_active = false
);

-- Delete và return
DELETE FROM users 
WHERE id = 1
RETURNING id, username, email;

-- TRUNCATE (nhanh hơn DELETE, reset auto-increment)
TRUNCATE TABLE temp_table;
TRUNCATE TABLE temp_table RESTART IDENTITY CASCADE;
```

---

## 🔧 Functions và Triggers

### Functions

```sql
-- Function đơn giản
CREATE OR REPLACE FUNCTION get_user_count()
RETURNS INTEGER AS $$
BEGIN
    RETURN (SELECT COUNT(*) FROM users);
END;
$$ LANGUAGE plpgsql;

-- Sử dụng function
SELECT get_user_count();

-- Function với parameters
CREATE OR REPLACE FUNCTION get_orders_by_status(order_status VARCHAR)
RETURNS TABLE (
    order_id INTEGER,
    order_number VARCHAR,
    total_amount DECIMAL
) AS $$
BEGIN
    RETURN QUERY
    SELECT id, order_number, total_amount
    FROM orders
    WHERE status = order_status;
END;
$$ LANGUAGE plpgsql;

-- Sử dụng
SELECT * FROM get_orders_by_status('pending');

-- Function update timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

### Triggers

```sql
-- Tạo trigger để tự động update updated_at
CREATE TRIGGER update_users_timestamp
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger để validate data
CREATE OR REPLACE FUNCTION validate_email()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.email NOT LIKE '%@%' THEN
        RAISE EXCEPTION 'Invalid email format';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER check_email_format
    BEFORE INSERT OR UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION validate_email();

-- Trigger để log changes
CREATE TABLE audit_log (
    id SERIAL PRIMARY KEY,
    table_name VARCHAR(50),
    action VARCHAR(10),
    old_data JSONB,
    new_data JSONB,
    changed_by VARCHAR(50),
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE OR REPLACE FUNCTION log_changes()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO audit_log (table_name, action, old_data, changed_by)
        VALUES (TG_TABLE_NAME, 'DELETE', row_to_json(OLD), current_user);
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit_log (table_name, action, old_data, new_data, changed_by)
        VALUES (TG_TABLE_NAME, 'UPDATE', row_to_json(OLD), row_to_json(NEW), current_user);
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO audit_log (table_name, action, new_data, changed_by)
        VALUES (TG_TABLE_NAME, 'INSERT', row_to_json(NEW), current_user);
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_users
    AFTER INSERT OR UPDATE OR DELETE ON users
    FOR EACH ROW
    EXECUTE FUNCTION log_changes();

-- Xem tất cả triggers
SELECT trigger_name, event_manipulation, event_object_table
FROM information_schema.triggers
WHERE trigger_schema = 'public';

-- Drop trigger
DROP TRIGGER IF EXISTS update_users_timestamp ON users;
```

---

## 💾 Backup & Restore

### Backup

```bash
# Backup toàn bộ database
pg_dump -U postgres -d johnhenry_db -F c -f backup.dump

# Backup dạng SQL plain text
pg_dump -U postgres -d johnhenry_db -f backup.sql

# Backup với compression
pg_dump -U postgres -d johnhenry_db -F c -Z 9 -f backup.dump.gz

# Backup chỉ schema (không có data)
pg_dump -U postgres -d johnhenry_db --schema-only -f schema.sql

# Backup chỉ data
pg_dump -U postgres -d johnhenry_db --data-only -f data.sql

# Backup specific tables
pg_dump -U postgres -d johnhenry_db -t users -t orders -f tables_backup.sql

# Backup với timestamp
pg_dump -U postgres -d johnhenry_db -F c -f "backup_$(date +%Y%m%d_%H%M%S).dump"

# Backup tất cả databases
pg_dumpall -U postgres -f all_databases.sql
```

### Restore

```bash
# Restore từ dump file
pg_restore -U postgres -d johnhenry_db -c backup.dump

# Restore từ SQL file
psql -U postgres -d johnhenry_db -f backup.sql

# Restore với clean (drop existing objects)
pg_restore -U postgres -d johnhenry_db -c -C backup.dump

# Restore chỉ schema
pg_restore -U postgres -d johnhenry_db --schema-only backup.dump

# Restore chỉ data
pg_restore -U postgres -d johnhenry_db --data-only backup.dump

# Restore specific table
pg_restore -U postgres -d johnhenry_db -t users backup.dump

# Restore với số jobs song song (nhanh hơn)
pg_restore -U postgres -d johnhenry_db -j 4 backup.dump
```

### Automated Backup Script

```bash
#!/bin/bash
# backup_postgres.sh

DB_NAME="johnhenry_db"
DB_USER="postgres"
BACKUP_DIR="/backups/postgres"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/${DB_NAME}_${DATE}.dump"

# Tạo backup directory nếu chưa có
mkdir -p $BACKUP_DIR

# Backup database
pg_dump -U $DB_USER -d $DB_NAME -F c -f $BACKUP_FILE

# Compress backup
gzip $BACKUP_FILE

# Xóa backup cũ hơn 30 ngày
find $BACKUP_DIR -name "*.dump.gz" -mtime +30 -delete

echo "Backup completed: ${BACKUP_FILE}.gz"
```

### Cron Job cho Auto Backup

```bash
# Edit crontab
crontab -e

# Backup hàng ngày lúc 2:00 AM
0 2 * * * /path/to/backup_postgres.sh >> /var/log/postgres_backup.log 2>&1

# Backup mỗi 6 giờ
0 */6 * * * /path/to/backup_postgres.sh >> /var/log/postgres_backup.log 2>&1
```

---

## ⚡ Performance & Optimization

### EXPLAIN và EXPLAIN ANALYZE

```sql
-- Xem query plan
EXPLAIN SELECT * FROM users WHERE email = 'test@example.com';

-- Xem query plan với execution time
EXPLAIN ANALYZE SELECT * FROM users WHERE email = 'test@example.com';

-- Format dễ đọc
EXPLAIN (FORMAT JSON) SELECT * FROM orders WHERE status = 'pending';

-- Với chi tiết
EXPLAIN (ANALYZE, BUFFERS, VERBOSE) 
SELECT u.username, COUNT(o.id)
FROM users u
LEFT JOIN orders o ON u.id = o.user_id
GROUP BY u.username;
```

### Analyzing Tables

```sql
-- Analyze 1 table
ANALYZE users;

-- Analyze tất cả tables
ANALYZE;

-- Vacuum (cleanup dead rows)
VACUUM users;

-- Vacuum với analyze
VACUUM ANALYZE users;

-- Full vacuum (slower nhưng hiệu quả hơn)
VACUUM FULL users;

-- Auto vacuum settings
SHOW autovacuum;
```

### Query Optimization Tips

```sql
-- ❌ BAD: Select tất cả columns
SELECT * FROM users;

-- ✅ GOOD: Select chỉ những columns cần thiết
SELECT id, username, email FROM users;

-- ❌ BAD: Không có WHERE trong UPDATE/DELETE
UPDATE products SET stock = 0;

-- ✅ GOOD: Luôn có WHERE
UPDATE products SET stock = 0 WHERE stock < 0;

-- ❌ BAD: Function trong WHERE (không dùng được index)
SELECT * FROM users WHERE UPPER(email) = 'TEST@EXAMPLE.COM';

-- ✅ GOOD: So sánh trực tiếp
SELECT * FROM users WHERE email = LOWER('TEST@EXAMPLE.COM');

-- ❌ BAD: OR với nhiều điều kiện
SELECT * FROM products WHERE category = 'A' OR category = 'B' OR category = 'C';

-- ✅ GOOD: Dùng IN
SELECT * FROM products WHERE category IN ('A', 'B', 'C');

-- ❌ BAD: NOT IN với subquery lớn
SELECT * FROM users WHERE id NOT IN (SELECT user_id FROM orders);

-- ✅ GOOD: LEFT JOIN với NULL check
SELECT u.* FROM users u
LEFT JOIN orders o ON u.id = o.user_id
WHERE o.user_id IS NULL;
```

### Indexes Strategy

```sql
-- Index cho foreign keys
CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_order_items_product_id ON order_items(product_id);

-- Index cho columns thường dùng trong WHERE
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_products_category ON products(category);

-- Composite index cho queries phức tạp
CREATE INDEX idx_orders_user_status ON orders(user_id, status);
CREATE INDEX idx_products_category_price ON products(category, price);

-- Partial index cho data subset
CREATE INDEX idx_active_users ON users(email) WHERE is_active = true;
CREATE INDEX idx_pending_orders ON orders(created_at) WHERE status = 'pending';

-- Xem index usage
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
ORDER BY idx_scan DESC;

-- Tìm indexes không được sử dụng
SELECT 
    schemaname,
    tablename,
    indexname
FROM pg_stat_user_indexes
WHERE idx_scan = 0 
AND indexname NOT LIKE '%_pkey'
ORDER BY schemaname, tablename;
```

### Connection Pooling

```sql
-- Xem current connections
SELECT count(*) FROM pg_stat_activity;

SELECT 
    datname,
    usename,
    application_name,
    client_addr,
    state,
    query
FROM pg_stat_activity
WHERE datname = 'johnhenry_db';

-- Kill connection
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'johnhenry_db' 
AND pid <> pg_backend_pid();

-- Xem max connections
SHOW max_connections;

-- Set max connections (trong postgresql.conf hoặc)
ALTER SYSTEM SET max_connections = 200;
SELECT pg_reload_conf();
```

### Database Statistics

```sql
-- Table statistics
SELECT 
    schemaname,
    tablename,
    n_live_tup as live_rows,
    n_dead_tup as dead_rows,
    last_vacuum,
    last_autovacuum,
    last_analyze
FROM pg_stat_user_tables
WHERE schemaname = 'public'
ORDER BY n_live_tup DESC;

-- Cache hit ratio (>90% là tốt)
SELECT 
    sum(heap_blks_read) as heap_read,
    sum(heap_blks_hit) as heap_hit,
    sum(heap_blks_hit) / (sum(heap_blks_hit) + sum(heap_blks_read)) as ratio
FROM pg_statio_user_tables;

-- Slow queries (cần enable pg_stat_statements extension)
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    max_time
FROM pg_stat_statements
ORDER BY mean_time DESC
LIMIT 10;
```

---

## 🔒 Security Best Practices

### Password & Authentication

```sql
-- Đổi password mạnh
ALTER USER postgres WITH PASSWORD 'Very$trong!P@ssw0rd#2025';

-- Tạo user với limited permissions
CREATE USER app_readonly WITH PASSWORD 'readonly_pass';
GRANT CONNECT ON DATABASE johnhenry_db TO app_readonly;
GRANT USAGE ON SCHEMA public TO app_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO app_readonly;

-- Revoke permissions
REVOKE ALL PRIVILEGES ON DATABASE johnhenry_db FROM app_user;
```

### Row Level Security (RLS)

```sql
-- Enable RLS
ALTER TABLE orders ENABLE ROW LEVEL SECURITY;

-- Policy: Users chỉ xem được orders của mình
CREATE POLICY user_orders_policy ON orders
    FOR SELECT
    USING (user_id = current_user_id());

-- Policy: Admin xem được tất cả
CREATE POLICY admin_orders_policy ON orders
    FOR ALL
    USING (current_user_is_admin());
```

### SSL Connection

```bash
# Kết nối với SSL
psql "postgresql://user:pass@host:5432/db?sslmode=require"

# Connection string với SSL
Host=localhost;Port=5432;Database=johnhenry_db;Username=postgres;Password=pass;SSL Mode=Require;Trust Server Certificate=true
```

---

## 🐛 Troubleshooting

### Common Issues

```sql
-- Kiểm tra locks
SELECT 
    pid,
    usename,
    pg_blocking_pids(pid) as blocked_by,
    query
FROM pg_stat_activity
WHERE cardinality(pg_blocking_pids(pid)) > 0;

-- Kill blocking query
SELECT pg_terminate_backend(12345);  -- Replace with actual PID

-- Kiểm tra table bloat
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename)) AS external_size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Kiểm tra replication lag (nếu có replica)
SELECT 
    client_addr,
    state,
    sent_lsn,
    write_lsn,
    flush_lsn,
    replay_lsn,
    sync_state
FROM pg_stat_replication;
```

### Logs

```bash
# Xem PostgreSQL logs (location varies)
# Ubuntu/Debian
tail -f /var/log/postgresql/postgresql-15-main.log

# macOS (Homebrew)
tail -f /usr/local/var/log/postgres.log

# Docker
docker logs -f johnhenry-postgres
```

### Performance Issues

```sql
-- Tìm long-running queries
SELECT 
    pid,
    now() - query_start as duration,
    query,
    state
FROM pg_stat_activity
WHERE state != 'idle'
AND now() - query_start > interval '5 minutes'
ORDER BY duration DESC;

-- Tìm tables cần VACUUM
SELECT 
    schemaname,
    tablename,
    n_dead_tup,
    n_live_tup,
    round(n_dead_tup * 100.0 / NULLIF(n_live_tup + n_dead_tup, 0), 2) AS dead_ratio
FROM pg_stat_user_tables
WHERE n_dead_tup > 1000
ORDER BY dead_ratio DESC;
```

---

## 📚 Resources & Learning

### Official Documentation
- PostgreSQL Docs: https://www.postgresql.org/docs/
- Npgsql (C# driver): https://www.npgsql.org/

### Tools
- **pgAdmin**: https://www.pgadmin.org/
- **DBeaver**: https://dbeaver.io/
- **DataGrip**: https://www.jetbrains.com/datagrip/

### Extensions
```sql
-- Xem available extensions
SELECT * FROM pg_available_extensions ORDER BY name;

-- Install extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";  -- Fuzzy text search
CREATE EXTENSION IF NOT EXISTS "hstore";   -- Key-value storage
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";  -- Query statistics
```

### Monitoring
```sql
-- Enable statistics
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Configure in postgresql.conf
shared_preload_libraries = 'pg_stat_statements'
pg_stat_statements.track = all
pg_stat_statements.max = 10000
```

---

## 🎓 Best Practices Summary

1. ✅ **Luôn dùng WHERE trong UPDATE/DELETE**
2. ✅ **Tạo indexes cho foreign keys và columns thường query**
3. ✅ **Dùng EXPLAIN ANALYZE để tối ưu queries**
4. ✅ **Backup thường xuyên và test restore**
5. ✅ **Sử dụng connection pooling**
6. ✅ **Monitor slow queries và long transactions**
7. ✅ **Vacuum và analyze tables định kỳ**
8. ✅ **Dùng transactions cho multi-step operations**
9. ✅ **Validate input data với constraints**
10. ✅ **Sử dụng prepared statements để tránh SQL injection**

---

## 📞 Support

Nếu gặp vấn đề với PostgreSQL trong dự án John Henry Fashion:

1. Kiểm tra logs: `tail -f /var/log/postgresql/*.log`
2. Xem database README: [DATABASE_MASTER_README.md](../database/DATABASE_MASTER_README.md)
3. Check connection string trong `appsettings.json`
4. Verify PostgreSQL service: `systemctl status postgresql`

---

**Ngày cập nhật:** 19/12/2025  
**Version:** PostgreSQL 15+  
**Author:** John Henry Fashion Development Team

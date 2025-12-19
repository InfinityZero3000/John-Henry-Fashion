# JOHN HENRY FASHION - DATABASE DOCUMENTATION

## 📚 Tổng Quan

Thư mục này chứa tất cả các file SQL và tài liệu liên quan đến cơ sở dữ liệu của hệ thống John Henry Fashion E-Commerce.

**Ngày cập nhật:** 19/12/2025  
**Database:** PostgreSQL 15  
**Framework:** ASP.NET Core 9.0 với Entity Framework Core

---

## 🗂️ Cấu Trúc Thư Mục

```
database/
├── master_schema.sql                          # ⭐ SCHEMA CHÍNH - Tất cả các bảng
├── master_functions_triggers_procedures.sql   # ⭐ FUNCTIONS, TRIGGERS, PROCEDURES
├── master_sample_data.sql                     # ⭐ DỮ LIỆU MẪU CHÍNH
│
├── docs/                                      # Tài liệu hướng dẫn
│   ├── DATABASE_README.md
│   ├── FUNCTIONS_PROCEDURES_GUIDE.md
│   ├── MIGRATIONS_GUIDE.md
│   └── BACKUP_RESTORE_GUIDE.md
│
├── backups/                                   # Các file backup
│   ├── backup_johnhenry_db_*.sql
│   └── local_data_export_*.sql
│
└── [legacy files]                            # Các file cũ (giữ để tham khảo)
    ├── database_schema.sql
    ├── triggers_functions_procedures.sql
    ├── insert_*.sql
    └── ...
```

---

## 🚀 Quick Start

### 1. Tạo Database Mới

```bash
# Kết nối PostgreSQL
psql -U postgres

# Tạo database
CREATE DATABASE johnhenry_db;

# Kết nối vào database
\c johnhenry_db
```

### 2. Import Schema (Bảng & Cấu Trúc)

```bash
psql -U postgres -d johnhenry_db -f master_schema.sql
```

**File này chứa:**
- ✅ 50+ bảng hệ thống
- ✅ Tất cả Foreign Keys và Constraints
- ✅ Indexes để tối ưu hiệu suất
- ✅ Comments và documentation

### 3. Import Functions & Triggers

```bash
psql -U postgres -d johnhenry_db -f master_functions_triggers_procedures.sql
```

**File này chứa:**
- ✅ 15+ Functions (tính toán, validation)
- ✅ 10+ Triggers (tự động cập nhật dữ liệu)
- ✅ 7+ Stored Procedures (xử lý nghiệp vụ)
- ✅ Views cho báo cáo

### 4. Import Dữ Liệu Mẫu (Tùy chọn)

```bash
psql -U postgres -d johnhenry_db -f master_sample_data.sql
```

**File này chứa:**
- ✅ Payment Methods & Shipping Methods
- ✅ 8 Sample Coupons
- ✅ 8 Blog Posts
- ✅ Marketing Banners
- ✅ System Configurations

### 5. Import Địa Chỉ Việt Nam

```bash
psql -U postgres -d johnhenry_db -f import_vietnam_addresses.sql
```

**Chứa:** 63 Tỉnh/Thành, 700+ Quận/Huyện, 10,000+ Phường/Xã

---

## 📊 Cấu Trúc Database

### Các Nhóm Bảng Chính

#### 1. **Core Product & Category** (5 bảng)
- `Categories` - Danh mục sản phẩm
- `Brands` - Thương hiệu
- `Products` - Sản phẩm
- `ProductImages` - Hình ảnh sản phẩm
- `ProductReviews` - Đánh giá sản phẩm

#### 2. **Order & Shopping Cart** (4 bảng)
- `Orders` - Đơn hàng
- `OrderItems` - Chi tiết đơn hàng
- `ShoppingCartItems` - Giỏ hàng
- `OrderStatusHistories` - Lịch sử trạng thái đơn hàng

#### 3. **Payment System** (4 bảng)
- `Payments` - Thanh toán
- `PaymentAttempts` - Lịch sử thanh toán
- `PaymentTransactions` - Giao dịch
- `PaymentMethods` - Phương thức thanh toán (data)

#### 4. **Checkout Process** (2 bảng)
- `CheckoutSessions` - Phiên checkout
- `CheckoutSessionItems` - Items trong checkout

#### 5. **Coupon & Promotion** (2 bảng)
- `Coupons` - Mã giảm giá
- `CouponUsages` - Lịch sử sử dụng coupon

#### 6. **Inventory Management** (2 bảng)
- `InventoryItems` - Tồn kho
- `StockMovements` - Di chuyển hàng

#### 7. **Blog System** (2 bảng)
- `BlogCategories` - Danh mục blog
- `BlogPosts` - Bài viết

#### 8. **User Interaction** (3 bảng)
- `Wishlists` - Danh sách yêu thích
- `Addresses` - Địa chỉ giao hàng
- `ContactMessages` - Tin nhắn liên hệ

#### 9. **Security & Audit** (4 bảng)
- `SecurityLogs` - Log bảo mật
- `PasswordHistories` - Lịch sử mật khẩu
- `ActiveSessions` - Phiên đăng nhập
- `AuditLogs` - Log kiểm toán

#### 10. **Analytics** (2 bảng)
- `UserSessions` - Phiên người dùng
- `PageViews` - Lượt xem trang

#### 11. **Seller & Marketplace** (3 bảng)
- `Stores` - Cửa hàng
- `SellerStores` - Liên kết seller-store
- `OrderRevenues` - Doanh thu đơn hàng

#### 12. **Marketing** (1 bảng)
- `MarketingBanners` - Banner quảng cáo

#### 13. **System Configuration** (2 bảng)
- `SystemConfigurations` - Cấu hình hệ thống
- `ShippingMethods` - Phương thức vận chuyển

#### 14. **Vietnamese Addresses** (3 bảng)
- `Provinces` - Tỉnh/Thành phố
- `Districts` - Quận/Huyện
- `Wards` - Phường/Xã

#### 15. **Notifications** (1 bảng)
- `Notifications` - Thông báo

**Tổng cộng: 50+ bảng**

---

## 🔧 Functions & Procedures Quan Trọng

### Functions Thường Dùng

```sql
-- Tính giá cuối sau coupon
SELECT get_product_final_price(
    'product-uuid'::UUID, 
    2, -- quantity
    'WELCOME2025' -- coupon code
);

-- Tính phí ship
SELECT calculate_shipping_cost(
    2.5, -- weight (kg)
    '79', -- province code (HCM)
    'standard' -- shipping method
);

-- Kiểm tra tồn kho
SELECT check_stock_availability(
    'product-uuid'::UUID,
    5 -- quantity
);

-- Kiểm tra trạng thái thanh toán
SELECT * FROM check_order_payment_status('order-uuid'::UUID);
```

### Procedures Quan Trọng

```sql
-- Seller xác nhận đơn hàng
CALL seller_confirm_order(
    'order-uuid'::UUID,
    'seller-id'
);

-- User xác nhận đã nhận hàng (tính revenue)
CALL process_user_delivery_confirmation(
    'order-uuid'::UUID,
    'user-id',
    10.00 -- commission rate
);

-- Admin xác nhận thanh toán
CALL confirm_payment(
    'order-uuid'::UUID,
    'admin-user-id',
    'Đã xác nhận chuyển khoản'
);

-- Dọn dẹp sessions cũ
CALL cleanup_expired_sessions();

-- Dọn dẹp coupons hết hạn
CALL cleanup_expired_coupons();
```

### Views Báo Cáo

```sql
-- Xem tình trạng thanh toán của orders
SELECT * FROM vw_orders_payment_status 
WHERE payment_status != 'paid';

-- Báo cáo doanh thu
SELECT * FROM vw_admin_revenue_report
WHERE revenue_date >= '2025-12-01';

-- Orders đang chờ xác nhận
SELECT * FROM vw_pending_confirmations
ORDER BY days_pending DESC;
```

---

## 🔄 Marketplace Flow

### Quy Trình Đơn Hàng Marketplace

```
1. User đặt hàng
   ↓
2. Seller xác nhận đơn hàng
   CALL seller_confirm_order(...)
   ↓
3. Đóng gói & Giao hàng
   UPDATE Orders SET Status = 'shipped'
   ↓
4. User xác nhận đã nhận hàng
   CALL process_user_delivery_confirmation(...)
   ↓
5. Hệ thống tự động tính revenue
   - Tạo record trong OrderRevenues
   - Tính commission cho platform
   - Tính earning cho seller
```

### Các Trường Quan Trọng

```sql
Orders:
  - IsSellerConfirmed (Seller đã xác nhận chưa)
  - IsUserConfirmedDelivery (User đã nhận hàng chưa)
  - IsRevenueCalculated (Đã tính doanh thu chưa)
  
OrderRevenues:
  - NetRevenue (Doanh thu thuần)
  - CommissionAmount (Hoa hồng platform)
  - SellerEarning (Tiền seller nhận được)
```

---

## 💾 Backup & Restore

### Backup Database

```bash
# Backup toàn bộ database
./backup_database.sh

# Hoặc thủ công
pg_dump -U postgres johnhenry_db > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Restore Database

```bash
# Restore từ file backup
psql -U postgres -d johnhenry_db < backup_20251219_120000.sql

# Hoặc dùng script
./restore_database.sh backup_20251219_120000.sql
```

**Xem thêm:** `docs/BACKUP_RESTORE_GUIDE.md`

---

## 📈 Migration & Updates

### Chạy Migration Mới

```bash
# ASP.NET Core Entity Framework
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Manual SQL Migration

```bash
# Tạo file migration mới trong database/
# Đặt tên: YYYYMMDD_description.sql

# Chạy migration
psql -U postgres -d johnhenry_db -f database/20251219_add_new_feature.sql
```

**Xem thêm:** `docs/MIGRATIONS_GUIDE.md`

---

## 🧪 Testing & Development

### Môi Trường Development

```bash
# Copy file cấu hình
cp appsettings.json appsettings.Development.json

# Cập nhật connection string
# ConnectionStrings.DefaultConnection = "Host=localhost;Database=johnhenry_dev;..."
```

### Import Data Mẫu Đầy Đủ

```bash
# 1. Schema
psql -U postgres -d johnhenry_dev -f master_schema.sql

# 2. Functions & Triggers
psql -U postgres -d johnhenry_dev -f master_functions_triggers_procedures.sql

# 3. Sample Data
psql -U postgres -d johnhenry_dev -f master_sample_data.sql

# 4. Địa chỉ VN
psql -U postgres -d johnhenry_dev -f import_vietnam_addresses.sql

# 5. Dashboard Data (optional)
psql -U postgres -d johnhenry_dev -f insert_sample_dashboard_data_v2.sql
```

---

## 📝 Các File Legacy (Tham Khảo)

Các file sau được giữ lại để tham khảo, nhưng nên dùng các file master:

| File Cũ | File Mới (Nên Dùng) |
|---------|---------------------|
| `database_schema.sql` | ✅ `master_schema.sql` |
| `triggers_functions_procedures.sql` | ✅ `master_functions_triggers_procedures.sql` |
| `insert_sample_coupons.sql` | ✅ `master_sample_data.sql` |
| `insert_8_blog_posts_final.sql` | ✅ `master_sample_data.sql` |
| `create_address_tables.sql` | ✅ `master_schema.sql` (đã tích hợp) |
| `add_marketplace_flow.sql` | ✅ `master_schema.sql` (đã tích hợp) |
| `create_payment_validation_functions.sql` | ✅ `master_functions_triggers_procedures.sql` |

---

## 🎯 Best Practices

### 1. Luôn Backup Trước Khi Thay Đổi

```bash
./backup_database.sh
```

### 2. Sử Dụng Transactions

```sql
BEGIN;
-- Your SQL commands here
COMMIT;
-- Hoặc ROLLBACK; nếu có lỗi
```

### 3. Test Trên Development Trước

- Chạy trên `johnhenry_dev` trước
- Kiểm tra kỹ
- Mới deploy lên production

### 4. Document Mọi Thay Đổi

- Thêm comment trong SQL
- Cập nhật README này
- Tạo migration file rõ ràng

---

## 🔐 Security Notes

### Permissions

```sql
-- Tạo user chỉ đọc
CREATE USER readonly_user WITH PASSWORD 'secure_password';
GRANT CONNECT ON DATABASE johnhenry_db TO readonly_user;
GRANT USAGE ON SCHEMA public TO readonly_user;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO readonly_user;

-- Tạo user cho app
CREATE USER app_user WITH PASSWORD 'secure_password';
GRANT CONNECT ON DATABASE johnhenry_db TO app_user;
GRANT USAGE ON SCHEMA public TO app_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO app_user;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO app_user;
```

### Connection String (Production)

```
Host=your-server.com;
Database=johnhenry_db;
Username=app_user;
Password=use-environment-variable;
SSL Mode=Require;
```

**⚠️ KHÔNG BAO GIỜ commit password vào Git!**

---

## 📞 Support & Contact

### Tài Liệu Bổ Sung

- [Functions & Procedures Guide](docs/FUNCTIONS_PROCEDURES_GUIDE.md)
- [Migrations Guide](docs/MIGRATIONS_GUIDE.md)
- [Backup & Restore Guide](docs/BACKUP_RESTORE_GUIDE.md)

### Issues & Questions

- 🐛 Bug Reports: Tạo issue trong repository
- 💡 Feature Requests: Thảo luận với team
- 📧 Email: dev@johnhenry.vn

---

## 📅 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0 | 2025-12-19 | Tổng hợp các file master, tổ chức lại cấu trúc |
| 1.5 | 2025-11-10 | Thêm marketplace flow, payment validation |
| 1.0 | 2025-10-24 | Schema ban đầu, 50+ bảng |

---

## ✅ Checklist Setup Mới

- [ ] Clone repository
- [ ] Tạo PostgreSQL database
- [ ] Import `master_schema.sql`
- [ ] Import `master_functions_triggers_procedures.sql`
- [ ] Import `master_sample_data.sql` (nếu cần)
- [ ] Import `import_vietnam_addresses.sql`
- [ ] Cấu hình connection string
- [ ] Test kết nối
- [ ] Chạy migrations (nếu có)
- [ ] Kiểm tra permissions
- [ ] Setup backup tự động

---

**🎉 Chúc bạn làm việc hiệu quả với John Henry Fashion Database!**

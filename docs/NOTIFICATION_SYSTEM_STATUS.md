# 📬 BÁO CÁO TÌNH TRẠNG HỆ THỐNG THÔNG BÁO

**Ngày kiểm tra:** 19/12/2025  
**Trang admin:** http://localhost:5101/admin/notifications

---

## ✅ CÁC THÔNG BÁO ĐÃ HOẠT ĐỘNG

### 1. **Đơn hàng mới** ✅
- **Nơi:** `CheckoutController` (dòng 817-835)
- **Khi nào:** Khi khách hàng đặt hàng thành công
- **Gửi đến:** 
  - ✅ Customer: "Đơn hàng đã được xác nhận"
  - ✅ Admin & Seller: "Có đơn hàng mới #{OrderNumber}"
- **Thông tin:** Số đơn hàng, tên khách hàng, tổng giá trị
- **Link:** `/seller/orders?orderNumber={OrderNumber}`

```csharp
// Gửi notification cho customer
await _notificationService.SendNotificationAsync(order.UserId,
    "Đơn hàng đã được xác nhận",
    $"Đơn hàng #{order.OrderNumber} đã được thanh toán và xác nhận thành công",
    "order_confirmed");

// Gửi notification cho admin và seller
foreach (var adminUser in notifyUsers)
{
    await _notificationService.SendNotificationAsync(adminUser.Id,
        "Đơn hàng mới",
        $"Có đơn hàng mới #{order.OrderNumber} từ khách hàng {customerName}",
        "new_order",
        $"/seller/orders?orderNumber={order.OrderNumber}");
}
```

### 2. **Yêu cầu hoàn tiền** ✅
- **Nơi:** `RefundController` (dòng 119, 272, 352)
- **Khi nào:** 
  - Khi khách hàng tạo yêu cầu hoàn tiền
  - Khi admin phê duyệt hoàn tiền
  - Khi admin từ chối hoàn tiền
- **Gửi đến:** Admin và Customer
- **Hoạt động:** ✅ Đầy đủ

### 3. **Liên hệ từ khách hàng** ✅ (MỚI THÊM)
- **Nơi:** `ContactController`
- **Khi nào:** Khi có người gửi form liên hệ
- **Gửi đến:** Admin
- **Thông tin:** Tên, email, chủ đề, nội dung
- **Link:** `/admin/support?ticketNumber={TicketNumber}`
- **Trạng thái:** ✅ **ĐÃ THÊM VÀO CODE**

```csharp
// Gửi in-app notification cho admin
var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
foreach (var admin in adminUsers)
{
    await _notificationService.CreateNotificationAsync(
        admin.Id,
        "Tin nhắn liên hệ mới",
        $"Có tin nhắn liên hệ mới từ {model.Name} ({model.Email}). Chủ đề: {model.Subject}",
        "contact",
        $"/admin/support?ticketNumber={ticketNumber}");
}
```

---

## ❌ CÁC THÔNG BÁO CHƯA CÓ (CẦN BỔ SUNG)

### 1. **Seller tạo sản phẩm mới** ❌
- **Nơi cần thêm:** `SellerProductsController.Create()` (dòng ~190)
- **Khi nào:** Khi seller tạo sản phẩm mới
- **Gửi đến:** Admin (để phê duyệt)
- **Thông tin:** Tên sản phẩm, seller, SKU
- **Link:** `/admin/products?search={SKU}`
- **Mức độ quan trọng:** 🔴 CAO

**Code cần thêm:**
```csharp
// Sau khi await _context.SaveChangesAsync(); (dòng ~188)

// Gửi notification cho admin
try
{
    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
    var sellerName = User.Identity?.Name ?? "Seller";
    
    foreach (var admin in adminUsers)
    {
        await _notificationService.CreateNotificationAsync(
            admin.Id,
            "Sản phẩm mới từ Seller",
            $"Seller {sellerName} đã tạo sản phẩm mới: {product.Name} (SKU: {product.SKU})",
            "new_product",
            $"/admin/products?search={product.SKU}");
    }
    _logger.LogInformation("Notifications sent to admins for new product {SKU}", product.SKU);
}
catch (Exception notifEx)
{
    _logger.LogError(notifEx, "Failed to send notifications for new product {SKU}", product.SKU);
}
```

### 2. **Review mới cần phê duyệt** ❌
- **Nơi cần thêm:** `ReviewController.SubmitReview()`
- **Khi nào:** Khi có review mới cần kiểm duyệt (không tự động approve)
- **Gửi đến:** Admin
- **Thông tin:** Tên sản phẩm, người review, rating
- **Link:** `/admin/approvals/review/{reviewId}`
- **Mức độ quan trọng:** 🟡 TRUNG BÌNH

**Code cần thêm:**
```csharp
// Trong ReviewController, sau khi tạo review
if (!review.IsApproved) // Chỉ gửi nếu cần phê duyệt
{
    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
    foreach (var admin in adminUsers)
    {
        await _notificationService.CreateNotificationAsync(
            admin.Id,
            "Review mới cần phê duyệt",
            $"Review mới cho sản phẩm {product.Name} từ {user.UserName}. Rating: {review.Rating}⭐",
            "review_pending",
            $"/admin/approvals/review/{review.Id}");
    }
}
```

### 3. **Sản phẩm được phê duyệt/từ chối** ❌
- **Nơi cần thêm:** `ProductApprovalController` (nếu có workflow phê duyệt sản phẩm)
- **Khi nào:** Khi admin phê duyệt hoặc từ chối sản phẩm của seller
- **Gửi đến:** Seller
- **Thông tin:** Tên sản phẩm, trạng thái, lý do (nếu từ chối)
- **Link:** `/seller/products/{productId}`
- **Mức độ quan trọng:** 🔴 CAO

**Code cần thêm:**
```csharp
// Khi admin approve sản phẩm
await _notificationService.CreateNotificationAsync(
    product.SellerId,
    "Sản phẩm đã được phê duyệt",
    $"Sản phẩm {product.Name} (SKU: {product.SKU}) đã được phê duyệt và hiển thị trên website",
    "product_approved",
    $"/seller/products/{product.Id}");

// Khi admin reject sản phẩm
await _notificationService.CreateNotificationAsync(
    product.SellerId,
    "Sản phẩm bị từ chối",
    $"Sản phẩm {product.Name} (SKU: {product.SKU}) bị từ chối. Lý do: {rejectionReason}",
    "product_rejected",
    $"/seller/products/{product.Id}");
```

### 4. **Review được phê duyệt** ❌
- **Nơi cần thêm:** `ProductApprovalController.ApproveReview()` (dòng ~155)
- **Khi nào:** Khi admin phê duyệt review
- **Gửi đến:** User đã viết review
- **Thông tin:** Tên sản phẩm đã được review
- **Link:** `/products/{productSlug}`
- **Mức độ quan trọng:** 🟢 THẤP

**Code cần thêm:**
```csharp
// Sau review.IsApproved = true;
try
{
    var product = await _context.Products.FindAsync(review.ProductId);
    if (product != null && review.UserId != null)
    {
        await _notificationService.CreateNotificationAsync(
            review.UserId,
            "Review của bạn đã được phê duyệt",
            $"Review của bạn cho sản phẩm {product.Name} đã được phê duyệt và hiển thị công khai",
            "review_approved",
            $"/products/{product.Slug}");
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to send notification for approved review {ReviewId}", id);
}
```

### 5. **Đơn hàng thay đổi trạng thái** ❌
- **Nơi cần thêm:** Controller xử lý cập nhật trạng thái đơn hàng
- **Khi nào:** Khi đơn hàng chuyển sang trạng thái mới (Đang xử lý, Đang giao, Đã giao, Đã hủy)
- **Gửi đến:** Customer
- **Thông tin:** Số đơn hàng, trạng thái mới
- **Link:** `/user/orders/{orderId}`
- **Mức độ quan trọng:** 🔴 CAO

**Code cần thêm:**
```csharp
// Khi cập nhật trạng thái đơn hàng
await _notificationService.CreateNotificationAsync(
    order.UserId,
    GetOrderStatusTitle(newStatus),
    GetOrderStatusMessage(order.OrderNumber, newStatus),
    "order_status_update",
    $"/user/orders/{order.Id}");
```

### 6. **Sản phẩm sắp hết hàng** ❌
- **Nơi cần thêm:** Background job hoặc khi stock < threshold
- **Khi nào:** Khi số lượng tồn kho < 10 (hoặc threshold tùy chỉnh)
- **Gửi đến:** Admin và Seller (của sản phẩm đó)
- **Thông tin:** Tên sản phẩm, số lượng còn lại
- **Link:** `/admin/inventory` hoặc `/seller/products/{productId}`
- **Mức độ quan trọng:** 🟡 TRUNG BÌNH

### 7. **Coupon sắp hết hạn** ❌
- **Nơi cần thêm:** Background job
- **Khi nào:** Coupon sẽ hết hạn trong 3 ngày
- **Gửi đến:** Admin
- **Thông tin:** Mã coupon, ngày hết hạn
- **Link:** `/admin/coupons`
- **Mức độ quan trọng:** 🟢 THẤP

### 8. **User mới đăng ký** ❌
- **Nơi cần thêm:** `AccountController.Register()`
- **Khi nào:** Khi có user mới đăng ký
- **Gửi đến:** Admin
- **Thông tin:** Tên user, email, thời gian đăng ký
- **Link:** `/admin/users?search={email}`
- **Mức độ quan trọng:** 🟢 THẤP

---

## 📊 TỔNG KẾT

| Loại thông báo | Trạng thái | Mức độ quan trọng |
|---------------|-----------|------------------|
| Đơn hàng mới | ✅ Đã có | 🔴 Cao |
| Liên hệ từ khách hàng | ✅ Đã thêm | 🔴 Cao |
| Yêu cầu hoàn tiền | ✅ Đã có | 🔴 Cao |
| Seller tạo sản phẩm | ❌ Chưa có | 🔴 Cao |
| Đơn hàng đổi trạng thái | ❌ Chưa có | 🔴 Cao |
| Sản phẩm phê duyệt/từ chối | ❌ Chưa có | 🔴 Cao |
| Review cần phê duyệt | ❌ Chưa có | 🟡 Trung bình |
| Sản phẩm sắp hết hàng | ❌ Chưa có | 🟡 Trung bình |
| Review được phê duyệt | ❌ Chưa có | 🟢 Thấp |
| User mới đăng ký | ❌ Chưa có | 🟢 Thấp |
| Coupon sắp hết hạn | ❌ Chưa có | 🟢 Thấp |

**Tổng số thông báo:**
- ✅ Đã có: 3/11 (27%)
- ❌ Chưa có: 8/11 (73%)

---

## 🎯 ƯU TIÊN THỰC HIỆN

### Phase 1 - CẦN GẤP (1-2 ngày)
1. ✅ **Liên hệ từ khách hàng** - ĐÃ HOÀN THÀNH
2. ❌ **Seller tạo sản phẩm mới**
3. ❌ **Đơn hàng thay đổi trạng thái**
4. ❌ **Sản phẩm được phê duyệt/từ chối**

### Phase 2 - QUAN TRỌNG (3-5 ngày)
5. ❌ **Review mới cần phê duyệt**
6. ❌ **Sản phẩm sắp hết hàng**

### Phase 3 - BỔ SUNG (7-10 ngày)
7. ❌ **Review được phê duyệt**
8. ❌ **User mới đăng ký**
9. ❌ **Coupon sắp hết hạn**

---

## 🔧 HƯỚNG DẪN TRIỂN KHAI

### Bước 1: Inject INotificationService
```csharp
private readonly INotificationService _notificationService;

public YourController(INotificationService notificationService)
{
    _notificationService = notificationService;
}
```

### Bước 2: Gửi notification
```csharp
await _notificationService.CreateNotificationAsync(
    userId,           // ID của người nhận
    title,           // Tiêu đề ngắn gọn
    message,         // Nội dung chi tiết
    type,            // Loại: "order", "product", "system", "contact", etc.
    actionUrl        // Link để xem chi tiết (optional)
);
```

### Bước 3: Wrap trong try-catch
```csharp
try
{
    // Gửi notifications
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to send notification");
    // Không fail toàn bộ operation
}
```

---

## 📋 NOTIFICATION TYPES ĐÃ ĐỊNH NGHĨA

| Type | Icon | Màu sắc | Mô tả |
|------|------|---------|-------|
| `order` | shopping-cart | Primary (blue) | Đơn hàng |
| `new_order` | shopping-bag | Primary | Đơn hàng mới cho admin |
| `order_confirmed` | check-circle | Success | Đơn hàng đã xác nhận |
| `order_status_update` | truck | Info | Cập nhật trạng thái đơn hàng |
| `product` | package | Warning (orange) | Sản phẩm |
| `new_product` | package-plus | Warning | Sản phẩm mới từ seller |
| `product_approved` | check | Success | Sản phẩm được phê duyệt |
| `product_rejected` | x-circle | Danger | Sản phẩm bị từ chối |
| `review_pending` | message-square | Info | Review chờ phê duyệt |
| `review_approved` | check | Success | Review được phê duyệt |
| `contact` | mail | Info | Liên hệ từ khách hàng |
| `system` | settings | Secondary (gray) | Thông báo hệ thống |
| `welcome` | smile | Success (green) | Chào mừng user mới |
| `refund` | dollar-sign | Warning | Hoàn tiền |

---

## 🧪 CÁCH KIỂM TRA

### 1. Kiểm tra trên UI
```
1. Đăng nhập với tài khoản Admin
2. Truy cập: http://localhost:5101/admin/notifications
3. Thực hiện các actions (đặt hàng, gửi form liên hệ, etc.)
4. Refresh trang notifications để xem thông báo mới
```

### 2. Kiểm tra qua API
```bash
# Lấy danh sách notifications
curl -X GET "http://localhost:5101/api/notifications" \
  -H "Cookie: .AspNetCore.Identity.Application=YOUR_COOKIE"

# Số lượng chưa đọc
curl -X GET "http://localhost:5101/api/notifications/unread-count" \
  -H "Cookie: .AspNetCore.Identity.Application=YOUR_COOKIE"
```

### 3. Kiểm tra Database
```sql
-- Xem tất cả notifications
SELECT * FROM "Notifications" ORDER BY "CreatedAt" DESC LIMIT 10;

-- Xem notifications của admin cụ thể
SELECT * FROM "Notifications" 
WHERE "UserId" = 'admin_user_id' 
ORDER BY "CreatedAt" DESC;

-- Thống kê theo loại
SELECT "Type", COUNT(*) as count 
FROM "Notifications" 
GROUP BY "Type";
```

---

## 📞 SUPPORT

Nếu cần hỗ trợ thêm về notification system:

1. Xem [NotificationsController.cs](../Controllers/NotificationsController.cs)
2. Xem [INotificationService.cs](../Services/INotificationService.cs)
3. Xem [Views/Admin/Notifications.cshtml](../Views/Admin/Notifications.cshtml)
4. Check logs trong `logs/` directory

---

**Ngày cập nhật:** 19/12/2025  
**Status:** ✅ Liên hệ từ khách hàng đã được thêm vào  
**Next:** Thêm notifications cho Seller tạo sản phẩm mới

# Hệ thống Kiểm Tra Thanh Toán (Payment Validation System)

## Tổng quan

Hệ thống này đảm bảo chỉ các đơn hàng đã được thanh toán mới được xử lý và giao hàng. Đặc biệt quan trọng cho các phương thức thanh toán online như VNPay và MoMo.

## Các Trạng Thái Thanh Toán

### 1. VNPay / MoMo (Online Payment)
- **pending**: Đơn hàng mới tạo, chờ thanh toán
- **paid**: Đã thanh toán thành công qua cổng thanh toán
- **failed**: Thanh toán thất bại
- **cancelled**: Đơn hàng bị hủy

### 2. COD (Cash on Delivery)
- **cod_pending**: Đơn hàng COD chờ xác nhận
- **pending**: Đơn hàng đang được xử lý
- **shipped**: Đơn hàng đang giao
- **delivered**: Đã giao hàng (tự động chuyển sang **paid**)
- **paid**: Đã thu tiền COD

### 3. Bank Transfer (Chuyển khoản)
- **awaiting_transfer**: Chờ khách hàng chuyển khoản
- **pending**: Đã chuyển khoản, chờ admin xác nhận
- **paid**: Admin đã xác nhận chuyển khoản

## Cách Sử Dụng

### A. Trong Code (C#)

#### 1. Inject PaymentValidator

```csharp
using JohnHenryFashionWeb.Helpers;

public class AdminController : Controller
{
    private readonly PaymentValidator _paymentValidator;
    
    public AdminController(PaymentValidator paymentValidator)
    {
        _paymentValidator = paymentValidator;
    }
}
```

#### 2. Kiểm tra trạng thái thanh toán

```csharp
// Kiểm tra đơn hàng đã thanh toán chưa
var validation = await _paymentValidator.ValidatePaymentStatusAsync(orderId);

if (validation.IsValid)
{
    // Đơn hàng đã thanh toán, có thể xử lý
    Console.WriteLine(validation.Message);
}
else
{
    // Đơn hàng chưa thanh toán
    return BadRequest(validation.Message);
}
```

#### 3. Kiểm tra có thể ship không

```csharp
// Kiểm tra có thể giao hàng không
var canShip = await _paymentValidator.CanShipOrderAsync(orderId);

if (canShip)
{
    // Có thể ship (VNPay/MoMo đã paid HOẶC là COD)
    await ShipOrder(orderId);
}
else
{
    return BadRequest("Đơn hàng chưa thanh toán, không thể giao hàng");
}
```

#### 4. Kiểm tra có thể complete không

```csharp
// Kiểm tra có thể hoàn thành đơn không
var canComplete = await _paymentValidator.CanCompleteOrderAsync(orderId);

if (canComplete)
{
    await CompleteOrder(orderId);
}
```

### B. Trong Database (SQL)

#### 1. Kiểm tra trạng thái thanh toán

```sql
-- Kiểm tra một đơn hàng cụ thể
SELECT * FROM check_order_payment_status('order-id-here');

-- Kết quả:
-- is_paid: true/false
-- payment_status: paid/pending/cod_pending/awaiting_transfer
-- payment_method: vnpay/momo/cod/bank_transfer
-- can_ship: true/false
-- can_complete: true/false
-- message: Thông báo chi tiết
```

#### 2. Xem tất cả đơn hàng với trạng thái

```sql
-- View tổng hợp
SELECT * FROM vw_orders_payment_status
WHERE payment_status != 'paid'
ORDER BY created_at DESC;
```

#### 3. Lấy danh sách đơn chưa thanh toán

```sql
-- Lấy 50 đơn chưa thanh toán gần nhất
SELECT * FROM get_unpaid_orders(50);

-- Kết quả bao gồm:
-- - order_id, order_number
-- - payment_method, payment_status
-- - total_amount
-- - days_pending (số ngày chờ thanh toán)
```

#### 4. Xác nhận thanh toán (Admin)

```sql
-- Admin xác nhận đã nhận chuyển khoản
CALL confirm_payment(
    'order-id-here',
    'admin-user-id',
    'Đã xác nhận chuyển khoản vào tài khoản'
);

-- Sau khi gọi:
-- - PaymentStatus => 'paid'
-- - Thêm record vào OrderStatusHistories
```

### C. Trigger Tự Động

#### Auto-update COD payment khi delivered

Khi đơn hàng COD được mark là `delivered`, hệ thống **tự động** cập nhật `PaymentStatus` thành `paid`.

```sql
-- Ví dụ: Admin mark đơn COD là delivered
UPDATE "Orders"
SET "Status" = 'delivered'
WHERE "Id" = 'cod-order-id';

-- Trigger tự động thực hiện:
-- PaymentStatus => 'paid' (tự động)
```

## Flow Xử Lý Đơn Hàng

### 1. VNPay / MoMo Flow

```
Khách đặt hàng
    ↓
Order.Status = 'pending'
Order.PaymentStatus = 'pending'
    ↓
Chuyển đến cổng thanh toán
    ↓
Thanh toán thành công
    ↓
Callback từ VNPay/MoMo
    ↓
Order.PaymentStatus = 'paid' ✅
Order.Status = 'processing'
    ↓
Admin có thể ship
    ↓
Order.Status = 'shipped'
    ↓
Order.Status = 'delivered'
```

### 2. COD Flow

```
Khách đặt hàng
    ↓
Order.Status = 'pending'
Order.PaymentStatus = 'cod_pending'
    ↓
Admin xác nhận đơn
    ↓
Order.Status = 'processing'
    ↓
Admin ship hàng
    ↓
Order.Status = 'shipped'
    ↓
Giao hàng thành công
    ↓
Order.Status = 'delivered'
Order.PaymentStatus = 'paid' ✅ (AUTO)
```

### 3. Bank Transfer Flow

```
Khách đặt hàng
    ↓
Order.Status = 'pending'
Order.PaymentStatus = 'awaiting_transfer'
    ↓
Khách chuyển khoản
    ↓
Khách upload ảnh xác nhận
    ↓
Admin kiểm tra và xác nhận
    ↓
CALL confirm_payment(...)
    ↓
Order.PaymentStatus = 'paid' ✅
Order.Status = 'processing'
    ↓
Admin ship hàng
```

## Lưu Ý Quan Trọng

### ⚠️ KHÔNG BAO GIỜ
- **KHÔNG** ship đơn VNPay/MoMo nếu `PaymentStatus != 'paid'`
- **KHÔNG** ship đơn Bank Transfer nếu chưa xác nhận chuyển khoản
- **KHÔNG** tự ý đổi `PaymentStatus` thành `paid` mà không có xác nhận

### ✅ LUÔN LUÔN
- **LUÔN** kiểm tra `PaymentStatus` trước khi ship
- **LUÔN** sử dụng `PaymentValidator` trong code
- **LUÔN** gọi `check_order_payment_status()` trong SQL trước khi thao tác
- **LUÔN** log lại khi xác nhận thanh toán thủ công

## Ví Dụ Thực Tế

### Admin Ship Đơn Hàng

```csharp
[HttpPost]
public async Task<IActionResult> ShipOrder(Guid orderId)
{
    // Kiểm tra thanh toán
    var canShip = await _paymentValidator.CanShipOrderAsync(orderId);
    
    if (!canShip)
    {
        return BadRequest("Không thể giao hàng: Đơn hàng chưa thanh toán");
    }
    
    // Tiếp tục xử lý ship
    var order = await _context.Orders.FindAsync(orderId);
    order.Status = "shipped";
    order.ShippedAt = DateTime.UtcNow;
    
    await _context.SaveChangesAsync();
    
    return Ok("Đã cập nhật trạng thái giao hàng");
}
```

### Admin Xác Nhận Chuyển Khoản

```csharp
[HttpPost]
public async Task<IActionResult> ConfirmBankTransfer(Guid orderId, string notes)
{
    var order = await _context.Orders.FindAsync(orderId);
    
    if (order.PaymentMethod.ToLower() != "bank_transfer")
    {
        return BadRequest("Đơn hàng không phải chuyển khoản");
    }
    
    // Xác nhận thanh toán
    order.PaymentStatus = "paid";
    order.UpdatedAt = DateTime.UtcNow;
    
    // Log lại
    _context.OrderStatusHistories.Add(new OrderStatusHistory
    {
        OrderId = orderId,
        Status = "payment_confirmed",
        Notes = notes,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
    });
    
    await _context.SaveChangesAsync();
    
    return Ok("Đã xác nhận chuyển khoản");
}
```

## Troubleshooting

### Vấn đề: Đơn VNPay đã thanh toán nhưng vẫn hiện "pending"

**Nguyên nhân**: Callback từ VNPay chưa được xử lý

**Giải pháp**:
1. Kiểm tra log VNPay callback
2. Kiểm tra `PaymentAttempts` table
3. Chạy lại callback manually nếu cần

### Vấn đề: Đơn COD không thể ship

**Nguyên nhân**: Status không đúng

**Giải pháp**:
```sql
-- Kiểm tra status
SELECT "Status", "PaymentStatus" FROM "Orders" WHERE "Id" = 'order-id';

-- COD có thể ship khi Status = 'pending' hoặc 'processing'
UPDATE "Orders"
SET "Status" = 'processing'
WHERE "Id" = 'order-id';
```

## Cập Nhật Data Cũ

Nếu có data cũ cần cập nhật:

```sql
-- Update tất cả đơn COD đã delivered nhưng chưa paid
UPDATE "Orders"
SET "PaymentStatus" = 'paid'
WHERE 
    LOWER("PaymentMethod") = 'cod'
    AND "Status" = 'delivered'
    AND "PaymentStatus" != 'paid';

-- Update đơn COD pending
UPDATE "Orders"
SET "PaymentStatus" = 'cod_pending'
WHERE 
    LOWER("PaymentMethod") = 'cod'
    AND "Status" = 'pending'
    AND "PaymentStatus" = 'pending';

-- Update đơn Bank Transfer
UPDATE "Orders"
SET "PaymentStatus" = 'awaiting_transfer'
WHERE 
    LOWER("PaymentMethod") = 'bank_transfer'
    AND "Status" = 'pending'
    AND "PaymentStatus" = 'pending';
```

## Tham Khảo

- File: `Helpers/PaymentValidator.cs`
- File: `database/create_payment_validation_functions.sql`
- Controller: `PaymentController.cs`
- Controller: `CheckoutController.cs`

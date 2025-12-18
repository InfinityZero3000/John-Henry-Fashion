# Email Templates - John Henry Fashion

Thư mục này chứa các mẫu email được sử dụng trong hệ thống John Henry Fashion.

## 📧 Danh Sách Templates

### 1. **Welcome.html** ✅ ĐÃ CẢI TIẾN
**Mục đích:** Email chào mừng user mới đăng ký tài khoản

**Sử dụng ở:**
- `EmailService.SendWelcomeEmailAsync()` - Services/EmailService.cs (line 94-101)
- `AccountController.VerifyEmailCode()` - Sau khi user verify email thành công (line 1554-1590)
- `AccountController.Register()` - Flow đăng ký không cần verify (line 416-438)
- `AccountController.SendWelcomeEmailAsync()` - Đăng ký qua Google (line 1949-1963)

**Variables:**
- `{{UserName}}` - Tên đầy đủ của user
- `{{CompanyName}}` - Tên công ty (John Henry Fashion)
- `{{LoginUrl}}` - Link đăng nhập

**Đặc điểm:**
- ✨ Sử dụng hero banner từ CDN
- 🎁 Hiển thị ưu đãi giảm giá 15% cho đơn đầu
- 🌟 Liệt kê đầy đủ đặc quyền thành viên
- 📱 Responsive design cho mobile
- 🎨 Gradient hiện đại và professional

---

### 2. **OrderConfirmation.html**
**Mục đích:** Email xác nhận đơn hàng sau khi đặt hàng thành công

**Sử dụng ở:**
- `EmailService.SendOrderConfirmationEmailAsync()` - Services/EmailService.cs (line 104-118)
- `CheckoutController.ProcessCheckout()` - Sau khi thanh toán thành công (line 798)
- `NotificationService.SendOrderNotificationAsync()` - line 212

**Variables:**
- `{{OrderNumber}}` - Mã đơn hàng
- `{{OrderDate}}` - Ngày đặt hàng
- `{{CustomerName}}` - Tên khách hàng
- `{{ShippingAddress}}` - Địa chỉ giao hàng
- `{{PaymentMethod}}` - Phương thức thanh toán
- `{{OrderItems}}` - Danh sách sản phẩm (HTML)
- `{{Subtotal}}` - Tổng tiền hàng
- `{{ShippingFee}}` - Phí vận chuyển
- `{{TotalAmount}}` - Tổng thanh toán
- `{{TrackingUrl}}` - Link theo dõi đơn hàng

---

### 3. **OrderStatusUpdate.html**
**Mục đích:** Email thông báo cập nhật trạng thái đơn hàng

**Sử dụng ở:**
- `EmailService.SendOrderStatusUpdateEmailAsync()` - Services/EmailService.cs (line 121-141)
- Được gọi khi admin cập nhật trạng thái đơn hàng

**Variables:**
- `{{OrderNumber}}` - Mã đơn hàng
- `{{OrderStatus}}` - Trạng thái mới (Pending, Processing, Shipped, Delivered, Cancelled)
- `{{StatusClass}}` - CSS class cho badge (status-pending, status-processing, etc.)
- `{{StatusMessage}}` - Thông điệp trạng thái
- `{{CustomerName}}` - Tên khách hàng
- `{{OrderDate}}` - Ngày đặt hàng
- `{{TotalAmount}}` - Tổng tiền
- `{{TrackingUrl}}` - Link theo dõi đơn hàng

---

### 4. **ContactConfirmation.html**
**Mục đích:** Email xác nhận đã nhận được tin nhắn liên hệ từ khách hàng

**Sử dụng ở:**
- `EmailService.SendContactConfirmationEmailAsync()` - Services/EmailService.cs (line 144-163)
- `ContactController.Contact()` - Sau khi submit form liên hệ (line 84)

**Variables:**
- `{{CustomerName}}` - Tên người liên hệ
- `{{Email}}` - Email người liên hệ
- `{{Subject}}` - Chủ đề
- `{{Message}}` - Nội dung tin nhắn
- `{{MessageDate}}` - Ngày gửi tin nhắn
- `{{ResponseTime}}` - Thời gian phản hồi dự kiến (24-48h)

---

## 🔧 Cách Sử Dụng

### 1. Trong EmailService.cs

```csharp
// Đọc template
var template = await GetEmailTemplateAsync("Welcome");

// Thay thế variables
var body = template.Replace("{{UserName}}", userName)
                  .Replace("{{CompanyName}}", "John Henry Fashion")
                  .Replace("{{LoginUrl}}", loginUrl);

// Gửi email
await SendEmailAsync(email, subject, body, null, null, isHtml: true);
```

### 2. Thêm Template Mới

1. Tạo file HTML mới trong thư mục EmailTemplates/
2. Sử dụng các biến với format `{{VariableName}}`
3. Thêm method trong IEmailService interface
4. Implement trong EmailService.cs
5. Gọi method từ Controller hoặc Service

---

## 🎨 Design Guidelines

### Color Scheme
- **Primary Red:** `#dc3545` - Brand color
- **Dark:** `#1a1a2e` - Headers, footers
- **Success Green:** `#28a745` - Đơn hàng thành công
- **Info Blue:** `#007bff` - Thông tin cập nhật

### Typography
- **Font:** Segoe UI, Tahoma, Geneva, Verdana, sans-serif
- **Heading:** 2em - 2.5em
- **Body:** 1em - 1.1em
- **Small:** 0.9em - 0.95em

### Layout
- **Max Width:** 600-650px
- **Padding:** 30-40px
- **Border Radius:** 8-12px
- **Shadow:** 0 2px 10px rgba(0,0,0,0.1)

---

## 📱 Responsive Design

Tất cả templates đều responsive với breakpoint:
```css
@media (max-width: 650px) {
    /* Mobile styles */
}
```

---

## 🔗 External Resources

### Banner Image
```
https://raw.githubusercontent.com/InfinityZero3000/Image-CDN/refs/heads/main/banner_018d84a8.jpg
```
Được sử dụng trong Welcome.html

---

## 📊 Email Sending Flow

### User Registration
1. User đăng ký → `AccountController.Register()`
2. Tạo user → `userManager.CreateAsync()`
3. Gửi email verification (nếu cần)
4. **✅ Gửi Welcome email** → `SendWelcomeEmailAsync()`

### Order Placement
1. User checkout → `CheckoutController.ProcessCheckout()`
2. Tạo order → Order entity
3. **✅ Gửi Order Confirmation** → `SendOrderConfirmationEmailAsync()`

### Order Status Update
1. Admin update status → Admin panel
2. **✅ Gửi Status Update** → `SendOrderStatusUpdateEmailAsync()`

### Contact Form
1. User submit form → `ContactController.Contact()`
2. Lưu message → ContactMessage entity
3. **✅ Gửi Confirmation** → `SendContactConfirmationEmailAsync()`

---

## ⚙️ Configuration

Email settings trong `appsettings.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderName": "John Henry Fashion",
    "SenderEmail": "noreply@johnhenry.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "BaseUrl": "https://yourdomain.com"
  }
}
```

---

## 🧪 Testing

Test emails tại: `/TestEmail` page

```csharp
// Test Welcome email
await _emailService.SendWelcomeEmailAsync("test@example.com", "Test User");

// Test Order Confirmation
await _emailService.SendOrderConfirmationEmailAsync("test@example.com", mockOrder);

// Test Contact Confirmation
await _emailService.SendContactConfirmationEmailAsync("test@example.com", mockMessage);

// Test Order Status Update
await _emailService.SendOrderStatusUpdateEmailAsync("test@example.com", mockOrder);
```

---

## 📝 Changelog

### Version 2.0 - December 17, 2025
- ✨ **Welcome.html:** Cải tiến hoàn toàn với hero banner
- 🎁 Thêm promo banner giảm giá 15%
- 🌟 Nâng cấp UI/UX với gradient và shadow
- 📱 Tối ưu responsive cho mobile
- 🔗 Tích hợp CDN banner image

### Version 1.0 - Initial Release
- ✅ Welcome.html (basic version)
- ✅ OrderConfirmation.html
- ✅ OrderStatusUpdate.html
- ✅ ContactConfirmation.html

---

## 🐛 Troubleshooting

### Email không gửi được
1. Kiểm tra EmailSettings trong appsettings.json
2. Verify SMTP credentials
3. Check firewall/port 587
4. Enable "Less secure app access" cho Gmail

### Template không hiển thị đúng
1. Kiểm tra các biến `{{VariableName}}` đã được replace chưa
2. Verify HTML syntax
3. Test trên nhiều email clients (Gmail, Outlook, etc.)

### Images không load
1. Sử dụng absolute URL cho images
2. Verify CDN/image hosting
3. Check CORS headers

---

**Maintained by:** John Henry Fashion Development Team
**Last Updated:** December 17, 2025

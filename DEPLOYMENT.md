# 🚀 HƯỚNG DẪN DEPLOY LÊN RENDER

Hướng dẫn chi tiết từng bước để deploy ứng dụng **John Henry Fashion** lên Render.com với đầy đủ các dịch vụ cần thiết.

---

## 📋 MỤC LỤC

1. [Chuẩn bị trước khi deploy](#1-chuẩn-bị-trước-khi-deploy)
2. [Tạo tài khoản và cấu hình các dịch vụ bên thứ ba](#2-tạo-tài-khoản-và-cấu-hình-các-dịch-vụ-bên-thứ-ba)
3. [Đẩy code lên GitHub](#3-đẩy-code-lên-github)
4. [Tạo PostgreSQL Database trên Render](#4-tạo-postgresql-database-trên-render)
5. [Tạo Web Service trên Render](#5-tạo-web-service-trên-render)
6. [Cấu hình Environment Variables](#6-cấu-hình-environment-variables)
7. [Cấu hình lưu trữ ảnh và video](#7-cấu-hình-lưu-trữ-ảnh-và-video)
8. [Chạy Database Migrations](#8-chạy-database-migrations)
9. [Troubleshooting](#9-troubleshooting)

---

## 1. CHUẨN BỊ TRƯỚC KHI DEPLOY

### ✅ Checklist

- [ ] Tài khoản GitHub
- [ ] Tài khoản Render (https://render.com - miễn phí)
- [ ] Code đã được push lên GitHub
- [ ] File `render.yaml`, `Dockerfile`, `.dockerignore` đã được tạo (✅ Done)
- [ ] Các API keys và credentials đã sẵn sàng

---

## 2. TẠO TÀI KHOẢN VÀ CẤU HÌNH CÁC DỊCH VỤ BÊN THỨ BA

### 2.1. 🔐 **Google OAuth** (cho đăng nhập Google)

1. Truy cập: https://console.cloud.google.com/
2. Tạo project mới hoặc chọn project hiện có
3. Vào **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth 2.0 Client ID**
5. Chọn **Web application**
6. Thêm **Authorized redirect URIs**:
   ```
   https://your-app-name.onrender.com/signin-google
   https://your-app-name.onrender.com/Account/GoogleResponse
   ```
7. Lưu lại `Client ID` và `Client Secret`

### 2.2. 📧 **Gmail App Password** (cho gửi email)

1. Truy cập: https://myaccount.google.com/apppasswords
2. Chọn **Mail** và **Other (Custom name)**
3. Đặt tên: "John Henry Fashion"
4. Click **Generate**
5. Lưu lại mật khẩu 16 ký tự (không có dấu cách)

⚠️ **Lưu ý**: Phải bật 2-Step Verification trước khi tạo App Password

### 2.3. 💳 **VNPay** (Thanh toán VN)

#### Môi trường Sandbox (Test):
1. Truy cập: https://sandbox.vnpayment.vn/
2. Đăng ký tài khoản merchant
3. Lấy `TmnCode` và `HashSecret` từ dashboard
4. Sử dụng URL sandbox:
   - Payment: `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`
   - API: `https://sandbox.vnpayment.vn/merchant_webapi/api/transaction`

#### Môi trường Production:
1. Liên hệ VNPay để đăng ký tài khoản chính thức
2. Cập nhật URL production và `VNPAY_SANDBOX=false`

### 2.4. 💰 **MoMo** (Thanh toán VN)

#### Môi trường Sandbox:
1. Truy cập: https://developers.momo.vn/
2. Đăng ký và tạo ứng dụng
3. Lấy `Partner Code`, `Access Key`, `Secret Key`
4. Test URL: `https://test-payment.momo.vn/v2/gateway/api/create`

#### Môi trường Production:
1. Đăng ký doanh nghiệp tại: https://business.momo.vn/
2. Cập nhật production keys và URL

### 2.5. 💎 **Stripe** (Thanh toán quốc tế)

1. Truy cập: https://stripe.com/
2. Đăng ký tài khoản
3. Lấy keys từ **Developers** > **API keys**:
   - `Publishable key` (pk_test_...)
   - `Secret key` (sk_test_...)
4. Tạo **Webhook endpoint**:
   - URL: `https://your-app-name.onrender.com/api/stripe/webhook`
   - Events: `payment_intent.succeeded`, `payment_intent.payment_failed`
   - Lưu `Webhook Secret` (whsec_...)

### 2.6. 🗺️ **Google Maps API** (Optional)

1. Truy cập: https://console.cloud.google.com/google/maps-apis
2. Enable APIs:
   - Maps JavaScript API
   - Places API
   - Geocoding API
3. Tạo API key từ **Credentials**
4. Restrict key cho domain của bạn

---

## 3. ĐẨY CODE LÊN GITHUB

```bash
# Khởi tạo Git (nếu chưa có)
git init

# Thêm tất cả files
git add .

# Commit
git commit -m "Prepare for Render deployment"

# Thêm remote repository
git remote add origin https://github.com/your-username/john-henry-website.git

# Push lên GitHub
git push -u origin main
```

⚠️ **Quan trọng**: Đảm bảo file `.env` đã được gitignore và KHÔNG được push lên GitHub!

---

## 4. TẠO POSTGRESQL DATABASE TRÊN RENDER

1. Đăng nhập: https://dashboard.render.com/
2. Click **New +** > **PostgreSQL**
3. Điền thông tin:
   - **Name**: `johnhenry-db`
   - **Database**: `johnhenry_db`
   - **User**: `johnhenry_user`
   - **Region**: `Singapore` (gần Việt Nam nhất)
   - **Plan**: **Free** (hoặc Starter nếu cần nhiều tài nguyên)
4. Click **Create Database**
5. Đợi database được tạo (khoảng 1-2 phút)
6. Lưu lại thông tin kết nối:
   - **Internal Database URL** (dùng trong Render)
   - **External Database URL** (dùng để connect từ máy local)

### 📝 Kết nối từ máy local (để test):

```bash
# Cài đặt psql (nếu chưa có)
brew install postgresql  # macOS

# Connect
psql <External Database URL>
```

---

## 5. TẠO WEB SERVICE TRÊN RENDER

### Option 1: Dùng Blueprint (render.yaml) - **RECOMMENDED** ⭐

1. Vào Dashboard > **New +** > **Blueprint**
2. Connect GitHub repository của bạn
3. Render sẽ tự động detect file `render.yaml` và tạo:
   - PostgreSQL Database
   - Web Service với Docker
4. Review cấu hình và click **Apply**

### Option 2: Tạo thủ công

1. Vào Dashboard > **New +** > **Web Service**
2. Connect GitHub repository
3. Điền thông tin:
   - **Name**: `johnhenry-web`
   - **Region**: `Singapore`
   - **Branch**: `main`
   - **Runtime**: **Docker**
   - **Plan**: **Free** (hoặc Starter)
4. Scroll xuống **Environment Variables** (xem section 6)
5. Click **Create Web Service**

---

## 6. CẤU HÌNH ENVIRONMENT VARIABLES

Trong Render Dashboard > Web Service > **Environment** tab, thêm các biến sau:

### 🔧 **Essential Variables**

```bash
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# Database (auto-filled nếu dùng blueprint)
DB_HOST=<từ Render PostgreSQL>
DB_PORT=5432
DB_NAME=johnhenry_db
DB_USER=johnhenry_user
DB_PASSWORD=<từ Render PostgreSQL>

# JWT
JWT_SECRET_KEY=<Generate random 32+ characters>
JWT_ISSUER=JohnHenryFashion
JWT_AUDIENCE=JohnHenryUsers

# Google OAuth
GOOGLE_CLIENT_ID=<từ Google Console>
GOOGLE_CLIENT_SECRET=<từ Google Console>

# Email
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_USE_SSL=true
EMAIL_USER=<your-email@gmail.com>
EMAIL_PASSWORD=<Gmail App Password>
EMAIL_FROM=<your-email@gmail.com>
EMAIL_FROM_NAME=John Henry Fashion
```

### 💳 **Payment Gateways**

```bash
# VNPay
VNPAY_TMN_CODE=<your-code>
VNPAY_HASH_SECRET=<your-secret>
VNPAY_PAYMENT_URL=https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
VNPAY_API_URL=https://sandbox.vnpayment.vn/merchant_webapi/api/transaction
VNPAY_ENABLED=true
VNPAY_SANDBOX=true

# MoMo
MOMO_PARTNER_CODE=<your-code>
MOMO_ACCESS_KEY=<your-key>
MOMO_SECRET_KEY=<your-secret>
MOMO_API_URL=https://test-payment.momo.vn/v2/gateway/api/create
MOMO_ENABLED=true
MOMO_SANDBOX=true

# Stripe
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_API_URL=https://api.stripe.com
STRIPE_CURRENCY=vnd
STRIPE_ENABLED=true
STRIPE_SANDBOX=true
```

### 🗺️ **Optional Services**

```bash
# Google Maps
GOOGLE_MAPS_API_KEY=<your-api-key>

# Redis (nếu dùng Redis Cloud)
REDIS_CONNECTION=<redis-host>:port,password=<pwd>,ssl=True
```

💡 **Tip**: Click **Add from .env** để paste nhiều biến cùng lúc!

---

## 7. CẤU HÌNH LƯU TRỮ ẢNH VÀ VIDEO

Render **KHÔNG HỖ TRỢ** persistent storage trên Free plan. Mỗi lần deploy, file sẽ bị xóa. 

### ⚠️ Giải pháp: Dùng Cloud Storage

### Option 1: **Cloudinary** (RECOMMENDED) ⭐

**Ưu điểm**: 
- Free 25GB storage
- Tự động optimize ảnh/video
- CDN toàn cầu
- API đơn giản

**Cách setup**:

1. Đăng ký: https://cloudinary.com/users/register/free
2. Lấy credentials từ Dashboard:
   - Cloud Name
   - API Key
   - API Secret
3. Cài package:
   ```bash
   dotnet add package CloudinaryDotNet
   ```
4. Thêm vào `appsettings.json`:
   ```json
   "Cloudinary": {
     "CloudName": "your-cloud-name",
     "ApiKey": "your-api-key",
     "ApiSecret": "your-api-secret"
   }
   ```
5. Thêm Environment Variables trên Render:
   ```bash
   CLOUDINARY_CLOUD_NAME=<your-cloud-name>
   CLOUDINARY_API_KEY=<your-api-key>
   CLOUDINARY_API_SECRET=<your-api-secret>
   ```

**Code example** (tạo service mới):

```csharp
// Services/CloudinaryService.cs
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]
        );
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder = "products")
    {
        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            Transformation = new Transformation()
                .Width(1200)
                .Height(1200)
                .Crop("limit")
                .Quality("auto:good")
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        return result.SecureUrl.ToString();
    }

    public async Task DeleteImageAsync(string publicId)
    {
        await _cloudinary.DestroyAsync(new DeletionParams(publicId));
    }
}
```

### Option 2: **AWS S3**

1. Tạo AWS account: https://aws.amazon.com/
2. Tạo S3 bucket (region: Singapore)
3. Tạo IAM user với S3 permissions
4. Cài package:
   ```bash
   dotnet add package AWSSDK.S3
   ```
5. Environment Variables:
   ```bash
   AWS_ACCESS_KEY_ID=<your-key>
   AWS_SECRET_ACCESS_KEY=<your-secret>
   AWS_REGION=ap-southeast-1
   AWS_BUCKET_NAME=johnhenry-uploads
   ```

### Option 3: **Render Disk** (Paid)

Nếu dùng Paid plan, bạn có thể thêm persistent disk:
1. Render Dashboard > Web Service > **Disks** tab
2. Add disk: `/app/wwwroot/uploads` (100GB)
3. Cost: $1/GB/month

---

## 8. CHẠY DATABASE MIGRATIONS

### Cách 1: Từ máy local (Nhanh nhất)

```bash
# 1. Copy External Database URL từ Render
# 2. Set connection string
export ConnectionStrings__DefaultConnection="<External Database URL>"

# 3. Chạy migrations
dotnet ef database update

# 4. (Optional) Seed data
# Tạo script SQL hoặc chạy từ code
```

### Cách 2: Từ Render Shell

1. Vào Render Dashboard > Web Service > **Shell** tab
2. Chạy lệnh:
   ```bash
   dotnet ef database update
   ```

### Cách 3: Auto-migrate khi khởi động (Production-ready)

Thêm vào `Program.cs`:

```csharp
// Sau var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Apply migrations
        await context.Database.MigrateAsync();
        
        // Seed data (optional)
        await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
```

---

## 9. TROUBLESHOOTING

### ❌ Build Failed: "Failed to restore packages"

**Nguyên nhân**: Thiếu dependencies hoặc timeout

**Giải pháp**:
```bash
# Xóa cache local
rm -rf bin/ obj/

# Restore lại
dotnet restore

# Commit và push
git add . && git commit -m "Fix dependencies" && git push
```

### ❌ Database Connection Error

**Kiểm tra**:
1. Environment variables có đúng không?
2. Database đã được tạo và running?
3. Sử dụng **Internal Database URL** (không phải External)
4. Check logs: Render Dashboard > Web Service > **Logs**

### ❌ 502 Bad Gateway

**Nguyên nhân**: App không start hoặc crash

**Giải pháp**:
1. Check logs xem lỗi gì
2. Đảm bảo `ASPNETCORE_URLS=http://+:8080`
3. Health check endpoint hoạt động: `/health`
4. Tăng timeout trong `render.yaml`:
   ```yaml
   healthCheckPath: /health
   startCommand: dotnet JohnHenryFashionWeb.dll
   ```

### ❌ Images không load sau deploy

**Nguyên nhân**: Local storage bị xóa mỗi lần deploy

**Giải pháp**: Dùng Cloudinary hoặc AWS S3 (xem section 7)

### ❌ Environment Variables không work

**Kiểm tra**:
1. Đúng tên biến không? (case-sensitive)
2. Restart service sau khi thêm biến
3. Check logs để xem giá trị có được load không

### 📊 **Monitor Performance**

```bash
# Check logs realtime
render logs --follow --service johnhenry-web

# Check database
render pg:psql johnhenry-db
```

---

## 📚 TÀI LIỆU THAM KHẢO

- [Render Docs](https://render.com/docs)
- [Render Docker Deploy](https://render.com/docs/docker)
- [Render Blueprints](https://render.com/docs/blueprint-spec)
- [ASP.NET Core on Render](https://render.com/docs/deploy-aspnet-core)
- [PostgreSQL on Render](https://render.com/docs/databases)

---

## 🎉 KẾT LUẬN

Sau khi hoàn tất các bước trên, ứng dụng của bạn sẽ:

✅ Chạy trên Docker container  
✅ Kết nối PostgreSQL database  
✅ Có SSL/TLS tự động (HTTPS)  
✅ Auto-deploy khi push code mới  
✅ Lưu ảnh/video trên cloud storage  
✅ Payment gateways đầy đủ  
✅ Email notifications  
✅ Google OAuth login  

🌐 **Your app**: `https://johnhenry-web.onrender.com`

---

## 📞 HỖ TRỢ

Nếu gặp vấn đề, check:
1. Render Dashboard > Logs
2. GitHub Actions (nếu có CI/CD)
3. Render Community: https://community.render.com/

**Happy Deploying! 🚀**

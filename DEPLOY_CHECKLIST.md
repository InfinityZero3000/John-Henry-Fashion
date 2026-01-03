# ✅ RENDER DEPLOYMENT CHECKLIST

Copy checklist này và đánh dấu ✅ khi hoàn thành mỗi bước.

---

## GIAI ĐOẠN 1: CHUẨN BỊ (5 phút)

- [ ] Code đã được commit và push lên GitHub
- [ ] File `.env` KHÔNG có trong Git (đã bị .gitignore)
- [ ] Đã có tài khoản GitHub
- [ ] Đã có tài khoản Render (https://render.com)

---

## GIAI ĐOẠN 2: CẤU HÌNH DỊCH VỤ BÊN NGOÀI (30-60 phút)

### 🔐 Google OAuth
- [ ] Đã tạo project trên Google Cloud Console
- [ ] Đã tạo OAuth 2.0 Client ID
- [ ] Đã thêm Authorized redirect URIs
- [ ] Đã lưu Client ID
- [ ] Đã lưu Client Secret

### 📧 Gmail App Password
- [ ] Đã bật 2-Step Verification
- [ ] Đã tạo App Password
- [ ] Đã lưu mật khẩu 16 ký tự

### 💳 VNPay (Optional)
- [ ] Đã đăng ký tài khoản Sandbox
- [ ] Đã lấy TmnCode
- [ ] Đã lấy HashSecret

### 💰 MoMo (Optional)
- [ ] Đã đăng ký developers.momo.vn
- [ ] Đã tạo ứng dụng
- [ ] Đã lấy Partner Code
- [ ] Đã lấy Access Key
- [ ] Đã lấy Secret Key

### 💎 Stripe (Optional)
- [ ] Đã đăng ký tài khoản Stripe
- [ ] Đã lấy Publishable Key (pk_test_)
- [ ] Đã lấy Secret Key (sk_test_)
- [ ] Đã tạo Webhook endpoint
- [ ] Đã lấy Webhook Secret (whsec_)

---

## GIAI ĐOẠN 3: DEPLOY LÊN RENDER (10 phút)

### Tạo Database
- [ ] Đã tạo PostgreSQL database trên Render
- [ ] Name: `johnhenry-db`
- [ ] Region: Singapore
- [ ] Plan: Free hoặc Starter
- [ ] Đã lưu Internal Database URL
- [ ] Đã lưu External Database URL

### Tạo Web Service
- [ ] Đã connect GitHub repository
- [ ] Đã chọn deploy type: **Blueprint** (render.yaml)
- [ ] Hoặc đã tạo **Web Service** thủ công
- [ ] Runtime: Docker
- [ ] Region: Singapore
- [ ] Branch: main

---

## GIAI ĐOẠN 4: ENVIRONMENT VARIABLES (10 phút)

Vào Dashboard → Web Service → Environment

### Bắt buộc
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `ASPNETCORE_URLS=http://+:8080`
- [ ] `DB_HOST` (từ Render PostgreSQL)
- [ ] `DB_PORT=5432`
- [ ] `DB_NAME=johnhenry_db`
- [ ] `DB_USER` (từ Render PostgreSQL)
- [ ] `DB_PASSWORD` (từ Render PostgreSQL)

### JWT
- [ ] `JWT_SECRET_KEY` (tạo random 32+ chars)
- [ ] `JWT_ISSUER=JohnHenryFashion`
- [ ] `JWT_AUDIENCE=JohnHenryUsers`

### Email
- [ ] `EMAIL_HOST=smtp.gmail.com`
- [ ] `EMAIL_PORT=587`
- [ ] `EMAIL_USE_SSL=true`
- [ ] `EMAIL_USER` (your Gmail)
- [ ] `EMAIL_PASSWORD` (App Password)
- [ ] `EMAIL_FROM` (your Gmail)
- [ ] `EMAIL_FROM_NAME=John Henry Fashion`

### Google OAuth
- [ ] `GOOGLE_CLIENT_ID`
- [ ] `GOOGLE_CLIENT_SECRET`

### Payment Gateways (Optional)
- [ ] `VNPAY_TMN_CODE`
- [ ] `VNPAY_HASH_SECRET`
- [ ] `VNPAY_PAYMENT_URL`
- [ ] `VNPAY_API_URL`
- [ ] `VNPAY_ENABLED=true`
- [ ] `VNPAY_SANDBOX=true`

- [ ] `MOMO_PARTNER_CODE`
- [ ] `MOMO_ACCESS_KEY`
- [ ] `MOMO_SECRET_KEY`
- [ ] `MOMO_API_URL`
- [ ] `MOMO_ENABLED=true`
- [ ] `MOMO_SANDBOX=true`

- [ ] `STRIPE_PUBLISHABLE_KEY`
- [ ] `STRIPE_SECRET_KEY`
- [ ] `STRIPE_WEBHOOK_SECRET`
- [ ] `STRIPE_CURRENCY=vnd`
- [ ] `STRIPE_ENABLED=true`
- [ ] `STRIPE_SANDBOX=true`

---

## GIAI ĐOẠN 5: DATABASE MIGRATIONS (5 phút)

Chọn một trong hai cách:

### Option A: Từ máy local
- [ ] Đã copy External Database URL
- [ ] Đã set environment variable
- [ ] Đã chạy `dotnet ef database update`
- [ ] Migrations thành công

### Option B: Từ Render Shell
- [ ] Đã mở Shell tab
- [ ] Đã chạy `dotnet ef database update`
- [ ] Migrations thành công

---

## GIAI ĐOẠN 6: STORAGE ẢNH/VIDEO (15 phút)

### Option 1: Cloudinary (Khuyến nghị)
- [ ] Đã đăng ký tài khoản Cloudinary
- [ ] Đã lấy Cloud Name
- [ ] Đã lấy API Key
- [ ] Đã lấy API Secret
- [ ] Đã thêm package `CloudinaryDotNet`
- [ ] Đã thêm env vars trên Render:
  - [ ] `CLOUDINARY_CLOUD_NAME`
  - [ ] `CLOUDINARY_API_KEY`
  - [ ] `CLOUDINARY_API_SECRET`
- [ ] Đã implement CloudinaryService
- [ ] Đã test upload ảnh

### Option 2: AWS S3
- [ ] Đã tạo AWS account
- [ ] Đã tạo S3 bucket
- [ ] Đã tạo IAM user
- [ ] Đã thêm package `AWSSDK.S3`
- [ ] Đã thêm env vars
- [ ] Đã implement S3Service

### Option 3: Render Disk (Paid)
- [ ] Đã upgrade plan
- [ ] Đã thêm persistent disk
- [ ] Path: `/app/wwwroot/uploads`

---

## GIAI ĐOẠN 7: TESTING & VERIFICATION (10 phút)

### Kiểm tra cơ bản
- [ ] Website load được: `https://your-app.onrender.com`
- [ ] Health check OK: `https://your-app.onrender.com/health`
- [ ] Không có lỗi trong Logs
- [ ] Database connection OK

### Kiểm tra chức năng
- [ ] Đăng ký tài khoản mới
- [ ] Đăng nhập thành công
- [ ] Đăng nhập với Google OAuth
- [ ] Xem danh sách sản phẩm
- [ ] Thêm sản phẩm vào giỏ hàng
- [ ] Upload ảnh (avatar/product)
- [ ] Gửi email (test contact form)

### Kiểm tra thanh toán (Optional)
- [ ] VNPay sandbox
- [ ] MoMo sandbox
- [ ] Stripe test mode
- [ ] Cash on Delivery

---

## GIAI ĐOẠN 8: PRODUCTION READY (Optional)

### Domain tùy chỉnh
- [ ] Đã mua domain
- [ ] Đã cấu hình DNS
- [ ] Đã add custom domain trên Render
- [ ] SSL certificate active

### Performance
- [ ] Đã setup Redis cache (Upstash/Redis Cloud)
- [ ] Đã optimize images (Cloudinary auto-optimization)
- [ ] Đã enable CDN

### Monitoring
- [ ] Đã setup error tracking (Sentry)
- [ ] Đã setup uptime monitoring (UptimeRobot)
- [ ] Đã setup analytics (Google Analytics)

### Security
- [ ] Đã đổi tất cả default passwords
- [ ] Đã enable 2FA cho admin accounts
- [ ] Đã review security headers
- [ ] Đã setup rate limiting
- [ ] Đã setup backup database

### Production Mode
- [ ] Đã chuyển payment gateways sang production:
  - [ ] `VNPAY_SANDBOX=false`
  - [ ] `MOMO_SANDBOX=false`
  - [ ] `STRIPE_SANDBOX=false`
- [ ] Đã dùng production API keys
- [ ] Đã dùng production URLs

---

## 📊 FINAL CHECKLIST

- [ ] ✅ Website chạy ổn định
- [ ] ✅ Không có critical errors trong logs
- [ ] ✅ Database backup được setup
- [ ] ✅ Monitoring active
- [ ] ✅ Security hardened
- [ ] ✅ Performance optimized
- [ ] ✅ Documentation updated

---

## 🎉 DEPLOYMENT COMPLETE!

**Your app is now live at:** `https://your-app.onrender.com`

### Next Steps:
1. Share link với team/users
2. Monitor logs daily trong tuần đầu
3. Setup automated backups
4. Plan for scaling nếu traffic tăng

---

## 📞 SUPPORT

Nếu gặp vấn đề:
1. ☑️ Check logs: Dashboard → Logs
2. ☑️ Check health endpoint
3. ☑️ Review environment variables
4. ☑️ Xem DEPLOYMENT.md và QUICKSTART.md
5. ☑️ Search Render Community
6. ☑️ Contact support

**Good luck! 🚀**

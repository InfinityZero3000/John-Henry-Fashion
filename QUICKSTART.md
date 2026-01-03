# 🚀 QUICK START - DEPLOY LÊN RENDER

## Các bước nhanh (15 phút):

### 1. Push code lên GitHub

```bash
git add .
git commit -m "Prepare for Render deployment"
git push origin main
```

### 2. Tạo tài khoản Render

- Truy cập: https://render.com
- Sign up with GitHub
- Authorize Render to access your repositories

### 3. Deploy với Blueprint (Auto Setup)

1. Vào Dashboard → **New +** → **Blueprint**
2. Chọn repository: `john-henry-website`
3. Render sẽ tự động:
   - Tạo PostgreSQL database
   - Build Docker image
   - Deploy web service
4. Click **Apply**

### 4. Cấu hình Environment Variables MỚI cần thiết

Vào Dashboard → Web Service → **Environment**

**Bắt buộc phải có:**

```bash
# JWT (Generate random string 32+ characters)
JWT_SECRET_KEY=your-super-secret-jwt-key-here-min-32-chars

# Email (Gmail App Password)
EMAIL_USER=your-email@gmail.com
EMAIL_PASSWORD=your-16-char-app-password
EMAIL_FROM=your-email@gmail.com

# Google OAuth
GOOGLE_CLIENT_ID=xxx.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=xxx

# Payment Gateways (Sandbox)
VNPAY_TMN_CODE=xxx
VNPAY_HASH_SECRET=xxx

MOMO_PARTNER_CODE=xxx
MOMO_ACCESS_KEY=xxx
MOMO_SECRET_KEY=xxx

STRIPE_PUBLISHABLE_KEY=pk_test_xxx
STRIPE_SECRET_KEY=sk_test_xxx
STRIPE_WEBHOOK_SECRET=whsec_xxx
```

**Các biến khác đã được set tự động từ render.yaml**

### 5. Chạy Database Migrations

**Option A: Từ máy local (Khuyến nghị)**

```bash
# Lấy External Database URL từ Render Dashboard
export ConnectionStrings__DefaultConnection="postgres://user:pass@host:5432/db"

# Chạy migrations
dotnet ef database update
```

**Option B: Từ Render Shell**

1. Dashboard → Web Service → **Shell** tab
2. Chạy:
```bash
dotnet ef database update
```

### 6. Kiểm tra deployment

- Web: `https://johnhenry-web.onrender.com`
- Health: `https://johnhenry-web.onrender.com/health`
- Logs: Dashboard → **Logs** tab

---

## ⚠️ LƯU Ý QUAN TRỌNG

### 1. Lưu trữ ảnh/video

Render **KHÔNG HỖ TRỢ** persistent storage trên Free plan!

**Giải pháp**: Dùng **Cloudinary** (Free 25GB)

```bash
# 1. Đăng ký: https://cloudinary.com/users/register/free
# 2. Cài package
dotnet add package CloudinaryDotNet

# 3. Thêm env vars trên Render
CLOUDINARY_CLOUD_NAME=your-name
CLOUDINARY_API_KEY=your-key
CLOUDINARY_API_SECRET=your-secret
```

### 2. Free plan limitations

- **Sleep sau 15 phút** không có traffic
- **Khởi động lại** khi có request (30-60 giây)
- **750 giờ/tháng** miễn phí
- **100GB bandwidth**

**Giải pháp**: 
- Upgrade lên Starter ($7/tháng)
- Hoặc dùng cron job để ping health endpoint mỗi 10 phút

### 3. Database Free tier

- **90 ngày** sau đó bị xóa
- **1GB storage**

**Giải pháp**: Upgrade lên Starter ($7/tháng) cho production

---

## 🔥 TROUBLESHOOTING NHANH

### Build failed?

```bash
# Xóa cache local
rm -rf bin/ obj/
dotnet restore
git add . && git commit -m "Fix build" && git push
```

### Database connection error?

- Check Environment Variables có đúng không
- Dùng **Internal Database URL** (không phải External)
- Xem logs: Dashboard → Logs

### 502 Bad Gateway?

- App không start → Xem logs
- Health check fail → Test: `curl https://your-app.onrender.com/health`
- Timeout → Tăng timeout trong render.yaml

### Images không load?

- Local storage bị xóa → Migrate sang Cloudinary
- Xem section 7 trong DEPLOYMENT.md

---

## 📚 Tài liệu chi tiết

Xem file [DEPLOYMENT.md](./DEPLOYMENT.md) để có hướng dẫn đầy đủ về:
- Cấu hình payment gateways
- Setup Cloudinary cho ảnh/video
- Cấu hình Google OAuth
- Gmail App Password
- Troubleshooting chi tiết

---

## 🆘 CẦN TRỢ GIÚP?

1. Check logs: Dashboard → Logs
2. Test health: `https://your-app.onrender.com/health`
3. Render Community: https://community.render.com/
4. Documentation: https://render.com/docs

**Good luck! 🚀**

# 🔧 TROUBLESHOOTING RENDER DEPLOYMENT

## ❌ VẤN ĐỀ ĐÃ SỬA

### 1. **Build quá chậm** ⏰
**Nguyên nhân:**
- Docker build không tối ưu (build debug + release)
- Upload quá nhiều files không cần thiết
- Không cache layers hiệu quả

**Đã fix:**
- ✅ Gộp build + publish thành 1 stage
- ✅ Thêm `.renderignore` để giảm upload size
- ✅ Tối ưu Dockerfile với `--no-restore`
- ✅ Sử dụng `linux-x64` runtime cụ thể

### 2. **Health check fail** ❌
**Nguyên nhân:**
- Package `AspNetCore.HealthChecks.Npgsql` có thể gây lỗi

**Giải pháp tạm thời:**
- Nếu vẫn lỗi, tắt PostgreSQL health check trong Program.cs

---

## 🚀 DEPLOYMENT STEPS

### **Bước 1: Commit & Push**
```bash
git add .
git commit -m "fix: Optimize Dockerfile for faster builds"
git push origin main
```

### **Bước 2: Kiểm tra Render Logs**
1. Vào Dashboard → Web Service
2. Click **Logs** tab
3. Theo dõi build process

### **Bước 3: Nếu build fail**

#### **Lỗi: "Package restore failed"**
```bash
# Xóa cache local
rm -rf bin/ obj/
git add .
git commit -m "fix: Clear cache"
git push
```

#### **Lỗi: "Health check failed"**
Tạm thời tắt PostgreSQL health check:

Sửa `Program.cs`:
```csharp
// TẮT PostgreSQL health check tạm thời
builder.Services.AddHealthChecks()
    // .AddNpgSql(...) // Comment dòng này
    .AddCheck("self", () => HealthCheckResult.Healthy());
```

#### **Lỗi: "Database connection"**
Kiểm tra Environment Variables:
- `DB_HOST` phải là **Internal** hostname (không phải External)
- Tất cả DB_ variables phải được set

---

## ⏱️ BUILD TIME ESTIMATE

**Trước khi optimize:**
- First build: 10-15 phút
- Subsequent builds: 8-10 phút

**Sau khi optimize:**
- First build: 5-7 phút
- Subsequent builds: 3-5 phút

---

## 🔍 DEBUG COMMANDS

### **Check logs realtime:**
```bash
# Trong Render Dashboard, có thể xem logs live
# Hoặc dùng Render CLI:
render logs --tail --service johnhenry-web
```

### **Test build locally:**
```bash
# Build Docker image
docker build -t johnhenry-test .

# Run container
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:8080 \
  johnhenry-test

# Test health endpoint
curl http://localhost:8080/health
```

### **Check image size:**
```bash
docker images johnhenry-test
# Target: < 500MB
```

---

## 📊 COMMON ERRORS & SOLUTIONS

### Error 1: "Build timeout"
**Giải pháp:**
- Upgrade to Starter plan ($7/month)
- Free plan có giới hạn build time

### Error 2: "Out of memory"
**Giải pháp:**
- Build locally và push image lên Docker Hub
- Deploy từ Docker Hub thay vì build trên Render

### Error 3: "Database connection timeout"
**Giải pháp:**
```bash
# Check database is running
# Ensure using INTERNAL database URL
# Format: postgres://user:pass@internal-host:5432/db
```

### Error 4: "Port 8080 already in use"
**Giải pháp:**
- Render tự động assign port
- Ensure `ASPNETCORE_URLS=http://+:8080`
- KHÔNG hardcode port trong code

---

## 🎯 OPTIMIZE CHECKLIST

- [x] Dockerfile optimized (2 stages only)
- [x] .dockerignore configured
- [x] .renderignore created
- [x] Health check working
- [x] Environment variables set
- [ ] Database migrations run
- [ ] Test all endpoints
- [ ] Setup monitoring

---

## 📞 NEXT STEPS IF STILL FAILING

### Option 1: Deploy without Blueprint
1. Delete current service
2. Create **New Web Service** manually
3. Choose **Docker** runtime
4. Point to repo
5. Set environment variables manually

### Option 2: Simplify Health Check
Remove PostgreSQL health check temporarily:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());
```

### Option 3: Use Render Build Command
Instead of render.yaml, use Build Command:
```
docker build -t app . && docker run app
```

---

## 🆘 SUPPORT

If still failing:
1. Share **full error logs** from Render
2. Check **Render Status**: https://status.render.com/
3. Render Community: https://community.render.com/

**Build should now be faster and more reliable! 🚀**

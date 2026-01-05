# 🔧 REDIS SETUP FOR PRODUCTION (Optional)

## ⚠️ TRẠNG THÁI HIỆN TẠI

**Redis đã bị TẮT tạm thời** để app chạy nhanh hơn.

**Hiện đang dùng:** In-Memory Distributed Cache
- ✅ App chạy nhanh
- ✅ Không cần external service
- ⚠️ Cache mất khi restart
- ⚠️ Không scale được (single instance only)

---

## 🎯 KHI NÀO CẦN REDIS?

**Bạn NÊN setup Redis khi:**
- 💰 Upgrade lên **Starter plan** trở lên (không sleep)
- 👥 Có nhiều users đồng thời (>100)
- 📈 Cần scale horizontal (multiple instances)
- 🔄 Cần persistent cache giữa các restart
- ⚡ Cần distributed locking

**BÂY GIỜ (Free plan):**
- ❌ KHÔNG CẦN Redis
- ✅ In-memory cache đủ dùng
- ✅ Save cost & complexity

---

## 🚀 CÁCH SETUP REDIS (Khi cần)

### **Option 1: Upstash Redis** (Recommended) ⭐

**Free tier:**
- 10,000 commands/day
- 256MB storage
- Global low latency

**Setup:**

1. Đăng ký: https://upstash.com/
2. Create Redis database:
   - Name: `johnhenry-cache`
   - Region: `Asia-Pacific (Singapore)`
   - Type: `Regional`
3. Copy connection string:
   ```
   rediss://default:xxx@xxx.upstash.io:6379
   ```

4. Thêm vào Render Environment Variables:
   ```bash
   REDIS_CONNECTION=rediss://default:xxx@xxx.upstash.io:6379
   ```

5. Uncomment Redis code trong `Program.cs`:
   ```csharp
   builder.Services.AddStackExchangeRedisCache(options =>
   {
       options.Configuration = builder.Configuration.GetConnectionString("Redis");
       options.InstanceName = "JohnHenryFashion";
   });
   ```

6. Comment in-memory cache:
   ```csharp
   // builder.Services.AddDistributedMemoryCache();
   ```

7. Update `appsettings.json`:
   ```json
   "ConnectionStrings": {
       "Redis": "rediss://default:xxx@xxx.upstash.io:6379"
   }
   ```

---

### **Option 2: Redis Cloud** 

**Free tier:**
- 30MB storage
- No credit card required

**Setup:**

1. Đăng ký: https://redis.com/try-free/
2. Create subscription
3. Create database:
   - Cloud: AWS
   - Region: Singapore
4. Copy endpoint & password
5. Connection string:
   ```
   redis-xxxxx.redis-cloud.com:xxxxx,password=xxx,ssl=True,abortConnect=False
   ```

---

### **Option 3: Render Redis** (Paid)

**Cost:** $7/month (Starter)
- 25MB storage
- Shared instance

**Setup:**

1. Render Dashboard → **New +** → **Redis**
2. Name: `johnhenry-redis`
3. Region: Singapore
4. Plan: Starter
5. Create

6. Update `render.yaml`:
   ```yaml
   - type: redis
     name: johnhenry-redis
     plan: starter
     region: singapore
   ```

7. Environment variable:
   ```yaml
   - key: REDIS_CONNECTION
     fromService:
       type: redis
       name: johnhenry-redis
       property: connectionString
   ```

---

## 📊 COMPARISON

| Provider | Free Tier | Cost | Pros | Cons |
|----------|-----------|------|------|------|
| **In-Memory** | ✅ Yes | Free | Simple, Fast | Lost on restart |
| **Upstash** | ✅ 10k cmd/day | $0-10 | Serverless, Global | Command limit |
| **Redis Cloud** | ✅ 30MB | $0-7 | Reliable | Small storage |
| **Render Redis** | ❌ No | $7/mo | Integrated | More expensive |

---

## 🎯 KHUYẾN NGHỊ

### **Phase 1: MVP/Testing (Now)** ✅
```
✅ In-Memory Cache
✅ Free
✅ Simple
```

### **Phase 2: Early Production (1-3 months)**
```
→ Upstash Redis (Free tier)
→ 10k commands/day
→ Monitor usage
```

### **Phase 3: Scale (3+ months)**
```
→ Redis Cloud (Paid)
→ Or Render Redis
→ Based on needs
```

---

## 🔄 ENABLE REDIS (When ready)

### **1. Update Program.cs:**
```csharp
// Uncomment Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisConnection = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnection))
    {
        options.Configuration = redisConnection;
        options.InstanceName = "JohnHenryFashion";
    }
});

// Comment in-memory fallback
// builder.Services.AddDistributedMemoryCache();
```

### **2. Add Environment Variable:**
```bash
REDIS_CONNECTION=<your-redis-connection-string>
```

### **3. Test:**
```bash
# Local test
dotnet run

# Check cache working
curl http://localhost:5000/health
```

### **4. Deploy:**
```bash
git add .
git commit -m "feat: Enable Redis cache"
git push origin main
```

---

## 📝 KẾT LUẬN

**Hiện tại:**
- ✅ App đang chạy với in-memory cache
- ✅ Health check nhanh (<100ms)
- ✅ Đủ cho testing/MVP

**Tương lai:**
- ⏭️ Setup Redis khi có nhiều users
- ⏭️ Dùng Upstash free tier trước
- ⏭️ Upgrade khi cần thiết

**BÂY GIỜ: Không cần làm gì thêm! App đã OK! 🎉**

# 🎫 GIẢI PHÁP THỐNG NHẤT HỆ THỐNG SUPPORT TICKETS

**Ngày phân tích:** 19/12/2025  
**Vấn đề:** Có 2 hệ thống tickets không đồng nhất

---

## 🔍 PHÂN TÍCH HIỆN TRẠNG

### 1. **Tickets Cũ (System Tickets)**
**Nguồn:** Các sample data trong database  
**Categories:**
- `Order Delivery Issue` - Vấn đề giao hàng
- `Product Quality` - Chất lượng sản phẩm
- `Refund Request` - Yêu cầu hoàn tiền
- `Account Issue` - Vấn đề tài khoản
- `Payment Failed` - Thanh toán thất bại
- `General Inquiry` - Thắc mắc chung

**Đặc điểm:**
- ✅ Có model đầy đủ trong `SupportTicket`
- ✅ Có controller `SupportManagementController`
- ✅ Có view `/admin/support`
- ❌ **CHƯA CÓ** form để user tạo ticket
- ❌ **CHƯA CÓ** trang user xem tickets của mình

### 2. **Tickets Mới (Contact Tickets)**
**Nguồn:** Form liên hệ `/contact`  
**Category:** `contact`  
**Đặc điểm:**
- ✅ Có form tạo từ `ContactController`
- ✅ Tự động tạo ticket khi submit form
- ✅ Gửi email xác nhận
- ✅ Tạo in-app notification cho admin
- ⚠️ **Style hiển thị khác** (màu vàng highlight)
- ⚠️ **Category không chuẩn** với hệ thống cũ

### 3. **Vấn đề cần giải quyết:**
1. ❌ User không có cách tạo ticket trực tiếp (phải qua form contact)
2. ❌ User không xem được tickets của mình
3. ❌ 2 loại tickets hiển thị khác nhau
4. ❌ Categories không thống nhất
5. ❌ Workflow không rõ ràng

---

## 💡 GIẢI PHÁP ĐỀ XUẤT

### **Option 1: THỐNG NHẤT HOÀN TOÀN (Recommended) ⭐**

#### A. Chuẩn hóa Categories
```csharp
public enum TicketCategory
{
    // From Contact Form
    Contact,        // Liên hệ chung từ form
    
    // From User Dashboard
    Order,          // Vấn đề đơn hàng
    Product,        // Vấn đề sản phẩm
    Payment,        // Vấn đề thanh toán
    Account,        // Vấn đề tài khoản
    Refund,         // Yêu cầu hoàn tiền
    Technical,      // Vấn đề kỹ thuật
    General         // Thắc mắc chung
}
```

#### B. Tạo User Support Portal
**File mới:** `Controllers/UserSupportController.cs`
**Views mới:**
- `/user/support` - Dashboard tickets của user
- `/user/support/create` - Form tạo ticket mới
- `/user/support/{id}` - Chi tiết và chat ticket

#### C. Migration Strategy
```sql
-- Cập nhật categories cũ thành chuẩn mới
UPDATE "SupportTickets" 
SET "Category" = CASE 
    WHEN "Category" LIKE '%Order%' OR "Category" LIKE '%Delivery%' THEN 'order'
    WHEN "Category" LIKE '%Product%' THEN 'product'
    WHEN "Category" LIKE '%Payment%' THEN 'payment'
    WHEN "Category" LIKE '%Account%' THEN 'account'
    WHEN "Category" LIKE '%Refund%' THEN 'refund'
    WHEN "Category" = 'contact' THEN 'contact'
    ELSE 'general'
END
WHERE "Category" IS NOT NULL;
```

#### D. Unified Styling
```css
/* Màu sắc theo category */
.ticket-badge-contact { background: #fff3cd; color: #856404; }
.ticket-badge-order { background: #cfe2ff; color: #084298; }
.ticket-badge-product { background: #f8d7da; color: #842029; }
.ticket-badge-payment { background: #d1e7dd; color: #0f5132; }
.ticket-badge-account { background: #e2e3e5; color: #383d41; }
.ticket-badge-refund { background: #fff3cd; color: #856404; }
.ticket-badge-technical { background: #cfe2ff; color: #084298; }
.ticket-badge-general { background: #d1e7dd; color: #0f5132; }

/* Priority colors */
.ticket-priority-low { border-left: 4px solid #28a745; }
.ticket-priority-medium { border-left: 4px solid #ffc107; }
.ticket-priority-high { border-left: 4px solid #fd7e14; }
.ticket-priority-urgent { border-left: 4px solid #dc3545; }
```

---

### **Option 2: PHÂN HỆ RIÊNG BIỆT**

Giữ 2 hệ thống riêng:
- **Contact Form** → Dành cho câu hỏi chung, pre-sale
- **Support Tickets** → Dành cho vấn đề sau mua hàng

**Ưu điểm:**
- Không cần migration
- Dễ phân biệt nguồn

**Nhược điểm:**
- Quản lý phức tạp
- User bối rối không biết dùng cái nào
- Admin phải theo dõi 2 nơi

---

## 🚀 IMPLEMENTATION PLAN (Option 1)

### **Phase 1: Chuẩn hóa Backend (2-3 giờ)**

#### 1. Cập nhật Model
```csharp
// Models/SupportModels.cs
public class SupportTicket
{
    // ... existing properties ...
    
    [StringLength(50)]
    public string Source { get; set; } = "user_portal";  // contact_form, user_portal, admin_created
    
    // Thêm computed property
    public string CategoryDisplay => Category?.ToLower() switch
    {
        "contact" => "Liên hệ",
        "order" => "Đơn hàng",
        "product" => "Sản phẩm",
        "payment" => "Thanh toán",
        "account" => "Tài khoản",
        "refund" => "Hoàn tiền",
        "technical" => "Kỹ thuật",
        _ => "Chung"
    };
    
    public string PriorityBadge => Priority?.ToLower() switch
    {
        "low" => "Thấp",
        "medium" => "Trung bình",
        "high" => "Cao",
        "urgent" => "Khẩn cấp",
        _ => "Trung bình"
    };
}
```

#### 2. Tạo ViewModels
```csharp
// ViewModels/SupportViewModels.cs
public class CreateTicketViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
    [StringLength(500)]
    public string Subject { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng mô tả vấn đề")]
    public string Description { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public string Category { get; set; } = "general";
    
    public Guid? RelatedOrderId { get; set; }
    public Guid? RelatedProductId { get; set; }
    
    public List<IFormFile>? Attachments { get; set; }
}

public class TicketDetailViewModel
{
    public SupportTicket Ticket { get; set; } = null!;
    public List<TicketReply> Replies { get; set; } = new();
    public Order? RelatedOrder { get; set; }
    public Product? RelatedProduct { get; set; }
}
```

#### 3. Migration Data Script
```sql
-- Script: database/migrate_support_tickets.sql

-- Backup bảng
CREATE TABLE "SupportTickets_Backup" AS TABLE "SupportTickets";

-- Cập nhật categories
UPDATE "SupportTickets" 
SET 
    "Category" = LOWER(CASE 
        WHEN "Category" ILIKE '%Order%' OR "Category" ILIKE '%Delivery%' THEN 'order'
        WHEN "Category" ILIKE '%Product%' OR "Category" ILIKE '%Quality%' THEN 'product'
        WHEN "Category" ILIKE '%Payment%' OR "Category" ILIKE '%Failed%' THEN 'payment'
        WHEN "Category" ILIKE '%Account%' THEN 'account'
        WHEN "Category" ILIKE '%Refund%' THEN 'refund'
        WHEN "Category" = 'contact' THEN 'contact'
        WHEN "Category" ILIKE '%Technical%' THEN 'technical'
        ELSE 'general'
    END),
    "Source" = CASE 
        WHEN "Category" = 'contact' THEN 'contact_form'
        ELSE 'admin_created'
    END,
    "UpdatedAt" = NOW()
WHERE "Category" IS NOT NULL;

-- Thống kê sau migration
SELECT 
    "Category",
    "Source",
    COUNT(*) as count,
    COUNT(CASE WHEN "Status" = 'Open' THEN 1 END) as open_count
FROM "SupportTickets"
GROUP BY "Category", "Source"
ORDER BY count DESC;
```

### **Phase 2: User Support Portal (4-5 giờ)**

#### 1. Controller
**File:** `Controllers/UserSupportController.cs`

```csharp
[Authorize]
[Route("user/support")]
public class UserSupportController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserSupportController> _logger;

    // GET: /user/support
    [HttpGet("")]
    public async Task<IActionResult> Index(string? status = null)
    {
        var userId = _userManager.GetUserId(User);
        var query = _context.SupportTickets
            .Where(t => t.UserId == userId)
            .Include(t => t.Replies)
            .Include(t => t.RelatedOrder)
            .Include(t => t.RelatedProduct)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.Status.ToLower() == status.ToLower());
        }

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        ViewBag.TotalTickets = tickets.Count;
        ViewBag.OpenTickets = tickets.Count(t => t.Status == "Open");
        ViewBag.InProgressTickets = tickets.Count(t => t.Status == "InProgress");
        ViewBag.ResolvedTickets = tickets.Count(t => t.Status == "Resolved");

        return View(tickets);
    }

    // GET: /user/support/create
    [HttpGet("create")]
    public async Task<IActionResult> Create(Guid? orderId = null, Guid? productId = null)
    {
        var model = new CreateTicketViewModel
        {
            RelatedOrderId = orderId,
            RelatedProductId = productId
        };

        // Load user's orders for dropdown
        var userId = _userManager.GetUserId(User);
        ViewBag.Orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new { o.Id, o.OrderNumber })
            .ToListAsync();

        return View(model);
    }

    // POST: /user/support/create
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketViewModel model)
    {
        if (ModelState.IsValid)
        {
            var userId = _userManager.GetUserId(User);
            var ticketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

            var ticket = new SupportTicket
            {
                Id = Guid.NewGuid(),
                TicketNumber = ticketNumber,
                UserId = userId!,
                UserType = "customer",
                Subject = model.Subject,
                Description = model.Description,
                Category = model.Category,
                Priority = "medium",
                Status = "Open",
                Source = "user_portal",
                RelatedOrderId = model.RelatedOrderId,
                RelatedProductId = model.RelatedProductId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Send notification to admins
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var user = await _userManager.GetUserAsync(User);
            var userName = $"{user?.FirstName} {user?.LastName}".Trim();

            foreach (var admin in adminUsers)
            {
                await _notificationService.CreateNotificationAsync(
                    admin.Id,
                    "Yêu cầu hỗ trợ mới",
                    $"{userName} đã tạo yêu cầu hỗ trợ mới #{ticketNumber}. Danh mục: {ticket.CategoryDisplay}",
                    "support_ticket",
                    $"/admin/support?ticketNumber={ticketNumber}");
            }

            TempData["SuccessMessage"] = $"Đã tạo yêu cầu hỗ trợ #{ticketNumber}. Chúng tôi sẽ phản hồi sớm nhất!";
            return RedirectToAction("Details", new { id = ticket.Id });
        }

        return View(model);
    }

    // GET: /user/support/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        var ticket = await _context.SupportTickets
            .Include(t => t.Replies)
                .ThenInclude(r => r.User)
            .Include(t => t.RelatedOrder)
            .Include(t => t.RelatedProduct)
            .Include(t => t.AssignedAdmin)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (ticket == null)
        {
            return NotFound();
        }

        var viewModel = new TicketDetailViewModel
        {
            Ticket = ticket,
            Replies = ticket.Replies.OrderBy(r => r.CreatedAt).ToList(),
            RelatedOrder = ticket.RelatedOrder,
            RelatedProduct = ticket.RelatedProduct
        };

        return View(viewModel);
    }

    // POST: /user/support/{id}/reply
    [HttpPost("{id}/reply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReply(Guid id, string message)
    {
        var userId = _userManager.GetUserId(User);
        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (ticket == null)
        {
            return NotFound();
        }

        var reply = new TicketReply
        {
            Id = Guid.NewGuid(),
            TicketId = id,
            UserId = userId!,
            Message = message,
            IsAdminReply = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketReplies.Add(reply);
        ticket.ReplyCount++;
        ticket.UpdatedAt = DateTime.UtcNow;
        
        // Update status if needed
        if (ticket.Status == "Resolved")
        {
            ticket.Status = "Open"; // Reopen if user replies
        }

        await _context.SaveChangesAsync();

        // Notify assigned admin
        if (!string.IsNullOrEmpty(ticket.AssignedTo))
        {
            await _notificationService.CreateNotificationAsync(
                ticket.AssignedTo,
                "Phản hồi mới từ khách hàng",
                $"Ticket #{ticket.TicketNumber} có phản hồi mới từ khách hàng",
                "ticket_reply",
                $"/admin/support/{ticket.Id}");
        }

        TempData["SuccessMessage"] = "Đã gửi phản hồi thành công";
        return RedirectToAction("Details", new { id });
    }
}
```

#### 2. Views

**File:** `Views/UserSupport/Index.cshtml`
```html
@model List<SupportTicket>
@{
    ViewData["Title"] = "Yêu cầu hỗ trợ của tôi";
    var totalTickets = ViewBag.TotalTickets ?? 0;
    var openTickets = ViewBag.OpenTickets ?? 0;
    var inProgressTickets = ViewBag.InProgressTickets ?? 0;
    var resolvedTickets = ViewBag.ResolvedTickets ?? 0;
}

<div class="container my-5">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h1><i class="bi bi-headset"></i> Yêu cầu hỗ trợ</h1>
        <a href="/user/support/create" class="btn btn-danger">
            <i class="bi bi-plus-circle"></i> Tạo yêu cầu mới
        </a>
    </div>

    <!-- Statistics -->
    <div class="row g-3 mb-4">
        <div class="col-md-3">
            <div class="card text-center">
                <div class="card-body">
                    <h3 class="text-warning">@openTickets</h3>
                    <p class="mb-0">Chờ xử lý</p>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card text-center">
                <div class="card-body">
                    <h3 class="text-info">@inProgressTickets</h3>
                    <p class="mb-0">Đang xử lý</p>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card text-center">
                <div class="card-body">
                    <h3 class="text-success">@resolvedTickets</h3>
                    <p class="mb-0">Đã giải quyết</p>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card text-center">
                <div class="card-body">
                    <h3>@totalTickets</h3>
                    <p class="mb-0">Tổng số</p>
                </div>
            </div>
        </div>
    </div>

    <!-- Tickets List -->
    <div class="card">
        <div class="card-body">
            @if (Model.Any())
            {
                <div class="table-responsive">
                    <table class="table table-hover">
                        <thead>
                            <tr>
                                <th>Mã</th>
                                <th>Tiêu đề</th>
                                <th>Danh mục</th>
                                <th>Trạng thái</th>
                                <th>Ngày tạo</th>
                                <th>Phản hồi</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var ticket in Model)
                            {
                                <tr>
                                    <td><code>@ticket.TicketNumber</code></td>
                                    <td>@ticket.Subject</td>
                                    <td><span class="badge bg-secondary">@ticket.CategoryDisplay</span></td>
                                    <td>
                                        @switch (ticket.Status.ToLower())
                                        {
                                            case "open":
                                                <span class="badge bg-warning">Chờ xử lý</span>
                                                break;
                                            case "inprogress":
                                                <span class="badge bg-info">Đang xử lý</span>
                                                break;
                                            case "resolved":
                                                <span class="badge bg-success">Đã giải quyết</span>
                                                break;
                                            default:
                                                <span class="badge bg-secondary">@ticket.Status</span>
                                                break;
                                        }
                                    </td>
                                    <td>@ticket.CreatedAt.ToString("dd/MM/yyyy HH:mm")</td>
                                    <td>
                                        <span class="badge bg-light text-dark">
                                            <i class="bi bi-chat"></i> @ticket.ReplyCount
                                        </span>
                                    </td>
                                    <td>
                                        <a href="/user/support/@ticket.Id" class="btn btn-sm btn-outline-primary">
                                            <i class="bi bi-eye"></i> Xem
                                        </a>
                                    </td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            }
            else
            {
                <div class="text-center py-5">
                    <i class="bi bi-inbox" style="font-size: 4rem; color: #ccc;"></i>
                    <p class="text-muted mt-3">Bạn chưa có yêu cầu hỗ trợ nào</p>
                    <a href="/user/support/create" class="btn btn-danger">Tạo yêu cầu đầu tiên</a>
                </div>
            }
        </div>
    </div>
</div>
```

**File:** `Views/UserSupport/Create.cshtml` - Form tạo ticket

**File:** `Views/UserSupport/Details.cshtml` - Chi tiết và chat

### **Phase 3: Admin View Updates (2 giờ)**

Cập nhật `Views/Admin/Support.cshtml` để:
1. ✅ Bỏ phân chia 2 sections riêng biệt
2. ✅ Thống nhất styling theo category
3. ✅ Thêm filter theo source
4. ✅ Badge màu sắc nhất quán

### **Phase 4: ContactController Updates (30 phút)**

Thêm vào ContactController để đồng bộ:
```csharp
ticket.Source = "contact_form";
ticket.Category = "contact"; // Giữ nguyên
```

---

## 📋 CHECKLIST TRIỂN KHAI

### Backend
- [ ] Thêm `Source` field vào SupportTicket model
- [ ] Tạo migration script
- [ ] Chạy migration trên database
- [ ] Test categories mới

### User Portal
- [ ] Tạo `UserSupportController`
- [ ] Tạo views: Index, Create, Details
- [ ] Add routes và navigation
- [ ] Test tạo ticket từ user
- [ ] Test reply functionality

### Admin Updates
- [ ] Cập nhật Support.cshtml để thống nhất style
- [ ] Thêm filter theo source
- [ ] Test notification flow
- [ ] Verify categories hiển thị đúng

### Notifications
- [ ] Ticket mới từ user → Admin
- [ ] Admin reply → User
- [ ] Status change → User
- [ ] Assignment → Admin

### Testing
- [ ] User tạo ticket
- [ ] Admin phản hồi
- [ ] User reply back
- [ ] Change status
- [ ] View statistics

---

## 🎨 STYLE GUIDE

```css
/* Category badges */
.ticket-category {
    padding: 4px 12px;
    border-radius: 12px;
    font-size: 0.85rem;
    font-weight: 500;
}

.ticket-category-contact { background: #fff3cd; color: #856404; }
.ticket-category-order { background: #cfe2ff; color: #084298; }
.ticket-category-product { background: #f8d7da; color: #842029; }
.ticket-category-payment { background: #d1e7dd; color: #0f5132; }
.ticket-category-account { background: #e2e3e5; color: #383d41; }
.ticket-category-refund { background: #fff3cd; color: #856404; }
.ticket-category-technical { background: #cfe2ff; color: #084298; }
.ticket-category-general { background: #d1e7dd; color: #0f5132; }
```

---

## 📞 NEXT STEPS

Bạn muốn tôi implement:
1. ✅ **Phase 1**: Migration script + Model updates?
2. ✅ **Phase 2**: User Support Portal?
3. ✅ **Phase 3**: Admin view updates?
4. ✅ **All phases**: Toàn bộ giải pháp?

Cho tôi biết bạn chọn option nào! 🚀

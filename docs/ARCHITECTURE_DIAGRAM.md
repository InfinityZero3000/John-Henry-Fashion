# Admin System Architecture Diagram

## System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                    JOHN HENRY ADMIN DASHBOARD                       │
│                        /admin (Entry Point)                         │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
            ┌───────▼───────┐              ┌────────▼────────┐
            │ Quick Actions │              │ Sidebar Menu    │
            │  (Dashboard)  │              │ (_AdminLayout)  │
            └───────┬───────┘              └────────┬────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
        ┌───────────────────────────┼───────────────────────────┐
        │           │               │               │           │
        ▼           ▼               ▼               ▼           ▼
```

## Module Structure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           5 ADMIN MODULES                                   │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│  PAYMENT         │  │ SUPPORT          │  │ MARKETING        │
│  Management      │  │  System          │  │  Management      │
├──────────────────┤  ├──────────────────┤  ├──────────────────┤
│ Route:           │  │ Route:           │  │ Route:           │
│ /admin/payments  │  │ /admin/support   │  │ /admin/marketing │
├──────────────────┤  ├──────────────────┤  ├──────────────────┤
│ • Transactions   │  │ • Tickets        │  │ • Banners        │
│ • Settlements    │  │ • Assignment     │  │ • Promotions     │
│ • Withdrawals    │  │ • Comments       │  │ • Emails         │
│ • Methods        │  │ • FAQs           │  │ • Push Notifs    │
│ • Statistics     │  │ • Analytics      │  │ • Analytics      │
├──────────────────┤  ├──────────────────┤  ├──────────────────┤
│ Green Theme      │  │ Orange Theme     │  │ Purple Theme     │
│ 7 Views          │  │ 7 Views          │  │ 15 Views         │
│ 12 Routes        │  │ 11 Routes        │  │ 21 Routes        │
└──────────────────┘  └──────────────────┘  └──────────────────┘

┌──────────────────┐  ┌──────────────────┐
│ PRODUCT          │  │ SYSTEM           │
│  Approval        │  │  Configuration   │
├──────────────────┤  ├──────────────────┤
│ Route:           │  │ Route:           │
│ /admin/approvals │  │ /admin/settings  │
├──────────────────┤  ├──────────────────┤
│ • Pending        │  │ • General        │
│ • Details        │  │ • Shipping       │
│ • Bulk Actions   │  │ • Email          │
│ • History        │  │ • Payment GW     │
│ • Statistics     │  │ • Categories     │
├──────────────────┤  ├──────────────────┤
│ Purple Theme     │  │ Cyan Theme       │
│ 4 Views          │  │ 6 Views          │
│ 9 Routes         │  │ 23 Routes        │
└──────────────────┘  └──────────────────┘
```

## Database Layer

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         APPLICATION DB CONTEXT                              │
│                         (Entity Framework Core)                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
        ┌───────────────────────────┼────────────────────────────────────┐
        │               │                 │               │              │
        ▼               ▼                 ▼               ▼              ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│   Payments   │ │ SupportTicket│ │  Marketing   │ │ ProductApprv │ │SystemConfig  │
│              │ │              │ │   Tables     │ │              │ │   Tables     │
├──────────────┤ ├──────────────┤ ├──────────────┤ ├──────────────┤ ├──────────────┤
│ • Id         │ │ • Id         │ │ • Banners    │ │ • Id         │ │ • Settings   │
│ • Amount     │ │ • Title      │ │ • Promotions │ │ • ProductId  │ │ • Shipping   │
│ • Status     │ │ • Priority   │ │ • Emails     │ │ • Status     │ │ • Email      │
│ • Gateway    │ │ • Status     │ │ • Pushes     │ │ • ReviewerId │ │ • PaymentGW  │
│ • UserId     │ │ • AgentId    │ │              │ │ • Reason     │ │              │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
        │               │               │               │               │
        └───────────────┴───────────────┴───────────────┴───────────────┘
                                    │
                                    ▼
                        ┌─────────────────────┐
                        │   PostgreSQL 15     │
                        │   Database Server   │
                        └─────────────────────┘
```

## Security Layer

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           AUTHENTICATION & AUTHORIZATION                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
            ┌───────▼──────┐              ┌─────────▼───────┐
            │ ASP.NET      │              │ Policy-Based    │
            │ Identity     │              │ Authorization   │
            └───────┬──────┘              └─────────┬───────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                        ┌───────────▼──────────┐
                        │ [Authorize(Roles =   │
                        │   UserRoles.Admin)]  │
                        └──────────────────────┘
                                    │
        ┌───────────────────────────┼───────────────────────────┐
        │           │               │               │           │
        ▼           ▼               ▼               ▼           ▼
    Payment     Support       Marketing       Approval      Settings
   Controller  Controller    Controller      Controller    Controller
```

## UI Layer Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
            ┌───────▼──────┐              ┌─────────▼───────┐
            │ _AdminLayout │              │  Dashboard      │
            │  (Master)    │              │  (Home)         │
            └───────┬──────┘              └─────────┬───────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
        ┌───────────────────────────┼──────────────────────────────────────┐
        │                │               │                │                │
        ▼                ▼               ▼                ▼                ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│  Payment     │ │  Support     │ │  Marketing   │ │  Approval    │ │  Settings    │
│  Views       │ │  Views       │ │  Views       │ │  Views       │ │  Views       │
├──────────────┤ ├──────────────┤ ├──────────────┤ ├──────────────┤ ├──────────────┤
│ Green        │ │ Orange       │ │ Purple       │ │ Purple       │ │ Cyan         │
│ Glassmorphic │ │ Glassmorphic │ │ Glassmorphic │ │ Glassmorphic │ │ Glassmorphic │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
        │               │               │               │               │
        └───────────────┴───────────────┴───────────────┴───────────────┘
                                    │
                        ┌───────────▼──────────┐
                        │ Bootstrap 5.3        │
                        │ Lucide Icons         │
                        │ Font Awesome         │
                        │ Chart.js             │
                        │ DataTables           │
                        └──────────────────────┘
```

## Request Flow Diagram

```
┌─────────────┐
│   Client    │
│  (Browser)  │
└──────┬──────┘
       │
       │ HTTP Request
       │ GET /admin/payments
       ▼
┌─────────────────────────────────────┐
│        ASP.NET Core Pipeline        │
├─────────────────────────────────────┤
│ 1. Routing Middleware               │
│    ↓                                │
│ 2. Authentication Middleware        │
│    ↓                                │
│ 3. Authorization Middleware         │
│    ↓                                │
│ 4. MVC Middleware                   │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  PaymentManagementController        │
├─────────────────────────────────────┤
│ • Check User Role (Admin)           │
│ • Inject Dependencies               │
│ • Execute Action Method             │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│     ApplicationDbContext            │
├─────────────────────────────────────┤
│ • Query Payments Table              │
│ • Apply Filters                     │
│ • Include Related Data              │
│ • Return Results (async)            │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│        PostgreSQL Database          │
├─────────────────────────────────────┤
│ • Execute SQL Query                 │
│ • Return Data                       │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   Controller (Process Data)         │
├─────────────────────────────────────┤
│ • Map to ViewModel                  │
│ • Add ViewData/ViewBag              │
│ • Return View()                     │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│        Razor View Engine            │
├─────────────────────────────────────┤
│ • Apply _AdminLayout.cshtml         │
│ • Render Payment/Index.cshtml       │
│ • Generate HTML                     │
└──────────────┬──────────────────────┘
               │
               │ HTTP Response
               │ (HTML + CSS + JS)
               ▼
       ┌──────────────┐
       │   Client     │
       │  (Browser)   │
       └──────────────┘
```

## Data Flow Diagram

```
                        ┌─────────────────┐
                        │   Admin User    │
                        └────────┬────────┘
                                 │
                    ┌────────────┼────────────┐
                    │                         │
            ┌───────▼──────┐          ┌───────▼──────┐
            │ View Actions │          │ CRUD Actions │
            │ (Read Only)  │          │ (Read/Write) │
            └───────┬──────┘          └──────┬───────┘
                    │                        │
                    └───────────┬────────────┘
                                │
                    ┌───────────▼──────────┐
                    │     Controller       │
                    │   (Business Logic)   │
                    └───────────┬──────────┘
                                │
                    ┌───────────▼──────────┐
                    │   DbContext          │
                    │   (Data Access)      │
                    └───────────┬──────────┘
                                │
                    ┌───────────▼──────────┐
                    │   Database           │
                    │   (PostgreSQL)       │
                    └──────────────────────┘
```

## Module Interaction Map

```
                        ┌─────────────────┐
                        │   Dashboard     │
                        │   (Central Hub) │
                        └────────┬────────┘
                                 │
        ┌────────────────────────┼─────────────────────────────────────┐
        │              │               │                │              │
        ▼              ▼               ▼                ▼              ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│  Payments   │ │   Support   │ │  Marketing  │ │  Approvals  │ │  Settings   │
└──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘
       │               │               │               │               │
       └───────────────┴───────┬───────┴───────────────┴───────────────┘
                               │
                    ┌──────────▼──────────┐
                    │  Shared Resources   │
                    ├─────────────────────┤
                    │ • Users             │
                    │ • Products          │
                    │ • Orders            │
                    │ • Notifications     │
                    └─────────────────────┘
```

## Technology Stack Visual

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           TECHNOLOGY STACK                                  │
└─────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                        FRONTEND LAYER                          │
├────────────────────────────────────────────────────────────────┤
│  Bootstrap 5.3  │  Lucide Icons  │  Font Awesome  │  Chart.js  │
│  jQuery         │  DataTables    │  Custom CSS    │  JS        │
└────────────────────────────────────────────────────────────────┘
                                │
┌────────────────────────────────────────────────────────────────┐
│                        BACKEND LAYER                           │
├────────────────────────────────────────────────────────────────┤
│  ASP.NET Core 9.0  │  C# 12.0  │  Razor Views  │  MVC Pattern. │
│  Entity Framework  │  LINQ     │  Dependency Injection         │
└────────────────────────────────────────────────────────────────┘
                                │
┌────────────────────────────────────────────────────────────────┐
│                      SECURITY LAYER                            │
├────────────────────────────────────────────────────────────────┤
│  ASP.NET Identity  │  Authorization Policies  │  CSRF Tokens   │
│  Role-based Auth   │  Encrypted Settings      │  HTTPS         │
└────────────────────────────────────────────────────────────────┘
                                │
┌────────────────────────────────────────────────────────────────┐
│                        DATA LAYER                              │
├────────────────────────────────────────────────────────────────┤
│  PostgreSQL 15     │  Entity Framework Core  │  Migrations     │
│  Npgsql Provider   │  LINQ to SQL            │  Seed Data      │
└────────────────────────────────────────────────────────────────┘
```

## Growth & Scalability

```
    Current State (v2.0)         →         Future State (v3.0)
                                           
┌─────────────────────┐           ┌─────────────────────────┐
│   5 Core Modules    │           │   8+ Extended Modules   │
│   • Payment         │           │   • Payment             │
│   • Support         │    →      │   • Support             │
│   • Marketing       │           │   • Marketing           │
│   • Approvals       │           │   • Approvals           │
│   • Settings        │           │   • Settings            │
└─────────────────────┘           │   • Analytics (New)     │
                                  │   • Reports (New)       │
                                  │   • Automation (New)    │
                                  └─────────────────────────┘
```

---

**Legend:**
- Green = Payment/Financial
- Orange = Support/Help
- Purple/Pink = Marketing/Creative
- Purple = Quality/Approval
- Cyan/Teal = Configuration/Technical


# Home Interface 
![alt text](image.png)

# John Henry Fashion Web Platform

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-9.0-512BD4?style=for-the-badge&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)

A comprehensive, enterprise-grade e-commerce platform built with modern web technologies for fashion retail business. This platform provides a complete solution for online fashion stores with advanced features including multi-vendor support, real-time analytics, payment processing, and robust security systems.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Technology Stack](#technology-stack)
- [Core Features](#core-features)
- [Authentication & Authorization](#authentication--authorization)
- [Performance & Optimization](#performance--optimization)
- [Security Features](#security-features)
- [API Documentation](#api-documentation)
- [Installation & Setup](#installation--setup)
- [Development Environment](#development-environment)
- [Database Schema](#database-schema)
- [Contributing](#contributing)
- [License](#license)

## Architecture Overview

This platform follows a modern **Model-View-Controller (MVC)** architecture with **Domain-Driven Design (DDD)** principles, implementing **Clean Architecture** patterns for maintainability and scalability.

### System Architecture Diagram

```
┌─────────────────┐    ┌───────────────────┐    ┌─────────────────┐
│   Client Side   │    │   Server Side     │    │   Data Layer    │
│                 │    │                   │    │                 │
│ • React/jQuery  │◄──►│ • ASP.NET Core    │◄──►│ • PostgreSQL    │
│ • Bootstrap 5   │    │ • Entity Framework│    │ • Redis Cache   │
│ • Responsive UI │    │ • Identity System │    │ • File Storage  │
└─────────────────┘    └───────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  External APIs  │     │   Middleware    │     │   Monitoring    │
│                 │     │                 │     │                 │
│ • Google OAuth  │     │ • Authentication│     │ • Serilog       │
│ • Payment APIs  │     │ • Authorization │     │ • App Insights  │
│ • Email Service │     │ • Security      │     │ • Performance   │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## Technology Stack

### Backend Technologies

![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-9.0-512BD4?style=flat-square&logo=dotnet) **ASP.NET Core 9.0**
- Modern web framework with high performance
- Cross-platform compatibility
- Built-in dependency injection

![Entity Framework](https://img.shields.io/badge/Entity_Framework_Core-9.0-512BD4?style=flat-square&logo=dotnet) **Entity Framework Core 9.0**
- Code-first database approach
- Advanced query optimization
- Migration management

![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?style=flat-square&logo=postgresql) **PostgreSQL 15**
- Advanced relational database
- JSON support for complex data
- High performance and reliability

![Redis](https://img.shields.io/badge/Redis-Latest-DC382D?style=flat-square&logo=redis) **Redis Cache**
- In-memory data structure store
- Session management
- High-performance caching

### Frontend Technologies

![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=flat-square&logo=bootstrap) **Bootstrap 5.3**
- Responsive design framework
- Modern UI components
- Mobile-first approach

![jQuery](https://img.shields.io/badge/jQuery-3.7-0769AD?style=flat-square&logo=jquery) **jQuery 3.7**
- DOM manipulation
- AJAX operations
- Event handling

![JavaScript](https://img.shields.io/badge/JavaScript-ES6+-F7DF1E?style=flat-square&logo=javascript&logoColor=black) **Modern JavaScript**
- ES6+ features
- Async/await patterns
- Module system

### Authentication & Security

![Google](https://img.shields.io/badge/Google_OAuth-2.0-4285F4?style=flat-square&logo=google) **Google OAuth 2.0**
- Social authentication
- Secure user registration
- Single sign-on capability

![JWT](https://img.shields.io/badge/JWT-Authentication-000000?style=flat-square&logo=jsonwebtokens) **JWT Authentication**
- Stateless authentication
- API security
- Token-based authorization

![Identity](https://img.shields.io/badge/ASP.NET_Identity-Core-512BD4?style=flat-square&logo=dotnet) **ASP.NET Core Identity**
- User management
- Role-based authorization
- Two-factor authentication

### DevOps & Deployment

![Docker](https://img.shields.io/badge/Docker-Containerization-2496ED?style=flat-square&logo=docker) **Docker**
- Application containerization
- Multi-container orchestration
- Development environment consistency

![Azure](https://img.shields.io/badge/Microsoft_Azure-Cloud-0078D4?style=flat-square&logo=microsoftazure) **Azure Cloud Services**
- Application hosting
- Database management
- CDN and storage

### Monitoring & Analytics

![Serilog](https://img.shields.io/badge/Serilog-Logging-1E90FF?style=flat-square) **Serilog**
- Structured logging
- Multiple output targets
- Performance monitoring

![Application Insights](https://img.shields.io/badge/Application_Insights-Monitoring-0078D4?style=flat-square&logo=microsoftazure) **Application Insights**
- Real-time monitoring
- Performance analytics
- Error tracking

## Core Features

### E-commerce Functionality

#### Product Management
- **Advanced Product Catalog**: Comprehensive product information management with multiple categories, variants, and pricing tiers
- **Inventory Tracking**: Real-time stock management with automated alerts and reorder points
- **Digital Asset Management**: Image optimization, multiple product photos, and digital asset storage
- **Product Variants**: Size, color, and style variations with individual SKU management
- **Pricing Engine**: Dynamic pricing, promotional discounts, and tier-based pricing strategies

#### Shopping Experience
- **Advanced Search & Filtering**: Full-text search with category, price, brand, and attribute filters
- **Shopping Cart**: Persistent cart across sessions with saved items and wishlist functionality
- **Checkout Process**: Streamlined multi-step checkout with guest and registered user options
- **Order Management**: Complete order lifecycle management from placement to delivery
- **Review & Rating System**: Customer reviews, ratings, and feedback management

### Multi-Vendor Platform

#### Seller Dashboard
- **Vendor Registration**: Complete seller onboarding with document verification
- **Product Listing Tools**: Bulk product upload, inventory management, and pricing controls
- **Order Fulfillment**: Order processing, shipping management, and tracking integration
- **Financial Dashboard**: Sales analytics, commission tracking, and payout management
- **Performance Metrics**: Seller performance tracking with KPIs and improvement suggestions

#### Commission Management
- **Flexible Commission Structure**: Configurable commission rates by category, seller, or product
- **Automated Calculations**: Real-time commission calculation and tracking
- **Payment Processing**: Automated seller payouts with detailed reporting
- **Financial Reporting**: Comprehensive financial reports for both platform and sellers

### Advanced Admin Features

#### Analytics & Reporting
- **Sales Analytics**: Real-time sales data, trend analysis, and forecasting
- **Customer Analytics**: User behavior analysis, customer segmentation, and retention metrics
- **Performance Monitoring**: System performance metrics, API response times, and error tracking
- **Business Intelligence**: Custom reports, data visualization, and actionable insights

#### Content Management
- **Blog Management**: SEO-optimized blog system with content scheduling and management
- **Static Page Management**: Dynamic page creation and management for policies, about us, etc.
- **Email Templates**: Customizable email templates for order confirmations, newsletters, etc.
- **SEO Management**: Meta tags, structured data, and search engine optimization tools

#### System Administration
- **User Management**: Complete user lifecycle management with role-based permissions
- **Security Monitoring**: Real-time security monitoring with threat detection and response
- **System Configuration**: Platform-wide settings, feature toggles, and configuration management
- **Audit Logging**: Comprehensive audit trails for all system activities

## Authentication & Authorization

### Multi-Layer Security System

#### User Authentication
```csharp
// Multiple authentication providers
services.AddAuthentication()
    .AddJwtBearer() // API authentication
    .AddGoogle()    // Social login
    .AddCookie();   // Web application
```

#### Role-Based Access Control
- **Customer Role**: Product browsing, purchasing, order tracking, reviews
- **Seller Role**: Product management, order fulfillment, sales analytics
- **Admin Role**: Complete system access, user management, system configuration
- **Super Admin**: Platform-wide controls, security settings, system maintenance

#### Security Features
- **Two-Factor Authentication**: TOTP-based 2FA for enhanced security
- **Password Policy**: Enforced strong password requirements with complexity rules
- **Account Lockout**: Automated account protection against brute force attacks
- **Session Management**: Secure session handling with automatic expiration
- **API Rate Limiting**: Protection against API abuse and DoS attacks

### Google OAuth Integration

Complete integration with Google OAuth 2.0 for seamless user authentication:

```csharp
services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = configuration["Authentication:Google:ClientId"];
        options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
        options.SaveTokens = true;
    });
```

## Performance & Optimization

### Caching Strategy

#### Multi-Level Caching
- **Redis Distributed Cache**: Session data, user preferences, and frequently accessed data
- **Memory Cache**: Application-level caching for static data and configuration
- **Response Caching**: HTTP response caching for improved page load times
- **Database Query Caching**: Entity Framework query result caching

#### Performance Optimizations
```csharp
// Response compression
services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Static file caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers[HeaderNames.CacheControl] = 
            "public,max-age=" + (60 * 60 * 24 * 365); // 1 year cache
    }
});
```

### Database Optimization

#### Query Optimization
- **Entity Framework Performance**: Optimized LINQ queries with projection and filtering
- **Database Indexing**: Strategic indexing for frequently queried columns
- **Connection Pooling**: Efficient database connection management
- **Batch Operations**: Bulk insert/update operations for improved performance

#### PostgreSQL Features
- **JSON Support**: Storing and querying complex data structures
- **Full-Text Search**: Advanced search capabilities with ranking
- **Partitioning**: Large table partitioning for improved query performance
- **Extensions**: PostGIS for location-based features, pg_stat_statements for monitoring

## Security Features

### Comprehensive Security Implementation

#### Data Protection
- **Encryption at Rest**: Database encryption for sensitive data
- **Encryption in Transit**: HTTPS/TLS for all communications
- **Data Masking**: Sensitive data protection in logs and error messages
- **GDPR Compliance**: Data privacy and user consent management

#### Application Security
```csharp
// Security headers
app.Use((context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    return next();
});
```

#### Monitoring & Alerting
- **Security Event Logging**: Comprehensive logging of security-related events
- **Intrusion Detection**: Real-time monitoring for suspicious activities
- **Vulnerability Scanning**: Regular security assessments and updates
- **Incident Response**: Automated alerting and response procedures

## API Documentation

### RESTful API Design

The platform provides a comprehensive RESTful API for third-party integrations and mobile applications.

#### API Features
- **Swagger Documentation**: Interactive API documentation with testing capabilities
- **Versioning**: API versioning for backward compatibility
- **Rate Limiting**: Request throttling to prevent abuse
- **Authentication**: JWT-based API authentication

#### Swagger Integration
```csharp
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "John Henry Fashion API", 
        Version = "v1",
        Description = "Comprehensive e-commerce API"
    });
});
```

### API Endpoints Overview

#### Product Management API
- `GET /api/products` - Retrieve product catalog with filtering
- `POST /api/products` - Create new product (Seller/Admin)
- `PUT /api/products/{id}` - Update product information
- `DELETE /api/products/{id}` - Remove product from catalog

#### Order Management API
- `GET /api/orders` - Retrieve order history
- `POST /api/orders` - Create new order
- `PUT /api/orders/{id}/status` - Update order status
- `GET /api/orders/{id}/tracking` - Get order tracking information

#### User Management API
- `POST /api/auth/login` - User authentication
- `POST /api/auth/register` - User registration
- `GET /api/users/profile` - Get user profile
- `PUT /api/users/profile` - Update user profile

## Installation & Setup

### Prerequisites

Ensure you have the following installed on your development machine:

- ![.NET](https://img.shields.io/badge/.NET-9.0_SDK-512BD4?style=flat-square&logo=dotnet) **.NET 9.0 SDK**
- ![Docker](https://img.shields.io/badge/Docker-Latest-2496ED?style=flat-square&logo=docker) **Docker & Docker Compose**
- ![Git](https://img.shields.io/badge/Git-Latest-F05032?style=flat-square&logo=git) **Git**

### Quick Start

1. **Clone the Repository**
```bash
git clone https://github.com/InfinityZero3000/John-Henry-Website.git
cd John-Henry-Website
```

2. **Start Database Services**
```bash
docker-compose up -d postgres redis
```

3. **Configure Application Settings**
```bash
cp appsettings.Development.json.example appsettings.Development.json
# Edit configuration file with your settings
```

4. **Apply Database Migrations**
```bash
dotnet ef database update
```

5. **Install Dependencies & Run**
```bash
dotnet restore
dotnet run
```

6. **Access the Application**
- Web Application: `https://localhost:5001`
- API Documentation: `https://localhost:5001/swagger`
- Admin Panel: `https://localhost:5001/admin`

### Configuration

#### Database Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=johnhenry_db;Username=johnhenry_user;Password=YourPassword",
    "Redis": "localhost:6379"
  }
}
```

#### Authentication Configuration
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    }
  },
  "JWT": {
    "SecretKey": "your-jwt-secret-key",
    "Issuer": "JohnHenryFashion",
    "Audience": "JohnHenryFashionUsers"
  }
}
```

## Development Environment

### Docker Development Setup

The project includes a complete Docker development environment:

```yaml
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: johnhenry_db
      POSTGRES_USER: johnhenry_user
      POSTGRES_PASSWORD: JohnHenry@2025!
    ports:
      - "5432:5432"

  redis:
    image: redis:alpine
    ports:
      - "6379:6379"

  pgadmin:
    image: dpage/pgadmin4
    environment:
      PGADMIN_DEFAULT_EMAIL: thefirestar312@gmail.com
      PGADMIN_DEFAULT_PASSWORD: admin123
    ports:
      - "8080:80"
```

### Development Tools

#### Database Management
- **pgAdmin**: Web-based PostgreSQL administration (`http://localhost:8080`)
- **Entity Framework Tools**: Migration and database management
- **Redis CLI**: Command-line interface for Redis operations

#### Code Quality
- **EditorConfig**: Consistent coding style
- **ESLint**: JavaScript code quality
- **SonarQube**: Code quality analysis
- **Unit Testing**: Comprehensive test coverage

### Project Structure

```
John-Henry-Website/
├── Controllers/           # MVC Controllers
│   ├── AdminController.cs
│   ├── ProductsController.cs
│   └── Api/              # API Controllers
├── Models/               # Domain Models
│   ├── DomainModels.cs
│   └── AdminModels.cs
├── Services/             # Business Logic Services
│   ├── AuthService.cs
│   ├── PaymentService.cs
│   └── EmailService.cs
├── Views/                # Razor Views
│   ├── Shared/
│   ├── Home/
│   └── Admin/
├── wwwroot/              # Static Assets
│   ├── css/
│   ├── js/
│   └── images/
├── Data/                 # Database Context
├── Migrations/           # EF Core Migrations
├── Middleware/           # Custom Middleware
├── EmailTemplates/       # Email Templates
└── docker-compose.yml    # Docker Configuration
```

## Database Schema

### Core Entities

#### User Management
- **ApplicationUser**: Extended ASP.NET Identity user with custom fields
- **UserProfile**: Additional user information and preferences
- **UserAddress**: Multiple address management for users
- **UserRole**: Role-based access control

#### Product Catalog
- **Product**: Core product information and specifications
- **Category**: Hierarchical product categorization
- **ProductVariant**: Size, color, and style variations
- **ProductImage**: Multiple product images and media
- **ProductReview**: Customer reviews and ratings

#### Order Management
- **Order**: Order header information and status
- **OrderItem**: Individual items within an order
- **OrderStatus**: Order lifecycle tracking
- **ShippingAddress**: Delivery address information
- **PaymentTransaction**: Payment processing records

#### Analytics & Reporting
- **PageView**: Website analytics and user behavior
- **SalesReport**: Aggregated sales data
- **UserActivity**: User interaction tracking
- **SystemMetric**: Performance monitoring data

### Database Relationships

```sql
-- Example table relationships
ApplicationUser ||--o{ Order : "Places"
Order ||--o{ OrderItem : "Contains"
Product ||--o{ OrderItem : "Ordered as"
Product ||--o{ ProductReview : "Has reviews"
Category ||--o{ Product : "Categorizes"
```

## Contributing

We welcome contributions to the John Henry Fashion Web Platform! Please follow these guidelines:

### Development Workflow

1. **Fork the Repository**
2. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Make Changes and Test**
4. **Submit a Pull Request**

### Code Standards

- Follow C# coding conventions
- Write unit tests for new features
- Update documentation for API changes
- Ensure all tests pass before submitting

### Pull Request Process

1. Update the README.md with details of changes if applicable
2. Update API documentation for any new endpoints
3. Increase version numbers following semantic versioning
4. Ensure the PR description clearly describes the changes

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Contact & Support

For questions, support, or business inquiries:

- **Website**: [John Henry Fashion](https://johnhenryfashion.com)
- **Email**: support@johnhenry.com
- **Documentation**: [Developer Docs](https://docs.johnhenryfashion.com)

### Community

- **GitHub Issues**: Report bugs and request features
- **Discussions**: Join community discussions
- **Wiki**: Additional documentation and guides

---

**Built with passion for modern e-commerce solutions. Star this repository if you find it useful!**

![GitHub stars](https://img.shields.io/github/stars/InfinityZero3000/John-Henry-Website?style=social)
![GitHub forks](https://img.shields.io/github/forks/InfinityZero3000/John-Henry-Website?style=social)
![GitHub watchers](https://img.shields.io/github/watchers/InfinityZero3000/John-Henry-Website?style=social)
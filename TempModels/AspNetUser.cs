using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class AspNetUser
{
    public string Id { get; set; } = null!;

    public string? CompanyName { get; set; }

    public string? BusinessLicense { get; set; }

    public string? TaxCode { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public bool IsApproved { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public string? Notes { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Avatar { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public bool LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public virtual ICollection<ActiveSession> ActiveSessions { get; set; } = new List<ActiveSession>();

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual ICollection<AnalyticsDatum> AnalyticsData { get; set; } = new List<AnalyticsDatum>();

    public virtual ICollection<AspNetUserClaim> AspNetUserClaims { get; set; } = new List<AspNetUserClaim>();

    public virtual ICollection<AspNetUserLogin> AspNetUserLogins { get; set; } = new List<AspNetUserLogin>();

    public virtual ICollection<AspNetUserToken> AspNetUserTokens { get; set; } = new List<AspNetUserToken>();

    public virtual ICollection<AuditLog> AuditLogTargetUsers { get; set; } = new List<AuditLog>();

    public virtual ICollection<AuditLog> AuditLogUsers { get; set; } = new List<AuditLog>();

    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();

    public virtual ICollection<CheckoutSession> CheckoutSessions { get; set; } = new List<CheckoutSession>();

    public virtual ICollection<ContactMessage> ContactMessages { get; set; } = new List<ContactMessage>();

    public virtual ICollection<ContentModeration> ContentModerationModeratedByNavigations { get; set; } = new List<ContentModeration>();

    public virtual ICollection<ContentModeration> ContentModerationSubmittedByNavigations { get; set; } = new List<ContentModeration>();

    public virtual ICollection<ConversionEvent> ConversionEvents { get; set; } = new List<ConversionEvent>();

    public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();

    public virtual ICollection<Dispute> DisputeCustomers { get; set; } = new List<Dispute>();

    public virtual ICollection<Dispute> DisputeResolvedByNavigations { get; set; } = new List<Dispute>();

    public virtual ICollection<Dispute> DisputeSellers { get; set; } = new List<Dispute>();

    public virtual ICollection<EmailCampaign> EmailCampaigns { get; set; } = new List<EmailCampaign>();

    public virtual ICollection<FlashSale> FlashSales { get; set; } = new List<FlashSale>();

    public virtual ICollection<MarketingBanner> MarketingBanners { get; set; } = new List<MarketingBanner>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<PageView> PageViews { get; set; } = new List<PageView>();

    public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();

    public virtual ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();

    public virtual ICollection<PaymentTransaction> PaymentTransactionSellers { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<PaymentTransaction> PaymentTransactionUsers { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<ProductApprovalHistory> ProductApprovalHistories { get; set; } = new List<ProductApprovalHistory>();

    public virtual ICollection<ProductApproval> ProductApprovalReviewedByNavigations { get; set; } = new List<ProductApproval>();

    public virtual ICollection<ProductApproval> ProductApprovalSellers { get; set; } = new List<ProductApproval>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<PushNotificationCampaign> PushNotificationCampaigns { get; set; } = new List<PushNotificationCampaign>();

    public virtual ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();

    public virtual ICollection<ReportTemplate> ReportTemplates { get; set; } = new List<ReportTemplate>();

    public virtual ICollection<SalesReport> SalesReports { get; set; } = new List<SalesReport>();

    public virtual ICollection<SecurityLog> SecurityLogs { get; set; } = new List<SecurityLog>();

    public virtual ICollection<SellerSettlement> SellerSettlementSellers { get; set; } = new List<SellerSettlement>();

    public virtual ICollection<SellerSettlement> SellerSettlementSettledByNavigations { get; set; } = new List<SellerSettlement>();

    public virtual ICollection<SellerStore> SellerStores { get; set; } = new List<SellerStore>();

    public virtual ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<SupportTicket> SupportTicketAssignedToNavigations { get; set; } = new List<SupportTicket>();

    public virtual ICollection<SupportTicket> SupportTicketUsers { get; set; } = new List<SupportTicket>();

    public virtual ICollection<SystemConfiguration> SystemConfigurations { get; set; } = new List<SystemConfiguration>();

    public virtual ICollection<SystemPromotion> SystemPromotions { get; set; } = new List<SystemPromotion>();

    public virtual ICollection<TicketReply> TicketReplies { get; set; } = new List<TicketReply>();

    public virtual ICollection<TwoFactorToken> TwoFactorTokens { get; set; } = new List<TwoFactorToken>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    public virtual ICollection<WithdrawalRequest> WithdrawalRequestProcessedByNavigations { get; set; } = new List<WithdrawalRequest>();

    public virtual ICollection<WithdrawalRequest> WithdrawalRequestSellers { get; set; } = new List<WithdrawalRequest>();

    public virtual ICollection<AspNetRole> Roles { get; set; } = new List<AspNetRole>();
}

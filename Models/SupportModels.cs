using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JohnHenryFashionWeb.Models
{
    /// <summary>
    /// Support ticket system for customer and seller support
    /// </summary>
    public class SupportTicket
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TicketNumber { get; set; } = string.Empty;  // e.g., TKT-2025-00001
        
        // Optional UserId - allows anonymous ticket submission
        public string? UserId { get; set; }
        
        [StringLength(20)]
        public string UserType { get; set; } = "customer";  // customer, seller
        
        [Required]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "general";  // order, product, payment, technical, account, other
        
        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "medium";  // low, medium, high, urgent
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "open";  // open, in-progress, waiting-response, resolved, closed
        
        public string? AssignedTo { get; set; }  // Admin user ID
        
        public Guid? RelatedOrderId { get; set; }  // Optional link to order
        public Guid? RelatedProductId { get; set; }  // Optional link to product
        
        public string? AttachmentUrls { get; set; }  // JSON array of file URLs
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public int ReplyCount { get; set; } = 0;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
        
        [ForeignKey("AssignedTo")]
        public ApplicationUser? AssignedAdmin { get; set; }
        
        [ForeignKey("RelatedOrderId")]
        public Order? RelatedOrder { get; set; }
        
        [ForeignKey("RelatedProductId")]
        public Product? RelatedProduct { get; set; }
        
        public ICollection<TicketReply> Replies { get; set; } = new List<TicketReply>();
    }

    /// <summary>
    /// Replies/messages in a support ticket conversation
    /// </summary>
    public class TicketReply
    {
        public Guid Id { get; set; }
        
        public Guid TicketId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public bool IsAdminReply { get; set; } = false;
        public bool IsInternal { get; set; } = false;  // Internal note only visible to admins
        
        public string? AttachmentUrls { get; set; }  // JSON array of file URLs
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("TicketId")]
        public SupportTicket Ticket { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
    }

    /// <summary>
    /// Dispute management for order-related conflicts
    /// </summary>
    public class Dispute
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string DisputeNumber { get; set; } = string.Empty;  // e.g., DSP-2025-00001
        
        public Guid OrderId { get; set; }
        
        [Required]
        public string CustomerId { get; set; } = string.Empty;
        
        public string? SellerId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = string.Empty;  // wrong_item, damaged, not_delivered, etc.
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "open";  // open, investigating, seller-response, resolved, closed, cancelled
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal DisputedAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundAmount { get; set; }
        
        public string? Resolution { get; set; }  // Final resolution description
        
        [StringLength(50)]
        public string? ResolutionType { get; set; }  // full_refund, partial_refund, replacement, rejected
        
        public string? ResolvedBy { get; set; }  // Admin user ID
        
        public string? EvidenceUrls { get; set; }  // JSON array of evidence files
        
        public string? SellerResponse { get; set; }
        public DateTime? SellerRespondedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;
        
        [ForeignKey("CustomerId")]
        public ApplicationUser Customer { get; set; } = null!;
        
        [ForeignKey("SellerId")]
        public ApplicationUser? Seller { get; set; }
        
        [ForeignKey("ResolvedBy")]
        public ApplicationUser? Resolver { get; set; }
    }

    /// <summary>
    /// FAQ items for self-service support
    /// </summary>
    public class FAQ
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Question { get; set; } = string.Empty;
        
        [Required]
        public string Answer { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "general";  // order, payment, shipping, account, product
        
        public bool IsActive { get; set; } = true;
        
        public int SortOrder { get; set; } = 0;
        
        public int ViewCount { get; set; } = 0;
        
        public int HelpfulCount { get; set; } = 0;
        public int NotHelpfulCount { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class SupportTicket
{
    public Guid Id { get; set; }

    public string TicketNumber { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? AssignedTo { get; set; }

    public Guid? RelatedOrderId { get; set; }

    public Guid? RelatedProductId { get; set; }

    public string? AttachmentUrls { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int ReplyCount { get; set; }

    public virtual AspNetUser? AssignedToNavigation { get; set; }

    public virtual Order? RelatedOrder { get; set; }

    public virtual Product? RelatedProduct { get; set; }

    public virtual ICollection<TicketReply> TicketReplies { get; set; } = new List<TicketReply>();

    public virtual AspNetUser User { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class TicketReply
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }

    public string UserId { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsAdminReply { get; set; }

    public bool IsInternal { get; set; }

    public string? AttachmentUrls { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SupportTicket Ticket { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}

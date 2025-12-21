using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class ContactMessage
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public bool IsReplied { get; set; }

    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RepliedAt { get; set; }

    public string? RepliedBy { get; set; }

    public string? UserId { get; set; }

    public virtual AspNetUser? User { get; set; }
}

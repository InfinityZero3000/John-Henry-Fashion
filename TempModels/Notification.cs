using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? ActionUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}

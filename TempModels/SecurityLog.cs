using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class SecurityLog
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}

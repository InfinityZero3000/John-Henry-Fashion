using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class AuditLog
{
    public Guid Id { get; set; }

    public string? UserId { get; set; }

    public string? TargetUserId { get; set; }

    public string Action { get; set; } = null!;

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; }

    public string? AdditionalData { get; set; }

    public virtual AspNetUser? TargetUser { get; set; }

    public virtual AspNetUser? User { get; set; }
}

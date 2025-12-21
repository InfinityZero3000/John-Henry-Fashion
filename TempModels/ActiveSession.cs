using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class ActiveSession
{
    public int Id { get; set; }

    public string SessionId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? DeviceType { get; set; }

    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastActivity { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}

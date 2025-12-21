using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class UserSession
{
    public Guid Id { get; set; }

    public string SessionId { get; set; } = null!;

    public string? UserId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? Duration { get; set; }

    public string UserAgent { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? DeviceType { get; set; }

    public string? Browser { get; set; }

    public string? OperatingSystem { get; set; }

    public string? Country { get; set; }

    public string? City { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PageView> PageViews { get; set; } = new List<PageView>();

    public virtual AspNetUser? User { get; set; }
}

using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class AnalyticsDatum
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? UserId { get; set; }

    public string SessionId { get; set; } = null!;

    public string? Source { get; set; }

    public string? Medium { get; set; }

    public string? Campaign { get; set; }

    public string? Data { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AspNetUser? User { get; set; }
}

using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class PageView
{
    public Guid Id { get; set; }

    public string? UserId { get; set; }

    public string SessionId { get; set; } = null!;

    public string Page { get; set; } = null!;

    public string? Referrer { get; set; }

    public string UserAgent { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public DateTime ViewedAt { get; set; }

    public int? TimeOnPage { get; set; }

    public string? ExitPage { get; set; }

    public string? Source { get; set; }

    public string? Medium { get; set; }

    public string? Campaign { get; set; }

    public Guid? SessionId1 { get; set; }

    public virtual UserSession? SessionId1Navigation { get; set; }

    public virtual AspNetUser? User { get; set; }
}

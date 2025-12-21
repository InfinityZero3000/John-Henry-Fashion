using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class EmailCampaign
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string HtmlContent { get; set; } = null!;

    public string? PlainTextContent { get; set; }

    public string TargetAudience { get; set; } = null!;

    public string? TargetSegmentCriteria { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public int TotalRecipients { get; set; }

    public int SentCount { get; set; }

    public int OpenCount { get; set; }

    public int ClickCount { get; set; }

    public int UnsubscribeCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public virtual AspNetUser? CreatedByNavigation { get; set; }
}

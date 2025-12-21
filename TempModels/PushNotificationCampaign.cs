using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class PushNotificationCampaign
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? ActionUrl { get; set; }

    public string TargetAudience { get; set; } = null!;

    public string? TargetUserIds { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public int TotalRecipients { get; set; }

    public int SentCount { get; set; }

    public int OpenCount { get; set; }

    public int ClickCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public virtual AspNetUser? CreatedByNavigation { get; set; }
}

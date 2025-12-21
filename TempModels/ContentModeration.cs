using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class ContentModeration
{
    public Guid Id { get; set; }

    public string ContentType { get; set; } = null!;

    public Guid ContentId { get; set; }

    public string Content { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? FlaggedReason { get; set; }

    public string? ModeratorNotes { get; set; }

    public string? SubmittedBy { get; set; }

    public string? ModeratedBy { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ModeratedAt { get; set; }

    public decimal? AutoModerationScore { get; set; }

    public bool AutoFlagged { get; set; }

    public virtual AspNetUser? ModeratedByNavigation { get; set; }

    public virtual AspNetUser? SubmittedByNavigation { get; set; }
}

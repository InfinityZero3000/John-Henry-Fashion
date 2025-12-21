using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class ReportTemplate
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string ReportType { get; set; } = null!;

    public string Format { get; set; } = null!;

    public string Frequency { get; set; } = null!;

    public string? Parameters { get; set; }

    public string? Configuration { get; set; }

    public bool IsActive { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastRunDate { get; set; }

    public DateTime? NextRunDate { get; set; }

    public string? CreatedByUserId { get; set; }

    public virtual AspNetUser? CreatedByUser { get; set; }
}

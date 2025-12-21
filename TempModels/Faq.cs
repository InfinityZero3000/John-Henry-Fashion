using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class Faq
{
    public Guid Id { get; set; }

    public string Question { get; set; } = null!;

    public string Answer { get; set; } = null!;

    public string Category { get; set; } = null!;

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public int ViewCount { get; set; }

    public int HelpfulCount { get; set; }

    public int NotHelpfulCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class SystemConfiguration
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsEncrypted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual AspNetUser? UpdatedByNavigation { get; set; }
}

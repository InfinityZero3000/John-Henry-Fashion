using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class EmailConfiguration
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = null!;

    public string SmtpHost { get; set; } = null!;

    public int SmtpPort { get; set; }

    public bool UseSsl { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FromEmail { get; set; } = null!;

    public string? FromName { get; set; }

    public bool IsActive { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

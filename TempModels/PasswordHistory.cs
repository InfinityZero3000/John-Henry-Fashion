using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class PasswordHistory
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}

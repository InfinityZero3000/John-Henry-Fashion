using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class TwoFactorToken
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string Token { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}

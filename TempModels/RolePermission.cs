using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class RolePermission
{
    public Guid Id { get; set; }

    public string RoleId { get; set; } = null!;

    public string Permission { get; set; } = null!;

    public string? Module { get; set; }

    public bool IsGranted { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }
}

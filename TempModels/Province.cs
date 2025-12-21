using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class Province
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? CodeName { get; set; }

    public virtual ICollection<District> Districts { get; set; } = new List<District>();
}

using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class Ward
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? CodeName { get; set; }

    public int DistrictId { get; set; }

    public virtual District District { get; set; } = null!;
}

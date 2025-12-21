using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public Guid? ParentId { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CategoryApprovalRule> CategoryApprovalRules { get; set; } = new List<CategoryApprovalRule>();

    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();

    public virtual Category? Parent { get; set; }

    public virtual ICollection<PlatformFeeConfiguration> PlatformFeeConfigurations { get; set; } = new List<PlatformFeeConfiguration>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

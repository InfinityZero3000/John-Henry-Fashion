using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class BlogPost
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Excerpt { get; set; }

    public string Content { get; set; } = null!;

    public string? FeaturedImageUrl { get; set; }

    public string Status { get; set; } = null!;

    public bool IsFeatured { get; set; }

    public int ViewCount { get; set; }

    public List<string>? Tags { get; set; }

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public Guid? CategoryId { get; set; }

    public string AuthorId { get; set; } = null!;

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual AspNetUser Author { get; set; } = null!;

    public virtual BlogCategory? Category { get; set; }
}

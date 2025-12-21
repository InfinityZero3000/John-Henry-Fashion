using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class ProductApproval
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string SellerId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ReviewNotes { get; set; }

    public string? RejectionReason { get; set; }

    public string? ReviewChecklist { get; set; }

    public string? Priority { get; set; }

    public string? ReviewedBy { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int RevisionCount { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductApprovalHistory> ProductApprovalHistories { get; set; } = new List<ProductApprovalHistory>();

    public virtual AspNetUser? ReviewedByNavigation { get; set; }

    public virtual AspNetUser Seller { get; set; } = null!;
}

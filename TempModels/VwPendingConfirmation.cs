using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class VwPendingConfirmation
{
    public Guid? Id { get; set; }

    public string? OrderNumber { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public decimal? TotalAmount { get; set; }

    public bool? IsSellerConfirmed { get; set; }

    public DateTime? SellerConfirmedAt { get; set; }

    public bool? IsUserConfirmedDelivery { get; set; }

    public DateTime? UserConfirmedDeliveryAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public string? CustomerEmail { get; set; }

    public string? PendingAction { get; set; }

    public decimal? DaysPending { get; set; }
}

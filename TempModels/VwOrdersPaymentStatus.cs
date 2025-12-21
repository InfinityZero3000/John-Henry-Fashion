using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class VwOrdersPaymentStatus
{
    public Guid? OrderId { get; set; }

    public string? OrderNumber { get; set; }

    public string? UserId { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public string? OrderStatus { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? CanShip { get; set; }

    public bool? CanComplete { get; set; }
}

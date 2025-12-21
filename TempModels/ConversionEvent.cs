using System;
using System.Collections.Generic;

namespace JohnHenryFashionWeb.TempModels;

public partial class ConversionEvent
{
    public Guid Id { get; set; }

    public string? UserId { get; set; }

    public string SessionId { get; set; } = null!;

    public string ConversionType { get; set; } = null!;

    public decimal Value { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Source { get; set; }

    public string? Medium { get; set; }

    public string? Campaign { get; set; }

    public Guid? OrderId { get; set; }

    public string? ProductIds { get; set; }

    public DateTime ConvertedAt { get; set; }

    public string? AdditionalData { get; set; }

    public virtual Order? Order { get; set; }

    public virtual AspNetUser? User { get; set; }
}

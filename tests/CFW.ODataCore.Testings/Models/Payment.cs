using CFW.Core.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace CFW.ODataCore.Testings.Models;

public class Payment : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public PaymentInfo PaymentInfo { get; set; } = default!;

    public Customer Customer { get; set; } = default!;

    public ICollection<Order> Orders { get; set; }
        = new List<Order>();
}

[ComplexType]
public class PaymentInfo
{
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? PaymentMethod { get; set; }
}

//EF core not support.
//[ComplexType]
//public class BillingInfo
//{
//    public string? BillingAddress { get; set; }
//    public string? BillingCity { get; set; }
//    public string? BillingState { get; set; }
//    public string? BillingZip { get; set; }
//    public string? BillingCountry { get; set; }
//}
using CFW.ODataCore.Models;
using CFW.ODataCore.Testings.Models;
using System.Linq.Expressions;

namespace CFW.ODataCore.Testings.Features.Payments;

[Entity<Payment>("payments")]
public class PaymentEndpointConfiguration : EntityEndpoint<Payment>
{
    public override Expression<Func<Payment, Payment>> Model
        => x => new Payment
        {
            Id = x.Id,
            Amount = x.Amount,
            PaymentDate = x.PaymentDate,
            PaymentMethod = x.PaymentMethod,
            PaymentInfo = x.PaymentInfo,
            Customer = x.Customer,
            Orders = x.Orders
        };
}
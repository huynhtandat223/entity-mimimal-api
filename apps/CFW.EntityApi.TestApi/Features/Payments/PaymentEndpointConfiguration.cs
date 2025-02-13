using CFW.EntityApi.Registrators;
using System.Linq.Expressions;

namespace CFW.EntityApi.TestApi.Features.Payments;

[Entity("payments")]
public class PaymentEndpointConfiguration
{
    public Expression<Func<Payment, Payment>> Model
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

    public void Register(ContainerConfiguration containerRegistrator)
    {
        throw new NotImplementedException();
    }
}
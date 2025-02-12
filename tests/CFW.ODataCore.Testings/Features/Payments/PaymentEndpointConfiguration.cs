using CFW.EntityApi.Registrators;
using CFW.EntityMinimalApi.Testings.Models;
using System.Linq.Expressions;

namespace CFW.EntityMinimalApi.Testings.Features.Payments;

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
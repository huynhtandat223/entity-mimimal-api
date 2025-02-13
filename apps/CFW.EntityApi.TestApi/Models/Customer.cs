using CFW.Core.Entities;

namespace CFW.EntityApi.TestApi.Models;

public class Customer : IEntity<int>
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public ICollection<Order>? Orders { get; set; } = new List<Order>();

    public Address? ShippingAddress { get; set; }

    public Address? BillingAddress { get; set; }
}

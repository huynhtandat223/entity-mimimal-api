using CFW.Core.Entities;

namespace CFW.EntityApi.TestApi.Models;

public class Address : IEntity<int>
{
    public int Id { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }
}

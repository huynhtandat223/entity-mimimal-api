using CFW.EntityApi.Models.Builders;

namespace CFW.ODataCore.Features.Endpoints;

public class ApiConfiguration : DbEntityApiConfiguration<Models.Endpoint, AppDbContext, Guid>
    , IEntityApiConfiguration<Models.Endpoint>
{
    public ApiConfiguration(AppDbContext db) : base(db) { }
}


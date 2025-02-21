using CFW.EntityApi.TestApi.Features.Entities.ViewModels;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;

namespace CFW.EntityApi.TestApi.Features.Entities;

public class Query
{
    private readonly AppDbContext _db;

    public Query(AppDbContext db)
    {
        _db = db;
    }

    public IQueryable<EntityViewModel> GetQueryable()
    {
        var model = _db.Model.GetEntityTypes();
        return model.Select(x => new EntityViewModel
        {
            Name = x.ClrType.Name
        }).AsQueryable();
    }
}

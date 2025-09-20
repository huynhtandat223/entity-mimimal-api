using CFW.AppHost.Features.Core;
using CFW.DynamicApi;
using CFW.DynamicApi.Interceptors.OData;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Databases.Endpoints;

public class DatabasesTableQuery
{
    public class Request
    {
        public string TableName { get; set; } = null!;
    }

    [ApiOperation("databases", RouteName = "tables/{tableName}/query"
        , Interceptors = new Type[] { typeof(ODataFeatureInterceptor) }
        , HttpMethod = OperationHttpMethod.GET)]
    public class Handler : IRequestHandler<Request, IQueryable>
    {
        private readonly AppDbContext _db;
        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IResult<IQueryable>> Handle(RequestModel<Request> request, CancellationToken cancellationToken)
        {
            var tableNameParts = request.Model.TableName.Split('.');
            var schema = tableNameParts.Length == 2 ? tableNameParts[0] : null;
            var tableName = tableNameParts.Length == 2 ? tableNameParts[1] : tableNameParts[0];
            IQueryable result = null!;

            var entity = _db.Model.GetEntityTypes()
                .FirstOrDefault(x => (schema.IsNullOrEmpty() || (schema.IsNotNullOrEmpty() && x.GetSchema().CompareIgnoreCase(schema)))
                    && x.GetTableName().CompareIgnoreCase(tableName));

            if (entity is null)
                return await Task.FromResult(result.Notfound());

            var methodInfo = typeof(Handler).GetMethod(nameof(GetSet), System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static)?
                .MakeGenericMethod(entity.ClrType);

            var queryable = methodInfo?.Invoke(null, [_db]) as IQueryable;

            return queryable.Success();
        }

        private static IQueryable GetSet<T>(DbContext db) where T : class
        {
            return db.Set<T>().AsNoTracking();
        }
    }
}

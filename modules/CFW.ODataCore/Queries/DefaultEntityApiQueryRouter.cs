using CFW.EntityApi.Registrators;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;

namespace CFW.EntityApi.Queries;

public class DefaultEntityApiQueryRouter<TEntity> : IEntityApiQueryRouter<TEntity>
    where TEntity : class
{
    public Task Register(ContainerMemberRegistrationContext containerMemberRegistrationContext)
    {
        var entityGroupBuider = containerMemberRegistrationContext.MemberRouteGroup;

        entityGroupBuider.MapGet("/", async (ODataOutputFormatter outputFormatter
        , HttpContext httpContext
        , IEntityApiQuery<TEntity> entityApiQuery
        , ODataOutputFormatter formatter
        , CancellationToken cancellationToken) =>
        {
            var odataFeature = containerMemberRegistrationContext.ODataFeature;
            if (odataFeature is null)
            {
                odataFeature = containerMemberRegistrationContext.CreateODataFeature<TEntity>(httpContext.RequestServices
                    , null, null);

                httpContext.Features.Set(odataFeature);
                var queryable = entityApiQuery.GetQueryable();

                var ignoreQueryOptions = AllowedQueryOptions.None;

                var odataQueryContext = new ODataQueryContext(odataFeature.Model, typeof(TEntity), odataFeature.Path);
                var options = new ODataQueryOptions<TEntity>(odataQueryContext, httpContext.Request);

                var result = options.ApplyTo(queryable, ignoreQueryOptions);

                var formatterContext = new OutputFormatterWriteContext(httpContext,
                    (stream, encoding) => new StreamWriter(stream, encoding),
                    result.GetType() ?? typeof(object), result)
                {
                    ContentType = "application/json;odata.metadata=none",
                };

                await formatter.WriteAsync(formatterContext);
            }
        }).WithMetadata(containerMemberRegistrationContext);

        return Task.CompletedTask;
    }
}

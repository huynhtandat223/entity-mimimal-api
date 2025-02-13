using CFW.Core.Utils;
using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.Registrators;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.EntityApi.Queries;

public class DefaultDbEntityApiQueryRouter<TDbContext, TEntity, TKey> : IDbEntityApiQueryRouter<TDbContext, TEntity, TKey>
where TDbContext : DbContext
where TEntity : class
{
    public IEntityType EntityType { get; set; } = null!;

    public IProperty KeyProperty { get; set; } = null!;

    public EntityApiConfiguration? EntityApiBuilder { get; set; }

    public Task Register(ContainerMemberRegistrationContext containerMemberRegistrationContext)
    {
        var entityGroupBuider = containerMemberRegistrationContext.MemberRouteGroup;
        var entityConfiguration = (EntityApiConfiguration<TEntity>)containerMemberRegistrationContext.EntityConfiguration;

        var allowQueryOptions = containerMemberRegistrationContext.EntityConfiguration?.AllowedQueryOptions
            ?? containerMemberRegistrationContext
            .ContainerRegistrationContext.ContainerConfiguration.AllowedQueryOptions;

        var ignoreQueryOptions = ~allowQueryOptions;

        var entityApiQueryFactory = entityConfiguration?.QueryFactory;
        if (entityApiQueryFactory is null)
        {
            entityApiQueryFactory = s =>
            {
                var db = s.GetRequiredService<TDbContext>();
                var queryable = db.Set<TEntity>().AsNoTracking().AsQueryable();

                var result = new DefaultEntityApiQuery<TEntity>(queryable);

                return Task.FromResult<IEntityApiQuery<TEntity>>(result);
            };
        }

        entityGroupBuider.MapGet("/", async (ODataOutputFormatter outputFormatter
        , HttpContext httpContext
        , ODataOutputFormatter formatter
        , CancellationToken cancellationToken) =>
        {
            var odataFeature = containerMemberRegistrationContext.ODataFeature;
            if (odataFeature is null)
            {
                odataFeature = containerMemberRegistrationContext
                .CreateODataFeature<TEntity>(httpContext.RequestServices
                    , EntityType, KeyProperty.PropertyInfo!);
            }

            httpContext.Features.Set(odataFeature);

            var entityApiQuery = await entityApiQueryFactory(httpContext.RequestServices);

            var queryBuilder = new QueryBuilder(httpContext.Request.Query);
            var maxTop = containerMemberRegistrationContext.ODataOptions.QueryConfigurations.MaxTop;

            //Maybe $top always support by Odata
            var availableTop = new string[] { "$top", "top" };

            var topQuery = httpContext.Request.Query.SingleOrDefault(x => availableTop.Contains(x.Key.ToLower().Trim()));

            if (topQuery.Key.IsNullOrWhiteSpace())
            {
                queryBuilder.Add("$top", maxTop!.Value.ToString());
            }
            else
            {
                if (!int.TryParse(topQuery.Value, out var topValue))
                {
                    queryBuilder.Add("$top", maxTop!.Value.ToString());
                }
                else if (topValue > maxTop!.Value)
                {
                    queryBuilder.Add("$top", maxTop!.Value.ToString());
                }
            }

            httpContext.Request.QueryString = queryBuilder.ToQueryString();

            var odataQueryContext = new ODataQueryContext(odataFeature.Model, typeof(TEntity), odataFeature.Path);
            var options = new ODataQueryOptions<TEntity>(odataQueryContext, httpContext.Request);

            var result = await entityApiQuery.ExecuteQuery(options, cancellationToken);

            var formatterContext = new OutputFormatterWriteContext(httpContext,
                (stream, encoding) => new StreamWriter(stream, encoding),
                result.GetType() ?? typeof(object), result)
            {
                ContentType = "application/json;odata.metadata=none",
            };

            await formatter.WriteAsync(formatterContext);

        }).WithMetadata(containerMemberRegistrationContext);

        return Task.CompletedTask;
    }
}
﻿using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Metadata;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;

namespace CFW.ODataCore.RouteMappers;

public class EntityGetByKeyRouteMapper<TSource> : IRouteMapper
    where TSource : class
{
    private readonly IRouteMapper _internalRouteMapper;
    public EntityGetByKeyRouteMapper(MetadataEntity metadata, IServiceScopeFactory serviceScopeFactory)
    {
        //TODO: consider to use lazy initialization
        if (metadata.KeyProperty is null)
        {
            using var scope = serviceScopeFactory.CreateScope();
            metadata.InitSourceMetadata(scope.ServiceProvider);
        }

        var internalRouteMapperType = typeof(EntityGetByKeyRouteMapper<,>)
            .MakeGenericType(typeof(TSource), metadata.KeyProperty!.PropertyInfo!.PropertyType);
        _internalRouteMapper = (IRouteMapper)Activator.CreateInstance(internalRouteMapperType, metadata)!;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        return _internalRouteMapper.MapRoutes(routeGroupBuilder);
    }
}

public class EntityGetByKeyRouteMapper<TSource, TKey> : IRouteMapper
    where TSource : class
{
    private readonly MetadataEntity _metadata;

    public EntityGetByKeyRouteMapper(MetadataEntity metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        var ignoreQueryOptions = _metadata.ODataQueryOptions.IgnoreQueryOptions;
        var keyPattern = this.GetKeyPattern<TKey>();

        routeGroupBuilder.MapGet(keyPattern, async (HttpContext httpContext
                , TKey key
                , ODataOutputFormatter formatter
                , CancellationToken cancellationToken) =>
        {

            var feature = _metadata.CreateOrGetODataFeature<TSource>(httpContext.RequestServices);
            httpContext.Features.Set(feature);

            var entityConfig = _metadata.GetOrCreateEndpointConfiguration<TSource>(httpContext.RequestServices);
            var queryable = await entityConfig.GetQueryable(httpContext.RequestServices);
            var predicate = ExpressionUtils.BuilderEqualExpression<TSource>(key!, _metadata.KeyProperty!.Name);

            queryable = queryable.Where(predicate);

            //apply query options
            var odataQueryContext = new ODataQueryContext(feature.Model, typeof(TSource), feature.Path);
            var options = new ODataQueryOptions<TSource>(odataQueryContext, httpContext.Request);
            var appliedQuery = options.ApplyTo(queryable, ignoreQueryOptions);

            var result = appliedQuery.Cast<object>().SingleOrDefault();

            //write response
            var formatterContext = new OutputFormatterWriteContext(httpContext,
                (stream, encoding) => new StreamWriter(stream, encoding),
                typeof(object), result)
            {
                ContentType = "application/json;odata.metadata=none",
            };

            await formatter.WriteAsync(formatterContext);

        }).WithName(_metadata.Name);

        return Task.CompletedTask;
    }
}
using CFW.ODataCore.Models;
using Humanizer;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.OData.ModelBuilder;

namespace CFW.ODataCore;

public class DbContextSetupOptions<TDbContext> : DbContextSetupOptions
    where TDbContext : DbContext
{
    public DbContextSetupOptions() : base(typeof(TDbContext))
    {
    }
}

public class AutoGenerationEndpointsOptions
{
    public Models.ODataQueryOptions QueryOptions { get; set; } = new Models.ODataQueryOptions
    {
        AllowedQueryOptions = AllowedQueryOptions.All,
    };

    public int NestedLevel { get; set; } = 1;

    public string? RoutePrefix { get; set; }

    public Func<IEntityType, string> RouteNameFormatter { get; set; }
        = (entityType) =>
        {
            var name = entityType.ClrType.Name;

            if (name.EndsWith("`1") || name.EndsWith("`2") || name.EndsWith("`3"))
                name = name.Substring(0, name.Length - 2);

            return name.Pluralize().Kebaberize();
        };
}

public class DbContextSetupOptions
{
    public DbContextSetupOptions(Type dbContextType)
    {
        DbContextType = dbContextType;
    }

    internal Type DbContextType { get; set; }

    public AutoGenerationEndpointsOptions? AutoGenerateEndpoints { get; set; }

    public string? DefaultRoutePrefix { get; set; }
}

public class EntityMimimalApiOptions
{
    public string DefaultRoutePrefix { get; set; } = Constants.DefaultODataRoutePrefix;

    [Obsolete("Use DbContextSetupOptions instead")]
    internal Type DefaultDbContext { get; set; } = default!;

    [Obsolete("Use DbContextSetupOptions instead")]
    internal ServiceLifetime DbServiceLifetime { get; set; } = ServiceLifetime.Scoped;

    internal Action<ODataOptions> ODataOptions { get; set; } = (options) => options.EnableQueryFeatures();

    internal MetadataContainerFactory MetadataContainerFactory { get; set; } = new MetadataContainerFactory();

    internal Action<ODataConventionModelBuilder>? ConfigureModelBuilder { get; set; }

    internal Action<RouteGroupBuilder>? ConfigureContainerRouteGroup { get; set; }

    internal DbContextSetupOptions? DbContextOptions { get; set; }

    public EntityMimimalApiOptions UseDefaultDbContext<TDbContext>(Action<DbContextSetupOptions<TDbContext>>? generationSetup = null)
        where TDbContext : DbContext
    {
        DefaultDbContext = typeof(TDbContext);
        DbServiceLifetime = ServiceLifetime.Scoped;

        var options = new DbContextSetupOptions<TDbContext>();
        generationSetup?.Invoke(options);

        DbContextOptions = options;

        return this;
    }

    /// <summary>
    /// Configue OData options
    /// </summary>
    /// <param name="odataOptions"></param>
    /// <returns></returns>
    public EntityMimimalApiOptions UseODataOptions(Action<ODataOptions> odataOptions)
    {
        ODataOptions = odataOptions;
        return this;
    }

    /// <summary>
    /// Assemply container hold necessary cached types for api generation, you can manually configue types to this container
    /// </summary>
    /// <param name="metadataContainerFactory"></param>
    /// <returns></returns>
    public EntityMimimalApiOptions UseMetadataContainerFactory(MetadataContainerFactory metadataContainerFactory)
    {
        MetadataContainerFactory = metadataContainerFactory;
        return this;
    }

    /// <summary>
    /// Configure OData model builder after all entities, operations configured
    /// </summary>
    /// <param name="configureModelBuilder"></param>
    /// <returns></returns>
    public EntityMimimalApiOptions ConfigureODataModelBuilder(Action<ODataConventionModelBuilder> configureModelBuilder)
    {
        ConfigureModelBuilder = configureModelBuilder;
        return this;
    }

    public EntityMimimalApiOptions ConfigureMinimalApiContainerRouteGroup(Action<RouteGroupBuilder> configureContainerRouteGroup)
    {
        ConfigureContainerRouteGroup = configureContainerRouteGroup;
        return this;
    }
}

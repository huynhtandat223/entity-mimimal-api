using CFW.ODataCore.Attributes;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

namespace CFW.ODataCore.Models.Metadata;

public record EntityEndpointKey
{
    public required string Name { get; init; }

    public required Type SourceType { get; init; }
}

public class MetadataEntity
{
    public required string Name { get; init; }

    public required Type SourceType { get; init; }

    public required ApiMethod[] Methods { get; init; }

    public required MetadataContainer Container { get; init; }

    public required ODataQueryOptions ODataQueryOptions { get; init; }

    public required EntityHandlerAttribute[] HandlerAttributes { get; init; }

    public required Type? DbContextType { get; init; }

    public IList<MetadataEntityAction> Operations { get; } = new List<MetadataEntityAction>();

    public int NestedLevel { get; set; } = 1;

    private static object _lockToken = new();
    private IODataFeature? _cachedFeature;
    public IODataFeature CreateOrGetODataFeature<TSource>(IServiceProvider serviceProvider)
        where TSource : class
    {
        if (_cachedFeature is not null)
            return _cachedFeature;

        if (SourceType != typeof(TSource))
            throw new InvalidOperationException($"Invalid source type {SourceType} for {typeof(TSource)}");

        lock (_lockToken)
        {
            // Double-check if the feature was created while waiting for the lock.
            if (_cachedFeature is not null)
                return _cachedFeature;

            if (KeyProperty is null)
            {
                InitSourceMetadata(serviceProvider);
            }

            var builder = new ODataConventionModelBuilder();
            var entitySet = builder.EntitySet<TSource>(Name);

            var entityType = builder.AddEntityType(SourceType);
            builder.AddEntitySet(Name, entityType);
            entityType.HasKey(KeyProperty!.PropertyInfo);

            //TODO: add other odata properties

            builder.EnableLowerCamelCaseForPropertiesAndEnums();

            var model = builder.GetEdmModel();
            var edmEntitySet = model.EntityContainer.FindEntitySet(Name);
            var entitySetSegment = new EntitySetSegment(edmEntitySet);
            var segments = new List<ODataPathSegment> { entitySetSegment };

            var path = new ODataPath(segments);
            _cachedFeature = new ODataFeature
            {
                Path = path,
                Model = model,
                RoutePrefix = Container.RoutePrefix,
                Services = Container.ODataInternalServiceProvider
            };
        }

        return _cachedFeature;
    }

    internal IProperty? KeyProperty { get; set; }

    internal IEntityType? EFCoreEntityType { get; set; }

    private static readonly object _lock = new();

    /// <summary>
    /// Find key property for entity type, complext types, collections
    /// </summary>
    /// <param name="dbContext"></param>
    /// <exception cref="InvalidOperationException"></exception>
    internal void InitSourceMetadata(IServiceProvider serviceProvider)
    {
        if (KeyProperty is not null)
            return;

        lock (_lock)
        {
            // Check if KeyProperty is already set to avoid redundant initialization
            if (KeyProperty is not null)
                return;

            if (EFCoreEntityType is null)
            {
                using var scope = serviceProvider.CreateScope();

                var dbContext = (DbContext)ActivatorUtilities.CreateInstance(scope.ServiceProvider, DbContextType);

                var entityType = dbContext.Model.FindEntityType(SourceType);
                if (entityType is null)
                    throw new InvalidOperationException($"Entity type {SourceType} not found in DbContext");

                EFCoreEntityType = entityType;
            }


            var keyProperty = EFCoreEntityType.FindPrimaryKey();
            if (keyProperty is null)
                throw new InvalidOperationException($"Primary key not found for {SourceType}");

            KeyProperty = keyProperty.Properties.Single();

            var nestedEntityTypes = FindNestedEntityTypes(EFCoreEntityType, NestedLevel);

            nestedEntityTypes.Add(EFCoreEntityType);
            SupportEntities = nestedEntityTypes;
        }
    }

    internal IEnumerable<IEntityType> SupportEntities { get; private set; } = new List<IEntityType>();

    private List<IEntityType> FindNestedEntityTypes(
    IEntityType entityType,
    int nestingLevel,
    int currentLevel = 0)
    {
        if (currentLevel >= nestingLevel)
            return new List<IEntityType>();

        var nestedEntityTypes = new List<IEntityType>();

        foreach (var navigation in entityType.GetNavigations())
        {
            var targetEntityType = navigation.TargetEntityType;

            if (!nestedEntityTypes.Contains(targetEntityType))
            {
                nestedEntityTypes.Add(targetEntityType);

                // Recursively find nested types
                nestedEntityTypes.AddRange(
                    FindNestedEntityTypes(targetEntityType, nestingLevel, currentLevel + 1));
            }
        }

        return nestedEntityTypes;
    }


    internal EntityEndpoint<TEntity> GetOrCreateEndpointConfiguration<TEntity>(IServiceProvider serviceProvider)
        where TEntity : class
    {
        if (typeof(TEntity) != SourceType)
            throw new InvalidOperationException($"Invalid source type {SourceType} for {typeof(TEntity)}");

        if (KeyProperty is null)
        {
            InitSourceMetadata(serviceProvider);
        }

        var entityEndpoint = serviceProvider
            .GetKeyedService<EntityEndpoint<TEntity>>(Name);

        //TODO: remove entity endpoint creation
        if (entityEndpoint is null)
        {
            entityEndpoint = new EntityEndpoint<TEntity>();
        }

        entityEndpoint!.SetMetadata(this);
        return entityEndpoint;
    }
}

using CFW.Core.Results;
using CFW.DynamicApi.Deltas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

#pragma warning disable IDE0161 // Convert to file-scoped namespace : fix class name duplication
namespace CFW.DynamicApi.Buiders
#pragma warning restore IDE0161 // Convert to file-scoped namespace
{
    public class DynamicEntityGroupBuilder<TEntity, TDbContext> : DynamicEntityGroupBuilder<TEntity>
    where TEntity : class
    where TDbContext : DbContext
    {
        private readonly TDbContext _db;
        public DynamicEntityGroupBuilder(TDbContext db)
        {
            _db = db;
        }

        public DynamicEntityGroupBuilder<TEntity, TDbContext> AddQueryApi(Action<DynamicApiOperation>? operationConfig = null)
        {
            var result = new DynamicApiOperation
            {
                EntityGroup = this,
                HttpMethod = HttpMethod.Get.Method,
                Handler = async (context) =>
                {
                    var db = context.RequestServices.GetRequiredService<TDbContext>();
                    var entities = db.Set<TEntity>().AsNoTracking();
                    return await Task.FromResult(entities);
                }
            };

            operationConfig?.Invoke(result);

            WithOperation(result);
            return this;
        }


        public DynamicEntityGroupBuilder<TEntity, TDbContext> AddCreationApi(Action<DynamicApiOperation>? operationConfig = null)
        {
            var result = new DynamicApiOperation
            {
                EntityGroup = this,
                HttpMethod = HttpMethod.Post.Method,
                Handler = async (context) =>
                {
                    var delta = await EntityDelta<TEntity>.BindAsync(context);
                    var db = context.RequestServices.GetRequiredService<TDbContext>();

                    var entity = delta!.Instance!;
                    var entry = db!.Set<TEntity>().Add(entity);

                    await ProcessChangedNavigationPropertiesRecursive(delta.ChangedProperties!, entry, default);

                    var affected = await db.SaveChangesAsync();
                    if (affected == 0)
                    {
                        return entity.Failed("Failed to create entity");
                    }

                    return entity.Created();
                }
            };

            operationConfig?.Invoke(result);

            WithOperation(result);
            return this;
        }

        public override JsonConverterFactory CreateJsonConverterFactory(IServiceProvider serviceProvider)
        {
            var result = ActivatorUtilities.CreateInstance(serviceProvider
                , typeof(EntityDeltaConverterFactory<TEntity, TDbContext>));

            return result as JsonConverterFactory
                ?? throw new InvalidOperationException($"Cannot create {nameof(EntityDeltaConverterFactory<TEntity, TDbContext>)}");
        }

        protected override IEnumerable<PropertyMetadata> ResolveSelectedProperties()
        {
            var dbEntityType = _db.Set<TEntity>().EntityType;

            var props = dbEntityType.GetProperties();
            if (_includeProperties.Any())
                props = props.Where(x => _includeProperties.Contains(x.Name));

            if (_excludeProperties.Any())
                props = props.Where(x => !_excludeProperties.Contains(x.Name));

            var scalarProps = props
                .Select(x => new PropertyMetadata
                {
                    IsKey = x.IsKey(),
                    Name = x.Name,
                    ClrType = x.ClrType,
                    IsRequired = x.IsNullable,
                    PropertyType = PropertyType.Scalar
                });

            var complexProps = dbEntityType.GetComplexProperties();
            if (_includeProperties.Any())
                complexProps = complexProps.Where(x => _includeProperties.Contains(x.Name));

            if (_excludeProperties.Any())
                complexProps = complexProps.Where(x => !_excludeProperties.Contains(x.Name));

            var complexPropsMetadata = complexProps
                .Select(x => new PropertyMetadata
                {
                    IsKey = false,
                    Name = x.Name,
                    ClrType = x.ClrType,
                    IsRequired = x.IsNullable,
                    PropertyType = x.IsCollection ? PropertyType.Collection : PropertyType.Complex,
                    ChildProperties = x.DeclaringType.GetProperties()
                        .Select(p => new PropertyMetadata
                        {
                            IsKey = p.IsKey(),
                            Name = p.Name,
                            ClrType = p.ClrType,
                            IsRequired = p.IsNullable,
                            PropertyType = PropertyType.Scalar
                        })
                });

            var navigations = dbEntityType.GetNavigations();
            if (_includeProperties.Any())
                navigations = navigations.Where(x => _includeProperties.Contains(x.Name));
            if (_excludeProperties.Any())
                navigations = navigations.Where(x => !_excludeProperties.Contains(x.Name));

            var navigationProps = navigations
                .Select(x => new PropertyMetadata
                {
                    IsKey = false,
                    Name = x.Name,
                    ClrType = x.ClrType,
                    IsRequired = false,
                    PropertyType = x.IsCollection ? PropertyType.Collection : PropertyType.Complex,
                    ChildProperties = x.TargetEntityType.GetProperties()
                        .Select(p => new PropertyMetadata
                        {
                            IsKey = p.IsKey(),
                            Name = p.Name,
                            ClrType = p.ClrType,
                            IsRequired = !p.IsNullable,
                            PropertyType = PropertyType.Scalar
                        })
                        .ToList()
                })
                .ToList();

            return scalarProps
                .Concat(complexPropsMetadata)
                .Concat(navigationProps).ToList();
        }

        protected async Task ProcessChangedNavigationPropertiesRecursive(
            IDictionary<string, object> changedProperties,
            EntityEntry entry,
            CancellationToken cancellationToken = default)
        {
            var entityDeltas = changedProperties
                .Where(x => x.Value is EntityDelta delta && delta.EfCoreEntityType is not null);

            foreach (var (key, value) in entityDeltas)
            {
                var delta = (EntityDelta)value;
                var navigation = entry.Navigation(key);
                if (!navigation.IsLoaded)
                {
                    await navigation.LoadAsync(cancellationToken);
                }
                await ProcessChangedNavigationPropertiesRecursive(delta.ChangedProperties!
                    , entry.Context.Entry(navigation.CurrentValue!), cancellationToken);
            }

            var collectionDeltas = changedProperties
                .Where(x => x.Value is EntityDeltaSet deltaSets);

            foreach (var (key, value) in collectionDeltas)
            {
                var deltaSets = (EntityDeltaSet)value;
                var navigation = entry.Navigation(key);
                if (!navigation.IsLoaded)
                {
                    await navigation.LoadAsync(cancellationToken);
                }

                foreach (var delta in deltaSets.ChangedProperties)
                {
                    var itemEntry = entry.Context.Entry(delta.GetInstance()!);
                    await ProcessChangedNavigationPropertiesRecursive(delta!.ChangedProperties!
                        , itemEntry, cancellationToken);
                }
            }
        }

    }

}


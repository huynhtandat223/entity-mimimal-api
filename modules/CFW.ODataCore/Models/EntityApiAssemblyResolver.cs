using CFW.EntityApi.Attributes;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.EntityApi.Models;

public interface ITypesResolver : IAssemblyResolver
{
    public IList<Type> CachedTypes { get; }

    public IEnumerable<DbEntityAttribute> DbEntityAttributes { get; }
}

public class EntityApiAssemblyResolver : ITypesResolver
{
    public IEnumerable<Assembly> Assemblies => throw new NotImplementedException();

    public IList<Type> CachedTypes => AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(x => x.GetExportedTypes())
        .Where(x => x.GetCustomAttributes<BaseRoutingAttribute>() != null)
        .ToList();

    public IEnumerable<DbEntityAttribute> DbEntityAttributes => GetDbEntityAttributes();

    private IEnumerable<DbEntityAttribute> GetDbEntityAttributes()
    {
        var dbEntityAttributes = CachedTypes
            .Where(x => x.GetCustomAttributes<DbEntityAttribute>() is not null)
            .SelectMany(x => x.GetCustomAttributes<DbEntityAttribute>().Select(a => new { TargetType = x, Attribute = a }))
            .Aggregate(new List<DbEntityAttribute>(), (acc, x) =>
            {
                x.Attribute.TargetType = x.TargetType;
                acc.Add(x.Attribute);
                return acc;
            });

        return dbEntityAttributes;
    }
}

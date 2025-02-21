using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.Core.EfCoreExtensions;

public static class EntityModelExtensions
{
    public static string[] GetComplexAndNavigationPropertyNames<T>(this DbSet<T> dbSet)
        where T : class
    {
        var complexProperties = dbSet.EntityType.GetComplexProperties()
            .Select(x => x.Name)
            .ToArray();

        var navigationProperties = dbSet.EntityType.GetNavigations()
            .Select(x => x.Name)
            .ToArray();
        return complexProperties.Concat(navigationProperties).ToArray();
    }

    public static string[] GetScalarProperties<T>(this DbSet<T> dbSet, Func<IProperty, bool>? filter = null)
        where T : class
    {
        filter = filter ?? (x => true);

        return dbSet.EntityType.GetProperties()
            .Where(filter)
            .Select(x => x.Name)
            .ToArray();
    }

    public static bool IsDateTimeProperty(this IProperty property)
    {
        return property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTimeOffset)
            || property.ClrType == typeof(DateTime?) || property.ClrType == typeof(DateTimeOffset?);
    }
}

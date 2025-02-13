using Microsoft.EntityFrameworkCore;

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

    public static string[] GetScalarProperties<T>(this DbSet<T> dbSet)
        where T : class
    {
        return dbSet.EntityType.GetProperties()
            .Select(x => x.Name)
            .ToArray();
    }
}

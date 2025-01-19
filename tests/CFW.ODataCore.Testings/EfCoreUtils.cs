using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CFW.ODataCore.Testings
{
    public static class EfCoreUtils
    {
        public static async Task<object?> LoadAsync(
            this DbContext db,
            Type entityType,
            object[] keyValues,
            int nestingLevel = 2,
            CancellationToken cancellationToken = default)
        {
            // Load the primary entity
            var entity = await db.FindAsync(entityType, keyValues: keyValues, cancellationToken);

            if (entity is null)
                return null;

            // Recursively load navigation properties and complex types
            var visitedEntities = new HashSet<object>();
            await LoadNavigationsAsync(db.Entry(entity), nestingLevel, visitedEntities, cancellationToken);

            return entity;
        }

        private static async Task LoadNavigationsAsync(
            EntityEntry entry,
            int remainingLevels,
            HashSet<object> visitedEntities,
            CancellationToken cancellationToken)
        {
            if (remainingLevels <= 0 || visitedEntities.Contains(entry.Entity))
                return;

            // Mark the current entity as visited
            visitedEntities.Add(entry.Entity);

            // Iterate through all navigations (includes collections and references)
            foreach (var navigation in entry.Navigations)
            {
                if (!navigation.IsLoaded)
                {
                    await navigation.LoadAsync(cancellationToken);

                    if (navigation.CurrentValue is IEnumerable<object> collection)
                    {
                        // Process each entity in the collection
                        foreach (var item in collection)
                        {
                            if (!visitedEntities.Contains(item))
                            {
                                var itemEntry = entry.Context.Entry(item);
                                await LoadNavigationsAsync(itemEntry, remainingLevels - 1, visitedEntities, cancellationToken);
                            }
                        }
                    }
                    else
                    {
                        // Process single reference navigation
                        var navEntry = entry.Context.Entry(navigation.CurrentValue);
                        await LoadNavigationsAsync(navEntry, remainingLevels - 1, visitedEntities, cancellationToken);
                    }
                }
            }
        }
    }
}

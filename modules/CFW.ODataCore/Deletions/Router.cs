using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.Queries;
using CFW.EntityApi.Registrators;
using Microsoft.AspNetCore.Mvc;

namespace CFW.EntityApi.Deletions;

public class Router<TEntity, TKey> : IContainerMemberRouter
    where TEntity : class
{
    public Task Register(ContainerMemberRegistrationContext containerMemberRegistrationContext)
    {
        var entityGroupBuider = containerMemberRegistrationContext.MemberRouteGroup;
        var entityApiConfiguration = (EntityApiConfiguration<TEntity, TKey>)containerMemberRegistrationContext
            .EntityConfiguration;

        var deletionHandlerFactory = entityApiConfiguration.DeletionHandlerFactory;
        if (deletionHandlerFactory is null)
        {
            deletionHandlerFactory = async s =>
            {
                var defaultDeletionHandlerType = typeof(DefaultEntityApiDeletion<,,>)
                    .MakeGenericType(entityApiConfiguration.DbContextType!
                    , entityApiConfiguration.EntityType, entityApiConfiguration.DbPrimaryKey!.ClrType);


                var deletionHandler = (IEntityApiDeletionHandler<TEntity, TKey>)ActivatorUtilities
                    .CreateInstance(s, defaultDeletionHandlerType);

                return await Task.FromResult(deletionHandler);
            };
        }

        entityGroupBuider.MapDelete("/{key}", async (TKey key
            , [FromServices] IServiceProvider sp
            , CancellationToken cancellationToken) =>
        {
            var handler = await deletionHandlerFactory(sp);
            var result = await handler.Handle(key, cancellationToken);

            return result.ToResults();
        });

        return Task.CompletedTask;
    }
}

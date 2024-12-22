﻿using CFW.ODataCore.Features.BoundActions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace CFW.ODataCore.Features.Shared;

public interface IBoundActionRequestHandler<TODataViewModel, TKey, TRequest, TResponse>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    Task<ActionResult> Handle(BoundActionsController<TODataViewModel, TKey, TRequest, TResponse> controller
        , TRequest request, CancellationToken cancellationToken);
}

public class DefaultBoundActionRequestHandler<TViewModel, TKey, TRequest, TResponse>
    : DefaultBoundActionRequestHandler<TRequest, TResponse>, IBoundActionRequestHandler<TViewModel, TKey, TRequest, TResponse>
    where TViewModel : class, IODataViewModel<TKey>
{
    public async Task<ActionResult> Handle(BoundActionsController<TViewModel, TKey, TRequest, TResponse> controller
        , TRequest request, CancellationToken cancellationToken)
    {
        return await base.Handle(controller, request, cancellationToken);
    }
}

public interface IUnboundActionRequestHandler<TRequest, TResponse>
{
    Task<ActionResult> Handle(ODataController controller
        , TRequest request, CancellationToken cancellationToken);
}

public class DefaultBoundActionRequestHandler<TRequest, TResponse> : IUnboundActionRequestHandler<TRequest, TResponse>
{
    public async Task<ActionResult> Handle(ODataController controller
        , TRequest request, CancellationToken cancellationToken)
    {
        if (!controller.ModelState.IsValid)
            return controller.BadRequest(controller.ModelState);

        if (typeof(TResponse) == typeof(Result))
        {
            var handler = controller.HttpContext.RequestServices.GetRequiredService<IODataActionHandler<TRequest>>();
            var result = await handler.Execute(request, cancellationToken);
            return result.ToActionResult();
        }
        else
        {
            var handler = controller.HttpContext.RequestServices.GetRequiredService<IODataActionHandler<TRequest, TResponse>>();
            var result = await handler.Execute(request, cancellationToken);
            return result.ToActionResult();
        }
    }
}



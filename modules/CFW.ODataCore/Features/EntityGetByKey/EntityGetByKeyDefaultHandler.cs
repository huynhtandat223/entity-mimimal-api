﻿using CFW.ODataCore.Features.EFCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CFW.ODataCore.Features.EntityQuery;


public class EntityGetByKeyDefaultHandler<TODataViewModel, TKey> : IEntityGetByKeyHandler<TODataViewModel, TKey>
    where TODataViewModel : class, IODataViewModel<TKey>
{
    private readonly IODataDbContextProvider _dbContextProvider;
    private readonly IActionContextAccessor _actionContextAccessor;

    public EntityGetByKeyDefaultHandler(IODataDbContextProvider dbContextProvider, IActionContextAccessor actionContextAccessor)
    {
        _dbContextProvider = dbContextProvider;
        _actionContextAccessor = actionContextAccessor;
    }

    public async Task<Result<dynamic>> Handle(TKey key, ODataQueryOptions<TODataViewModel> options, CancellationToken cancellationToken)
    {
        if (_actionContextAccessor.ActionContext is not null && _actionContextAccessor.ActionContext.ModelState is not null)
        {
            var modelState = _actionContextAccessor.ActionContext.ModelState;
            if (!modelState.IsValid)
                return new Result<dynamic>
                {
                    HttpStatusCode = HttpStatusCode.BadRequest,
                    IsSuccess = false,
                    Data = modelState.ToJsonString(),
                };
        }

        var db = _dbContextProvider.GetContext();
        var query = db.Set<TODataViewModel>().Where(x => x.Id!.Equals(key));
        var appliedQuery = options.ApplyTo(query);

        var result = await appliedQuery.Cast<dynamic>().SingleOrDefaultAsync(cancellationToken);

        if (result == null)
            return new Result<dynamic>
            {
                HttpStatusCode = HttpStatusCode.NotFound,
                IsSuccess = false,
            };

        return new Result<dynamic>
        {
            HttpStatusCode = HttpStatusCode.OK,
            IsSuccess = true,
            Data = result,
        };
    }
}

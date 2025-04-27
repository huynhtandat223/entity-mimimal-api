using Microsoft.AspNetCore.Http;

namespace CFW.DynamicApi.Interceptors;

public interface IOperationInterceptor
{
    Task OnExecutingAsync(HttpContext context, DynamicApiOperation operation) => Task.CompletedTask;

    Task OnExecutedAsync(HttpContext context, DynamicApiOperation operation, object? result);
}

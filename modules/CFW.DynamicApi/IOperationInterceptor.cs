using Microsoft.AspNetCore.Http;

namespace CFW.DynamicApi;

public interface IOperationInterceptor
{
    Task<object?> OnExecutingAsync(HttpContext context, DynamicApiOperation operation);
    Task<object?> OnExecutedAsync(HttpContext context, DynamicApiOperation operation, object? result);
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.Extensions.DependencyInjection;

namespace CFW.DynamicApi.Interceptors.OData;
public class ODataResult : IResult
{
    private readonly object _result;

    public ODataResult(object result)
    {
        _result = result;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        if (httpContext.RequestAborted.IsCancellationRequested)
        {
            return;
        }

        var formatterContext = new OutputFormatterWriteContext(httpContext,
            (stream, encoding) => new StreamWriter(stream, encoding),
            _result.GetType() ?? typeof(object), _result)
        {
            ContentType = "application/json;odata.metadata=none",
        };

        var formatter = httpContext.RequestServices.GetRequiredService<ODataOutputFormatter>();
        await formatter.WriteAsync(formatterContext);
    }
}

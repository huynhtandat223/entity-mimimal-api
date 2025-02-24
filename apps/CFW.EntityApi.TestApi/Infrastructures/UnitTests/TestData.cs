using CFW.EntityApi.Models;

namespace CFW.EntityApi.TestApi.Infrastructures.UnitTests;

public record class TestData
{
    public DataProvider DataProvider { get; set; }

    public string Url { get; set; } = string.Empty;

    public string RoutePrefix { get; set; } = string.Empty;

    public string IdPropertyName { get; set; } = "Id";
}

public record class TestData<T> : TestData
{
    public string[] ScalarProperties { set; get; } = Array.Empty<string>();
}

public class TestAction<TRequest, TResponse>
{
    public string ActionRoute { get; set; } = string.Empty;

    public ApiMethod Method { get; set; } = ApiMethod.Post;
}
using System.Net;

namespace CFW.Core.Results;

public interface IResult
{
    public bool IsSuccess { get; set; }

    public string? Message { get; set; }
}

public interface IResult<TResult> : IResult
{
    public TResult? Data { set; get; }
}

public class Result : IResult
{
    public bool IsSuccess { get; set; }

    public string? Message { get; set; }

    public Exception? Exception { get; set; }

    public HttpStatusCode HttpStatusCode { get; set; }

    public object? Data { get; set; }

    /// <summary>
    /// Return directly the custom result if it is not null.
    /// </summary>
    public object? CustomResult { get; set; }
}

public class Result<T> : Result, IResult<T>
{
    public new T? Data
    {
        get => (T?)base.Data;
        set => base.Data = value;
    }
}

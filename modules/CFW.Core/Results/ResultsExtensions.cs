namespace CFW.Core.Results;

public static class ResultsExtensions
{
    public static IResult Success(this object _)
        => new Result { IsSuccess = true, HttpStatusCode = System.Net.HttpStatusCode.OK };

    public static IResult Notfound(this object _, string? message = null)
        => new Result { IsSuccess = false, Message = message, HttpStatusCode = System.Net.HttpStatusCode.NotFound };

    public static IResult<T> Success<T>(this T? data)
        => new Result<T> { IsSuccess = true, Data = data, HttpStatusCode = System.Net.HttpStatusCode.OK };

    public static IResult<T> Created<T>(this T data)
        => new Result<T> { IsSuccess = true, Data = data, HttpStatusCode = System.Net.HttpStatusCode.Created };

    public static IResult<T> Failed<T>(this T? data, string message)
        => new Result<T> { IsSuccess = false, Data = data, Message = message, HttpStatusCode = System.Net.HttpStatusCode.BadRequest };

    public static IResult Failed(this object _, string message)
        => new Result { IsSuccess = false, Message = message, HttpStatusCode = System.Net.HttpStatusCode.BadRequest };

    public static IResult<T> Notfound<T>(this T? _, string? message = null)
        => new Result<T> { IsSuccess = false, Message = message, HttpStatusCode = System.Net.HttpStatusCode.NotFound };

    public static bool IsNotSuccess(this Result result)
        => !result.IsSuccess;
}

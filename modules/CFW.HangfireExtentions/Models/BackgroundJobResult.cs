using CFW.Core.Results;

namespace CFW.HangfireExtentions.Models;
public class BackgroundJobResult : IResult
{
    //public IReadOnlyCollection<string> Errors => _baseResult.err;

    //public DomainOperationStatus Status => _domainResult.Status;

    //public DomainResultMetadata? Metadata { get; }

    public bool IsRetryable { set; get; }
    public bool IsSuccess { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string? Message { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}

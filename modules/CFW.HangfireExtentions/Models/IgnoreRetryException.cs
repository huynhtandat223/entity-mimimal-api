namespace CFW.HangfireExtentions.Models;
public class IgnoreRetryException : Exception
{
    public IgnoreRetryException() { }

    public IgnoreRetryException(string message) : base(message) { }
}

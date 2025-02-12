namespace CFW.EntityApi.Models;

public class ODataQueryResult<T>
{
    public IEnumerable<T> Value { get; set; } = Array.Empty<T>();

    public int? TotalCount { set; get; }
}
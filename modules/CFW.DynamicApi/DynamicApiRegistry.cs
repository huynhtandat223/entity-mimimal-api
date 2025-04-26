namespace CFW.DynamicApi;

public class DynamicApiRegistry
{
    private readonly List<DynamicApiOperation> _operations = new();

    internal void RegisterOperations(IEnumerable<DynamicApiOperation> ops)
    {
        _operations.AddRange(ops);
    }

    public IReadOnlyList<DynamicApiOperation> GetAllOperations() => _operations.AsReadOnly();
}

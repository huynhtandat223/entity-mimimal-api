using CFW.DynamicApi.Buiders;

namespace CFW.DynamicApi;

public class DynamicApiRegistry
{
    private readonly List<DynamicEntityGroupBuilder> _apiGroups = new();

    internal void RegisterApiGroup(DynamicEntityGroupBuilder apiGroup)
    {
        _apiGroups.Add(apiGroup);
    }

    public IReadOnlyCollection<DynamicEntityGroupBuilder> ApiGroups => _apiGroups.AsReadOnly();
}

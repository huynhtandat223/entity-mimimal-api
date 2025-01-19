using System.Collections;

namespace CFW.ODataCore.Models.Deltas;

//TODO: need to refactor
public class EntityDeltaSet
{
    public required Type ObjectType { get; set; }

    public List<EntityDelta> ChangedProperties { get; }
        = new List<EntityDelta>();

    public IList GetList()
    {
        var listType = typeof(List<>).MakeGenericType(ObjectType);
        var resultList = (IList)Activator.CreateInstance(listType)!;

        foreach (var item in ChangedProperties)
        {
            var instance = item.GetInstance();
            resultList.Add(instance);
        }
        return resultList;
    }
}

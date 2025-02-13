using System.Reflection;

namespace CFW.EntityApi.TestApi.Infrastructures.UnitTests;

public class GenericDataAttribute : MemberDataAttributeBase
{
    public GenericDataAttribute(string memberName, params object[] parameters) : base(memberName, parameters)
    {
    }

    protected override object[] ConvertDataItem(MethodInfo testMethod, object item)
    {
        return [item];
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var data = base.GetData(testMethod).ToList();

        var dataItemEnhancementAttributes = testMethod.GetCustomAttributes<DataItemEnhancementAttribute>();

        var result = new List<object[]>();
        foreach (var testDataItem in data)
        {
            var mainData = testDataItem.ToList();
            foreach (var attr in dataItemEnhancementAttributes)
            {
                attr.Enhance(mainData);
            }
            result.Add(mainData.ToArray());
        }

        var dataListEnhancementAttributes = testMethod.GetCustomAttributes<DataListEnhancementAttribute>();
        foreach (var attr in dataListEnhancementAttributes)
        {
            result = attr.Enhance(result);
        }

        return result;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class DataItemEnhancementAttribute : Attribute
{
    public virtual void Enhance(List<object> data)
    {
    }
}

public class DataListEnhancementAttribute : Attribute
{
    public virtual List<object[]> Enhance(List<object[]> data)
    {
        return data;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CombineAttribute : DataItemEnhancementAttribute
{
    public CombineAttribute(params object[] parameters)
    {
        Parameters = parameters;
    }
    public object[] Parameters { get; }

    public override void Enhance(List<object> data)
    {
        data.AddRange(Parameters);
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CombineRandomValueAttribute : DataListEnhancementAttribute
{
    private readonly Type? _randomType;
    private readonly object[]? _values;
    public CombineRandomValueAttribute(Type randomType)
    {
        _randomType = randomType;
    }

    public CombineRandomValueAttribute(object[] values)
    {
        _values = values;
    }

    public int ValueCount { get; set; } = 1;

    public override List<object[]> Enhance(List<object[]> data)
    {
        var randomValues = _values ?? DataGenerator.CreateList(_randomType!, ValueCount);
        var result = new List<object[]>();
        foreach (var randomValue in randomValues)
        {
            foreach (var item in data)
            {
                var newItem = item.ToList();
                newItem.Add(randomValue);
                result.Add(newItem.ToArray());
            }
        }

        return result;
    }
}

/// <summary>
/// Append test method parameters from the specified method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CombineFromAttribute : DataItemEnhancementAttribute
{
    private readonly int[] _parameterIndexes;
    private readonly string _method;

    /// <summary>
    /// Append test method parameters from the specified method.
    /// </summary>
    /// <param name="parameterIndexes">Test data index of lower test data pipeline</param>
    /// <param name="method"></param>
    public CombineFromAttribute(int[] parameterIndexes, string method)
    {
        _parameterIndexes = parameterIndexes;
        _method = method;
    }

    public Type TargetType { get; set; } = typeof(TestUtils);

    public object[]? AdditionalArguments { get; set; }

    public override void Enhance(List<object> data)
    {
        var parameters = _parameterIndexes.Select(idx => data[idx]).ToList();
        if (AdditionalArguments is not null && AdditionalArguments.Length > 0)
        {
            parameters.AddRange(AdditionalArguments);
        }

        MethodInfo? methodInfo = TargetType.GetMethod(_method);
        if (methodInfo is null)
        {
            throw new InvalidOperationException($"Method '{_method}' not found.");
        }

        var combineParameters = methodInfo!.Invoke(null, parameters.ToArray());

        data.Add(combineParameters!);
    }
}
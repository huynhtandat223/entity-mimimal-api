namespace CFW.AppHost.Features.Databases.ViewModels;

public class DatabaseTableViewModel
{
    public string Name { get; set; } = string.Empty;

    public IEnumerable<DatabaseColumnViewModel> Columns { get; set; } = Enumerable.Empty<DatabaseColumnViewModel>();

    public string? Schema { get; set; }
}

public class DatabaseColumnViewModel
{
    /// <summary>
    /// Indicates whether or not this column can contain null values.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// The column name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    //
    // Summary:
    //     Indicates whether the current object is read-only.
    //
    // Remarks:
    //     Annotations cannot be changed when the object is read-only. Runtime annotations
    //     cannot be changed when the object is not read-only.
    public bool IsReadOnly { get; set; }

    public object? DefaultValue { get; set; }

    public string? StoreType { get; set; }
}



using CFW.AppHost.Features.Databases.Models;

namespace CFW.AppHost.Features.Databases.ViewModels;

public class TableDefinitionViewModel
{
    public Guid? Id { set; get; }

    public string Namespace { set; get; } = string.Empty;

    public string Name { set; get; } = string.Empty;

    public IEnumerable<ColumnDefinitionViewModel> Properties { set; get; }
        = new List<ColumnDefinitionViewModel>();

    public IEnumerable<RelationshipDefinitionViewModel>? Relationships { set; get; }
}

public class ColumnDefinitionViewModel
{
    public Guid? Id { set; get; }

    public string Name { set; get; } = string.Empty;

    /// <summary>
    /// AssemblyQualifiedName
    /// </summary>
    public string Type { set; get; } = string.Empty;

    public bool IsKey { set; get; }

    public bool IsRequired { set; get; }

    public bool IsNullable { set; get; }
}

public class RelationshipDefinitionViewModel
{
    public Guid? Id { set; get; }

    public string Name { set; get; } = string.Empty;

    public string TargetEntity { set; get; } = string.Empty;

    public string TargetProperty { set; get; } = string.Empty;

    public RelationshipType Type { set; get; }
}
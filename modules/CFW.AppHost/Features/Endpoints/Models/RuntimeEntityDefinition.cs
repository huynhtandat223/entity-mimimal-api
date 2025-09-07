using CFW.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace CFW.AppHost.Features.Endpoints.Models;

public class RuntimeEntityDefinition : IEntity<Guid>
{
    public Guid Id { set; get; }

    [Required]
    public string Namespace { set; get; } = string.Empty;

    [Required]
    public string Name { set; get; } = string.Empty;

    public IEnumerable<RuntimeEntityPropertyDefinition> Properties { set; get; }
        = new List<RuntimeEntityPropertyDefinition>();

    public IEnumerable<RuntimeEntityRelationshipDefinition> Relationships { set; get; }
        = new List<RuntimeEntityRelationshipDefinition>();
}


public class RuntimeEntityPropertyDefinition : IEntity<Guid>
{
    public Guid Id { set; get; }

    public string Name { set; get; } = string.Empty;

    /// <summary>
    /// AssemblyQualifiedName
    /// </summary>
    public string Type { set; get; } = string.Empty;

    public bool IsKey { set; get; }

    public bool IsRequired { set; get; }

    public bool IsNullable { set; get; }
}

public class RuntimeEntityRelationshipDefinition : IEntity<Guid>
{
    public Guid Id { set; get; }

    public string Name { set; get; } = string.Empty;

    public string TargetEntity { set; get; } = string.Empty;

    public string TargetProperty { set; get; } = string.Empty;

    public RelationshipType Type { set; get; }
}

public enum RelationshipType
{
    OneToOne,
    OneToMany,
    ManyToOne,
    ManyToMany
}
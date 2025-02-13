namespace CFW.EntityApi.Attributes;

public abstract class BaseRoutingAttribute : Attribute
{
    /// <summary>
    /// If not set, the name will take value of container default. <see cref="Models.Builders.ContainerApiBuilder.SetDefault(bool)"/>
    /// </summary>
    public string? RoutePrefix { get; set; }
}

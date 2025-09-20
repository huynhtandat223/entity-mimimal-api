namespace CFW.AppHost.Features.Endpoints.ViewModels;

public class ContainerConfigurationViewModel
{
    public Guid? Id { get; set; }

    public string RoutePrefix { get; set; } = string.Empty;

    public int? DefaultPageSize { get; set; }
}

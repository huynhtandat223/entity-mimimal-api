//using CFW.AppHost.Features.Core;
//using CFW.AppHost.Features.Slidebots.Models;
//using CFW.DynamicApi;
//using CFW.DynamicApi.Buiders;

//namespace CFW.AppHost.Features.Slidebots.Endpoints;

//public class SlidebotsCRUDConfigurator : IEndpointConfigurator
//{
//    private readonly DynamicEntityGroupBuilder<Slide, AppDbContext, Guid> _buider;

//    public SlidebotsCRUDConfigurator(DynamicEntityGroupBuilder<Slide, AppDbContext, Guid> builder)
//    {
//        _buider = builder;
//    }

//    public DynamicEntityGroupBuilder Configure()
//    {
//        return _buider
//            .AddQueryApi()
//            .AddCreationApi()
//            .WithRouteName("slide-bots");
//    }
//}

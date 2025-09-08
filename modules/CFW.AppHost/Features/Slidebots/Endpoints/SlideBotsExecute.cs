//using CFW.AppHost.Infrastructures.PlaywrightExtensions;
//using CFW.DynamicApi;

//namespace CFW.AppHost.Features.Slidebots.Endpoints;

//[ApiOperation("slide-bots", RouteName = "/{id}/execute")]
//public class SlideBotsExecute : IRequestHandler<SlideBotsExecute.Request, SlideBotsExecute.Response>
//{
//    public class Request
//    {
//        public Guid Id { get; set; }
//    }

//    public class Response
//    {
//        public string? OutputPath { get; set; }

//        public string? Logs { get; set; }
//    }

//    public async Task<IResult<Response>> Handle(RequestModel<Request> request, CancellationToken cancellationToken)
//    {
//        //var db = request.GetService<AppDbContext>();
//        var result = new Response();

//        //var slideBot = await db.Set<Slide>().FirstOrDefaultAsync(x => x.Id == request.Model.Id);
//        //if (slideBot == null)
//        //{
//        //    return result.Notfound("Slide bot not found");
//        //}

//        var steps = new IStep[] {
//            new GotoStep
//            {
//                Url = "https://www.google.com",
//            },
//            new TypeStep
//            {
//                Selector = "textarea[name='q']",
//                Text = "CFW"
//            },
//            new ClickStep
//            {
//                Selector = "input[name='btnK']"
//            }
//        };
//        using var automationContext = await AutomationContext.CreateAsync("ws://127.0.0.1:54209/devtools/browser/9c40ec39-15b2-4232-af44-212f173a2d47");
//        await automationContext.Execute(steps);

//        return result.Success();
//    }
//}

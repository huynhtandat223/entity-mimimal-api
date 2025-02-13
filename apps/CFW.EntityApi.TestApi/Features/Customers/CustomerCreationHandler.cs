//using CFW.EntityMinimalApi.Attributes;
//using CFW.ODataCore.Testings.Models;

//namespace CFW.ODataCore.Testings.Features.Customers;

//[Entity]
//public class CustomerCreationHandler : IEntityCreationHandler<Customer>
//{
//    public Task<Result> Handle(CreationCommand<Customer> command, CancellationToken cancellationToken)
//    {
//        var result = command.Delta.Instance.Ok() as Result;
//        return Task.FromResult(result);
//    }
//}

using CFW.AppHost.Features.Core;
using CFW.AppHost.Features.Databases.ViewModels;
using CFW.AppHost.Infrastructures.DbContextExtensions.Models;
using CFW.AppHost.Infrastructures.DbContextExtensions.Services;
using CFW.DynamicApi;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace CFW.AppHost.Features.Databases.Endpoints;

public class DatabasesListTable
{
    public class Response
    {
        public IEnumerable<DatabaseTableViewModel> Value { get; set; } = Enumerable.Empty<DatabaseTableViewModel>();
    }

    [ApiOperation("databases", RouteName = "tables")]
    public class Handler : IRequestHandler<DatabaseConfiguration, Response>
    {
        private readonly DesignTimeService _designTimeService;
        private readonly ConnectionStringBuilder _connectionStringBuilder;
        private readonly AppRequestContext _appRequestContext;

        public Handler(DesignTimeService designTimeService, ConnectionStringBuilder connectionStringBuilder
            , AppRequestContext appRequestContext)
        {
            _designTimeService = designTimeService;
            _connectionStringBuilder = connectionStringBuilder;
            _appRequestContext = appRequestContext;
        }

        public async Task<IResult<Response>> Handle(RequestModel<DatabaseConfiguration> request, CancellationToken cancellationToken)
        {
            var model = request.Model;
            var connectionString = _connectionStringBuilder.BuildConnectionString(model);

            var serviceProvider = _designTimeService.CreateDesignTimeServiceProvider(connectionString, model.DatabaseProvider);
            var dbModelFactory = serviceProvider.GetRequiredService<IDatabaseModelFactory>();
            var dbModel = dbModelFactory.Create(connectionString, new DatabaseModelFactoryOptions());

            var tables = dbModel.Tables.Select(x => new DatabaseTableViewModel
            {
                Name = x.Name,
                Schema = x.Schema,
                Columns = x.Columns.Select(c => new DatabaseColumnViewModel
                {
                    Name = c.Name,
                    IsNullable = c.IsNullable,
                    IsReadOnly = c.IsReadOnly,
                    DefaultValue = c.DefaultValue,
                    StoreType = c.StoreType,
                })
            });

            var result = new Response
            {
                Value = tables,
            };

            return await Task.FromResult(result.Success());
        }
    }
}

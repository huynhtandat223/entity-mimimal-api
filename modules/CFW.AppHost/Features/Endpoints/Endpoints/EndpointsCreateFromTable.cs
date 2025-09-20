using CFW.AppHost.Features.Core;
using CFW.AppHost.Features.Endpoints.Models;
using CFW.AppHost.Features.Endpoints.ViewModels;
using CFW.AppHost.Infrastructures.DbContextExtensions.Models.Runtimes;
using CFW.AppHost.Infrastructures.DbContextExtensions.Services;
using CFW.AppHost.Infrastructures.DbContextExtensions.Services.Runtimes;
using CFW.AppHost.Infrastructures.DbContextExtensions.Utils;
using CFW.DynamicApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Endpoint = CFW.AppHost.Features.Endpoints.Models.Endpoint;

namespace CFW.AppHost.Features.Endpoints.Endpoints;

public class EndpointsCreateFromTable
{
    public class Request
    {
        public Guid? Id { set; get; }

        public string Path { set; get; } = string.Empty;

        public Models.HttpMethod Method { set; get; }

        public string? Description { set; get; }

        public bool IsAuthenticationRequired { set; get; }

        public EndpointAuthorization? Authorization { set; get; }

        public EndpointODataSupportOptions? ODataOptions { set; get; }

        public string TableName { get; set; } = string.Empty;

        public ContainerConfigurationViewModel ContainerConfiguration { set; get; } = default!;
    }

    public class Response { }

    [ApiOperation("endpoints", RouteName = "/table-sources")]
    public class Handler : IRequestHandler<Request, Response>
    {
        private readonly RuntimeTypeRegistry _runtimeTypeRegistry;
        private readonly ConnectionStringBuilder _connectionStringBuilder;
        private readonly DesignTimeService _designTimeService;
        private readonly AppDbContext _db;

        public Handler(RuntimeTypeRegistry runtimeTypeRegistry
            , ConnectionStringBuilder connectionStringBuilder
            , DesignTimeService designTimeService
            , AppDbContext db)
        {
            _runtimeTypeRegistry = runtimeTypeRegistry;
            _connectionStringBuilder = connectionStringBuilder;
            _designTimeService = designTimeService;
            _db = db;
        }

        public async Task<IResult<Response>> Handle(RequestModel<Request> request, CancellationToken cancellationToken)
        {
            var endpoint = request.Model.JsonConvert<Endpoint>();
            var model = request.Model;
            var result = new Response();

            var serviceProvider = DbContextUtils.CreateDesignTimeServiceProvider(_db);
            var dbModelFactory = serviceProvider.GetRequiredService<IDatabaseModelFactory>();
            var dbModel = dbModelFactory.Create(_db.Database.GetConnectionString()!, new DatabaseModelFactoryOptions());

            var table = dbModel.Tables
                .FirstOrDefault(x => x.Name.Equals(model.TableName, StringComparison.CurrentCultureIgnoreCase));

            if (table is null)
                return result.Notfound();

            if (table.PrimaryKey?.Columns.Count > 1)
                return result.Failed("Can't support primary key with multi column");

            var runtimeEntityDef = new RuntimeEntityDefinition
            {
                Name = model.TableName,
                Namespace = "Default",
                Properties = table.Columns.Select(x => new RuntimeEntityPropertyDefinition
                {
                    Name = x.Name,
                    IsKey = table.PrimaryKey!.Columns[0].Name == x.Name,
                    IsNullable = x.IsNullable,
                    IsRequired = false,
                    Type = x.GetClrType().AssemblyQualifiedName!,
                }).ToList()
            };

            _runtimeTypeRegistry.CreateTypeAddLoad(runtimeEntityDef);
            endpoint.RuntimeEntityDefinition = runtimeEntityDef;

            //Save entity
            _db.Set<Endpoint>().Add(endpoint);
            await _db.SaveChangesAsync();

            endpoint.ContainerConfiguration.Endpoints = null; //remove ref to prevent json serializer failed.
            return result.Created();
        }
    }
}

using CFW.AppHost.Features.Core;
using CFW.AppHost.Features.Databases.ViewModels;
using CFW.AppHost.Infrastructures.DbContextExtensions.Models.Runtimes;
using CFW.AppHost.Infrastructures.DbContextExtensions.Services;
using CFW.AppHost.Infrastructures.DbContextExtensions.Services.Runtimes;
using CFW.AppHost.Infrastructures.DbContextExtensions.Utils;
using CFW.DynamicApi;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Databases.Endpoints;

public class DatabasesCreateTable
{
    public class Response
    {
    }

    [ApiOperation("databases", RouteName = "create-tables")]
    public class Handler : IRequestHandler<TableDefinitionViewModel, Response>
    {
        private readonly DesignTimeService _designTimeService;
        private readonly RuntimeTypeRegistry _runtimeTypeRegistry;
        private readonly AppDbContext _db;
        private readonly RuntimeDbMigrator _runtimeDbMigrator;

        public Handler(DesignTimeService designTimeService
            , AppDbContext db
            , RuntimeTypeRegistry runtimeTypeRegistry
            , RuntimeDbMigrator runtimeDbMigrator)
        {
            _designTimeService = designTimeService;
            _db = db;
            _runtimeTypeRegistry = runtimeTypeRegistry;
            _runtimeDbMigrator = runtimeDbMigrator;
        }

        public async Task<IResult<Response>> Handle(RequestModel<TableDefinitionViewModel> request, CancellationToken cancellationToken)
        {
            
            //var serviceProvider = _designTimeService.CreateDesignTimeServiceProvider(connectionString, provider);

            //var runtimeEntityDefinition = new RuntimeEntityDefinition
            //{
            //    Name = request.Model.Name,
            //    Namespace = request.Model.Namespace,
            //    Properties = request.Model.Properties.Select(c => new RuntimeEntityPropertyDefinition
            //    {
            //        Name = c.Name,
            //        Type = c.Type,
            //        IsKey = c.IsKey,
            //        IsNullable = c.IsNullable,
            //        IsRequired = c.IsRequired
            //    }).ToList()
            //};

            var runtimeEntityDefinition = new RuntimeEntityDefinition
            {
                Name = request.Model.Name,
                Namespace = request.Model.Namespace,
                Properties = new List<RuntimeEntityPropertyDefinition>
                {
                    new RuntimeEntityPropertyDefinition
                    {
                        Name = "Id",
                        Type = typeof(int).AssemblyQualifiedName,
                        IsKey = true,
                        IsNullable = false,
                        IsRequired = true
                    },
                    new RuntimeEntityPropertyDefinition
                    {
                        Name = "Name",
                        Type = typeof(string).AssemblyQualifiedName,
                        IsKey = false,
                        IsNullable = false,
                        IsRequired = true
                    },
                    new RuntimeEntityPropertyDefinition
                    {
                        Name = "Description",
                        Type = typeof(string).AssemblyQualifiedName,
                        IsKey = false,
                        IsNullable = true,
                        IsRequired = false
                    }
                }
            };

            var runtimeType = _runtimeTypeRegistry.CreateTypeAddLoad(runtimeEntityDefinition);
            var runtimeDbContextType = typeof(RuntimeDbContext<>).MakeGenericType(runtimeType);
            var runtimeDbContext = (DbContext)Activator.CreateInstance(runtimeDbContextType
                , DbContextUtils.GetDatabaseProvider(_db.Database.ProviderName)
                , _db.Database.GetConnectionString()!)!;

            await _runtimeDbMigrator.GenerateMigrationScript(runtimeDbContext);
           
            return await Task.FromResult(new Response().Success());
        }
    }
}

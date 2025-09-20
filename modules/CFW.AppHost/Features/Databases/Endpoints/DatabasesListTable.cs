using CFW.AppHost.Features.Core;
using CFW.AppHost.Features.Databases.ViewModels;
using CFW.AppHost.Infrastructures.DbContextExtensions.Models;
using CFW.AppHost.Infrastructures.DbContextExtensions.Services;
using CFW.DynamicApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace CFW.AppHost.Features.Databases.Endpoints;

public class DatabasesListTable
{
    public class Request { }

    public class Response
    {
        public IEnumerable<DatabaseTableViewModel> Value { get; set; } = Enumerable.Empty<DatabaseTableViewModel>();
    }

    [ApiOperation("databases", RouteName = "tables")]
    public class Handler : IRequestHandler<Request, Response>
    {
        private readonly DesignTimeService _designTimeService;
        private readonly ConnectionStringBuilder _connectionStringBuilder;
        private readonly AppRequestContext _appRequestContext;
        private readonly AppDbContext _db;

        public Handler(DesignTimeService designTimeService, ConnectionStringBuilder connectionStringBuilder
            , AppRequestContext appRequestContext
            , AppDbContext db)
        {
            _designTimeService = designTimeService;
            _connectionStringBuilder = connectionStringBuilder;
            _appRequestContext = appRequestContext;
            _db = db;
        }

        public async Task<IResult<Response>> Handle(RequestModel<Request> request, CancellationToken cancellationToken)
        {
            var model = request.Model;
            var connectionString = _db.Database.GetConnectionString()!;
            var providerName = _db.Database.ProviderName ?? throw new InvalidOperationException("Database provider is not configured.");
            var provider = providerName switch
            {
                "Microsoft.EntityFrameworkCore.SqlServer" => DatabaseProvider.MSSQL,
                "Npgsql.EntityFrameworkCore.PostgreSQL" => DatabaseProvider.PostgreSQL,
                "Pomelo.EntityFrameworkCore.MySql" or "MySql.Data.EntityFrameworkCore" => DatabaseProvider.MySQL,
                "Oracle.EntityFrameworkCore" => DatabaseProvider.Oracle,
                "Microsoft.EntityFrameworkCore.Sqlite" => DatabaseProvider.Sqlite,
                _ => throw new NotSupportedException($"Database provider '{providerName}' is not supported."),
            };

            var serviceProvider = _designTimeService.CreateDesignTimeServiceProvider(connectionString, provider);
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
                    StoreType = GetTypeName(c),
                })
            });

            var result = new Response
            {
                Value = tables,
            };

            return await Task.FromResult(result.Success());
        }

        private string? GetTypeName(DatabaseColumn databaseColumn)
        {
            var annotations = databaseColumn.GetAnnotations();
            var runtimeTypeObj = annotations.FirstOrDefault(a => a.Name == "ClrType")?.Value;

            if (runtimeTypeObj is null)
            {
                return databaseColumn.StoreType;
            }

            var fullName = runtimeTypeObj.ToString();

            return fullName;
        }
    }
}

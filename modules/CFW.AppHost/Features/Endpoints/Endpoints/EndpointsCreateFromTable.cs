using CFW.AppHost.Features.Endpoints.Models;
using CFW.AppHost.Features.Endpoints.Services;
using CFW.AppHost.Features.Shared;
using CFW.AppHost.Infrastructures.DbContextExtensions.Models;
using CFW.AppHost.Infrastructures.DbContextExtensions.Services;
using CFW.DynamicApi;
using Microsoft.EntityFrameworkCore.Scaffolding;
using System.Text.RegularExpressions;
using Endpoint = CFW.AppHost.Features.Endpoints.Models.Endpoint;

namespace CFW.AppHost.Features.Endpoints.Endpoints;

public class EndpointsCreateFromTable
{
    public class Request
    {
        public Guid Id { set; get; }

        public string Path { set; get; } = string.Empty;

        public Models.HttpMethod Method { set; get; }

        public string? Description { set; get; }

        public bool IsAuthenticationRequired { set; get; }

        public EndpointAuthorization? Authorization { set; get; }

        public EndpointODataSupportOptions? ODataOptions { set; get; }

        public string TableName { get; set; } = string.Empty;

        public DatabaseConfiguration DatabaseConfiguration { get; set; } = default!;

        public Models.ContainerConfiguration ContainerConfiguration { set; get; } = default!;
    }

    [ApiOperation("endpoints/tables")]
    public class Handler : IRequestHandler<Request, Endpoint>
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

        public async Task<IResult<Endpoint>> Handle(RequestModel<Request> request, CancellationToken cancellationToken)
        {
            var result = request.Model.JsonConvert<Endpoint>();
            var model = request.Model;
            var dbConfig = model.DatabaseConfiguration;
            var connectionString = _connectionStringBuilder.BuildConnectionString(dbConfig);

            var serviceProvider = _designTimeService.CreateDesignTimeServiceProvider(connectionString, dbConfig.DatabaseProvider);
            var dbModelFactory = serviceProvider.GetRequiredService<IDatabaseModelFactory>();
            var dbModel = dbModelFactory.Create(connectionString, new DatabaseModelFactoryOptions());

            var table = dbModel.Tables.FirstOrDefault(x => x.Name.Equals(model.TableName, StringComparison.CurrentCultureIgnoreCase));

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
                    Type = GetClrType(x.StoreType!).AssemblyQualifiedName!,
                }).ToList()
            };

            _runtimeTypeRegistry.CreateTypeAddLoad(runtimeEntityDef);
            result.RuntimeEntityDefinition = runtimeEntityDef;

            //Save entity
            _db.Set<Endpoint>().Add(result);
            await _db.SaveChangesAsync();

            result.ContainerConfiguration.Endpoints = null; //remove ref to prevent json serializer failed.
            return result.Created();
        }

        private static readonly Regex LengthRegex = new Regex(@"\((max|\d+)\)", RegexOptions.Compiled);

        private static Type GetClrType(string sqlType)
        {
            if (string.IsNullOrWhiteSpace(sqlType))
                throw new ArgumentNullException(nameof(sqlType));

            // Normalize
            var type = sqlType.Trim().ToLowerInvariant();
            var baseType = LengthRegex.Replace(type, ""); // strip (xxx) or (max)

            return baseType switch
            {
                "uniqueidentifier" => typeof(Guid),
                "nvarchar" => typeof(string),
                "varchar" => typeof(string),
                "nchar" => typeof(string),
                "char" => typeof(string),
                "text" => typeof(string),
                "ntext" => typeof(string),

                "bit" => typeof(bool),
                "int" => typeof(int),
                "bigint" => typeof(long),
                "smallint" => typeof(short),
                "tinyint" => typeof(byte),
                "decimal" or "numeric" => typeof(decimal),
                "money" or "smallmoney" => typeof(decimal),
                "float" => typeof(double),
                "real" => typeof(float),

                "date" or "datetime" or "datetime2" or "smalldatetime"
                                   => typeof(DateTime),
                "datetimeoffset" => typeof(DateTimeOffset),
                "time" => typeof(TimeSpan),

                "binary" or "varbinary" or "image"
                                   => typeof(byte[]),

                _ => throw new NotImplementedException()// fallback if unknown
            };
        }

    }
}

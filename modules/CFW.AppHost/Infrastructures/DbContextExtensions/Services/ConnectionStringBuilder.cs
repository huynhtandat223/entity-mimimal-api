using CFW.AppHost.Infrastructures.DbContextExtensions.Models;
using CFW.Core.Dependencies;
using Microsoft.Data.SqlClient;

namespace CFW.AppHost.Infrastructures.DbContextExtensions.Services;

public class ConnectionStringBuilder : ISingletonService
{
    public string BuildConnectionString(DatabaseConfiguration config)
    {
        var provider = config.DatabaseProvider;
        return provider switch
        {
            DatabaseProvider.MSSQL => BuildMSSQLConnectionString(config.MSSQL!),
            DatabaseProvider.PostgreSQL => BuildPostgreSQLConnectionString(config.PostgreSQL!),
            DatabaseProvider.MySQL => BuildMySQLConnectionString(config.MySQL!),
            DatabaseProvider.Oracle => BuildOracleConnectionString(config.Oracle!),
            _ => throw new ArgumentException($"Unsupported database provider: {provider}")
        };
    }

    private string BuildMSSQLConnectionString(MSSQLConfig config)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = config.Port.HasValue ? $"{config.Server},{config.Port}" : config.Server,
            InitialCatalog = config.Database,
            IntegratedSecurity = config.IntegratedSecurity,
            Encrypt = config.Encrypt,
            TrustServerCertificate = config.TrustServerCertificate,
            MultipleActiveResultSets = config.MultipleActiveResultSets
        };

        if (!config.IntegratedSecurity && !string.IsNullOrEmpty(config.Username))
        {
            builder.UserID = config.Username;
            builder.Password = config.Password ?? string.Empty;
        }

        if (config.ConnectionTimeout.HasValue)
            builder.ConnectTimeout = config.ConnectionTimeout.Value;

        if (config.CommandTimeout.HasValue)
            builder.CommandTimeout = config.CommandTimeout.Value;

        return builder.ConnectionString;
    }

    private string BuildPostgreSQLConnectionString(PostgreSQLConfig config)
    {
        var parts = new List<string>
            {
                $"Host={config.Host}",
                $"Database={config.Database}"
            };

        if (config.Port.HasValue)
            parts.Add($"Port={config.Port}");

        if (!string.IsNullOrEmpty(config.Username))
        {
            parts.Add($"Username={config.Username}");
            if (!string.IsNullOrEmpty(config.Password))
                parts.Add($"Password={config.Password}");
        }

        if (!string.IsNullOrEmpty(config.Schema) && config.Schema != "public")
            parts.Add($"SearchPath={config.Schema}");

        parts.Add($"SSL Mode={config.SslMode switch
        {
            PostgreSQLSslMode.Disable => "Disable",
            PostgreSQLSslMode.Require => "Require",
            PostgreSQLSslMode.VerifyCA => "VerifyCA",
            PostgreSQLSslMode.VerifyFull => "VerifyFull",
            _ => "Require"
        }}");

        if (config.ConnectionTimeout.HasValue)
            parts.Add($"Timeout={config.ConnectionTimeout}");

        if (config.CommandTimeout.HasValue)
            parts.Add($"CommandTimeout={config.CommandTimeout}");

        return string.Join(";", parts);
    }

    private string BuildMySQLConnectionString(MySQLConfig config)
    {
        var parts = new List<string>
            {
                $"Server={config.Host}",
                $"Database={config.Database}"
            };

        if (config.Port.HasValue)
            parts.Add($"Port={config.Port}");

        if (!string.IsNullOrEmpty(config.Username))
        {
            parts.Add($"Uid={config.Username}");
            if (!string.IsNullOrEmpty(config.Password))
                parts.Add($"Pwd={config.Password}");
        }

        if (!string.IsNullOrEmpty(config.Charset))
            parts.Add($"CharSet={config.Charset}");

        parts.Add($"SslMode={config.SslMode}");

        if (config.ConnectionTimeout.HasValue)
            parts.Add($"ConnectionTimeout={config.ConnectionTimeout}");

        if (config.CommandTimeout.HasValue)
            parts.Add($"DefaultCommandTimeout={config.CommandTimeout}");

        return string.Join(";", parts);
    }

    private string BuildOracleConnectionString(OracleConfig config)
    {
        var parts = new List<string>();

        if (config.ConnectionType == OracleConnectionType.Basic)
        {
            var port = config.Port ?? 1521;
            parts.Add($"Data Source={config.Host}:{port}/{config.ServiceName}");
        }
        else
        {
            parts.Add($"Data Source={config.ServiceName}");
        }

        if (!string.IsNullOrEmpty(config.Username))
        {
            parts.Add($"User Id={config.Username}");
            if (!string.IsNullOrEmpty(config.Password))
                parts.Add($"Password={config.Password}");
        }

        if (config.ConnectionTimeout.HasValue)
            parts.Add($"Connection Timeout={config.ConnectionTimeout}");

        return string.Join(";", parts);
    }

    //public async Task<bool> TestConnectionAsync(DatabaseProvider provider, DatabaseConfiguration config)
    //{
    //    try
    //    {
    //        var connectionString = BuildConnectionString(provider, config);

    //        return provider switch
    //        {
    //            DatabaseProvider.MSSQL => await TestMSSQLConnectionAsync(connectionString),
    //            DatabaseProvider.PostgreSQL => await TestPostgreSQLConnectionAsync(connectionString),
    //            DatabaseProvider.MySQL => await TestMySQLConnectionAsync(connectionString),
    //            DatabaseProvider.Oracle => await TestOracleConnectionAsync(connectionString),
    //            _ => false
    //        };
    //    }
    //    catch
    //    {
    //        return false;
    //    }
    //}

    private async Task<bool> TestMSSQLConnectionAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection.State == System.Data.ConnectionState.Open;
    }

    private async Task<bool> TestPostgreSQLConnectionAsync(string connectionString)
    {
        // Using Npgsql
        // using var connection = new Npgsql.NpgsqlConnection(connectionString);
        // await connection.OpenAsync();
        // return connection.State == System.Data.ConnectionState.Open;

        // Placeholder - implement with your PostgreSQL provider
        await Task.Delay(100);
        return true;
    }

    private async Task<bool> TestMySQLConnectionAsync(string connectionString)
    {
        // Using MySqlConnector or MySql.Data
        // using var connection = new MySqlConnector.MySqlConnection(connectionString);
        // await connection.OpenAsync();
        // return connection.State == System.Data.ConnectionState.Open;

        // Placeholder - implement with your MySQL provider
        await Task.Delay(100);
        return true;
    }

    private async Task<bool> TestOracleConnectionAsync(string connectionString)
    {
        // Using Oracle.ManagedDataAccess
        // using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString);
        // await connection.OpenAsync();
        // return connection.State == System.Data.ConnectionState.Open;

        // Placeholder - implement with your Oracle provider
        await Task.Delay(100);
        return true;
    }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CFW.AppHost.Infrastructures.DbContextExtensions.Models;

public class DatabaseConfiguration
{
    public DatabaseProvider DatabaseProvider { get; set; }

    public MSSQLConfig? MSSQL { get; set; }
    public PostgreSQLConfig? PostgreSQL { get; set; }
    public MySQLConfig? MySQL { get; set; }
    public OracleConfig? Oracle { get; set; }
}

public class MSSQLConfig
{
    [Required]
    public string Server { get; set; } = string.Empty;

    public int? Port { get; set; }

    [Required]
    public string Database { get; set; } = string.Empty;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool IntegratedSecurity { get; set; } = false;

    public bool Encrypt { get; set; } = true;

    public bool TrustServerCertificate { get; set; } = true;

    public bool MultipleActiveResultSets { get; set; } = true;

    public int? ConnectionTimeout { get; set; }

    public int? CommandTimeout { get; set; }
}

public class PostgreSQLConfig
{
    [Required]
    public string Host { get; set; } = string.Empty;

    public int? Port { get; set; }

    [Required]
    public string Database { get; set; } = string.Empty;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Schema { get; set; } = "public";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PostgreSQLSslMode SslMode { get; set; } = PostgreSQLSslMode.Require;

    public int? ConnectionTimeout { get; set; }

    public int? CommandTimeout { get; set; }
}

public class MySQLConfig
{
    [Required]
    public string Host { get; set; } = string.Empty;

    public int? Port { get; set; }

    [Required]
    public string Database { get; set; } = string.Empty;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Charset { get; set; } = "utf8mb4";

    public bool SslMode { get; set; } = true;

    public int? ConnectionTimeout { get; set; }

    public int? CommandTimeout { get; set; }
}

public class OracleConfig
{
    [Required]
    public string Host { get; set; } = string.Empty;

    public int? Port { get; set; }

    [Required]
    public string ServiceName { get; set; } = string.Empty;

    public string? Username { get; set; }

    public string? Password { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OracleConnectionType ConnectionType { get; set; } = OracleConnectionType.Basic;

    public int? ConnectionTimeout { get; set; }

    public int? CommandTimeout { get; set; }
}

// Enums for specific configurations
public enum PostgreSQLSslMode
{
    Disable,
    Require,
    VerifyCA,
    VerifyFull
}

public enum OracleConnectionType
{
    Basic,
    TNS
}


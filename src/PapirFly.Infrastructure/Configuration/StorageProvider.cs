namespace PapirFly.Infrastructure.Configuration;

/// <summary>Identifies the supported article storage providers.</summary>
public enum StorageProvider
{
    /// <summary>Stores articles in memory for the lifetime of one application host.</summary>
    InMemory,

    /// <summary>Stores articles durably in a PostgreSQL database.</summary>
    PostgreSql
}

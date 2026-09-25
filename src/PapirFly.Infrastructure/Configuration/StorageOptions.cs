namespace PapirFly.Infrastructure.Configuration;

/// <summary>Configures the article storage provider and its connection.</summary>
public sealed class StorageOptions
{
    /// <summary>The application configuration section containing storage settings.</summary>
    public const string SectionName = "Storage";

    /// <summary>Gets or sets the provider; InMemory is the default.</summary>
    public StorageProvider Provider { get; set; } = StorageProvider.InMemory;

    /// <summary>Gets or sets the PostgreSQL connection string, required only for PostgreSql.</summary>
    public string? ConnectionString { get; set; }
}

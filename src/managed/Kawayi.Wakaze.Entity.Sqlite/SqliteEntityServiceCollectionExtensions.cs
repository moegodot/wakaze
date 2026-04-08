using Kawayi.Wakaze.Entity.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kawayi.Wakaze.Entity.Sqlite;

/// <summary>
/// Registers the SQLite-backed entity store in a dependency injection container.
/// </summary>
public static class SqliteEntityServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SQLite-backed entity store using a configuration callback.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configure">The callback that configures the store options.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddSqliteEntityStore(
        this IServiceCollection services,
        Action<SqliteEntityStoreOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SqliteEntityStoreOptions();
        configure(options);

        return services.AddSqliteEntityStore(options);
    }

    /// <summary>
    /// Registers the SQLite-backed entity store using a preconfigured options object.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="options">The store options.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddSqliteEntityStore(
        this IServiceCollection services,
        SqliteEntityStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        var frozenOptions = options.CloneValidated();

        services.AddSingleton(frozenOptions);
        services.AddSingleton<SqliteEntityStoreMigrator>();
        services.AddSingleton<SqliteEntityStore>();
        services.AddSingleton<IEntityStore>(static provider => provider.GetRequiredService<SqliteEntityStore>());
        services.AddSingleton<IEntityReader>(static provider => provider.GetRequiredService<SqliteEntityStore>());
        services.AddSingleton<IEntityHistoryReader>(static provider => provider.GetRequiredService<SqliteEntityStore>());
        services.AddSingleton<IEntitySnapshotSource>(static provider => provider.GetRequiredService<SqliteEntityStore>());
        services.AddSingleton<IEntityAtomicExecutor>(static provider => provider.GetRequiredService<SqliteEntityStore>());
        services.AddSingleton<IEntityGraphWriter>(static provider => provider.GetRequiredService<SqliteEntityStore>());

        return services;
    }
}

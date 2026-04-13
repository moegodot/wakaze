using Kawayi.Wakaze.Db.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kawayi.Wakaze.Db.PostgreSql;

/// <summary>
/// Registers the PostgreSQL database provider in a dependency injection container.
/// </summary>
public static class PostgreSqlServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PostgreSQL provider using a configuration callback.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configure">The callback that configures provider options.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddPostgreSqlDatabaseProvider(
        this IServiceCollection services,
        Action<PostgreSqlDatabaseProviderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new PostgreSqlDatabaseProviderOptions();
        configure(options);

        return services.AddPostgreSqlDatabaseProvider(options);
    }

    /// <summary>
    /// Registers the PostgreSQL provider using a preconfigured options object.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddPostgreSqlDatabaseProvider(
        this IServiceCollection services,
        PostgreSqlDatabaseProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        var frozenOptions = options.CloneValidated();

        services.AddSingleton(frozenOptions);
        services.AddSingleton<PostgreSqlDatabaseProvider>();
        services.AddSingleton<IDatabaseProvider>(static provider =>
            provider.GetRequiredService<PostgreSqlDatabaseProvider>());
        services.AddSingleton<IDatabaseMaintenanceService>(static provider =>
            provider.GetRequiredService<PostgreSqlDatabaseProvider>());
        services.AddSingleton<IDatabaseDumper>(static provider =>
            provider.GetRequiredService<PostgreSqlDatabaseProvider>());
        services.AddSingleton<IDatabaseRestorer>(static provider =>
            provider.GetRequiredService<PostgreSqlDatabaseProvider>());

        return services;
    }
}

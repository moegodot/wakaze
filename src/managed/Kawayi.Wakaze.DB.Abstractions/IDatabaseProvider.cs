using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;

namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Describes provider metadata and the core behaviors used to resolve and provision database resources.
/// </summary>
public interface IDatabaseProvider : IDatabaseResolver, IDatabaseProvisioner
{
    /// <summary>
    /// Gets the stable provider identifier as an exact <c>database-provder://</c> schema identifier.
    /// </summary>
    SchemaId<DatabaseProviderScheme> ProviderId { get; }

    /// <summary>
    /// Gets the supported engine as an exact <c>database://</c> schema identifier.
    /// </summary>
    SchemaId<DatabaseScheme> Engine { get; }

    /// <summary>
    /// Gets a human-readable provider display name.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the optional ADO.NET provider invariant name exposed by the provider.
    /// </summary>
    string? AdoNetProviderInvariantName { get; }

    /// <summary>
    /// Gets the optional capabilities exposed by the provider.
    /// </summary>
    DatabaseCapabilities Capabilities { get; }
}

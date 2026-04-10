namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Captures the stable description of a database resource.
/// </summary>
/// <param name="ProviderId">The stable provider identifier used to access the resource.</param>
/// <param name="Engine">The engine family exposed by the resource.</param>
/// <param name="Location">
/// The location model for the resource. Opaque locations carry a versioned provider-specific document.
/// </param>
/// <param name="DisplayName">A human-readable label for diagnostics and UI.</param>
/// <param name="Capabilities">Optional capabilities exposed by the resource or provider.</param>
public sealed record DatabaseDescriptor(
    DatabaseProviderId ProviderId,
    DatabaseEngine Engine,
    DatabaseLocation Location,
    string DisplayName,
    DatabaseCapabilities Capabilities = DatabaseCapabilities.None);

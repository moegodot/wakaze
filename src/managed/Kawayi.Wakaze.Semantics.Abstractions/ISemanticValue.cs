using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Represents an immutable semantic payload.
/// </summary>
/// <remarks>
/// Implementations are expected to behave as immutable values.
/// The public contract intentionally does not expose storage, database, or blob concerns.
/// </remarks>
public interface ISemanticValue
{
    /// <summary>
    /// Gets the semantic type that identifies the payload schema.
    /// </summary>
    TypeUri Type { get; }
}

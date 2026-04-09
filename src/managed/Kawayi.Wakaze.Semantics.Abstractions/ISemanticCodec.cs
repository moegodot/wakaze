using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Serializes and deserializes a semantic value type.
/// </summary>
public interface ISemanticCodec
{
    /// <summary>
    /// Gets the semantic type handled by the codec.
    /// </summary>
    TypeUri Type { get; }

    /// <summary>
    /// Deserializes a semantic payload into an immutable semantic value.
    /// </summary>
    /// <param name="payload">The payload to deserialize.</param>
    /// <returns>The deserialized semantic value.</returns>
    ISemanticValue Deserialize(SemanticPayload payload);

    /// <summary>
    /// Serializes an immutable semantic value into a payload.
    /// </summary>
    /// <param name="value">The semantic value to serialize.</param>
    /// <returns>The serialized payload.</returns>
    SemanticPayload Serialize(ISemanticValue value);
}

using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Represents a serialized semantic payload.
/// </summary>
public readonly struct SemanticPayload : IEquatable<SemanticPayload>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticPayload"/> struct.
    /// </summary>
    /// <param name="schema">The exact semantic schema of the payload.</param>
    /// <param name="format">The serialization format identifier.</param>
    /// <param name="content">The serialized payload bytes.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="format"/> is empty or whitespace.</exception>
    public SemanticPayload(
        SchemaId schema,
        string format,
        ReadOnlyMemory<byte> content)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("The payload format cannot be empty.", nameof(format));
        }

        Schema = schema;
        Format = format;
        Content = content;
    }

    /// <summary>
    /// Gets the exact semantic schema of the payload.
    /// </summary>
    public SchemaId Schema { get; }

    /// <summary>
    /// Gets the serialization format identifier.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Gets the serialized payload bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Content { get; }

    /// <summary>
    /// Determines whether the current payload is equal to another payload.
    /// </summary>
    /// <param name="other">The payload to compare with the current value.</param>
    /// <returns><see langword="true"/> when the payload values are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(SemanticPayload other)
    {
        return Schema.Equals(other.Schema)
               && string.Equals(Format, other.Format, StringComparison.Ordinal)
               && Content.Span.SequenceEqual(other.Content.Span);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current payload.
    /// </summary>
    /// <param name="obj">The object to compare with the current payload.</param>
    /// <returns><see langword="true"/> when the specified object is an equal <see cref="SemanticPayload"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is SemanticPayload other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current payload.
    /// </summary>
    /// <returns>A hash code for the current payload.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Schema);
        hash.Add(Format, StringComparer.Ordinal);

        foreach (var value in Content.Span)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Compares two payload values for equality.
    /// </summary>
    /// <param name="left">The first payload.</param>
    /// <param name="right">The second payload.</param>
    /// <returns><see langword="true"/> when the payloads are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(SemanticPayload left, SemanticPayload right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two payload values for inequality.
    /// </summary>
    /// <param name="left">The first payload.</param>
    /// <param name="right">The second payload.</param>
    /// <returns><see langword="true"/> when the payloads are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(SemanticPayload left, SemanticPayload right)
    {
        return !left.Equals(right);
    }
}

using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Represents a canonical semantic property extracted for indexing.
/// </summary>
public readonly struct SemanticProperty : IEquatable<SemanticProperty>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticProperty"/> struct.
    /// </summary>
    /// <param name="entityId">The entity identifier that owns the property.</param>
    /// <param name="revision">The entity revision that produced the property.</param>
    /// <param name="ownerSchema">The exact semantic schema that owns the property.</param>
    /// <param name="propertyKey">The property key within the owning semantic value.</param>
    /// <param name="valueKind">The canonical value kind.</param>
    /// <param name="canonicalValue">The canonical string representation of the property value.</param>
    /// <param name="ordinal">The zero-based ordinal within the property set.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="propertyKey"/> or <paramref name="canonicalValue"/> is empty or whitespace,
    /// when <paramref name="ordinal"/> is negative,
    /// or when <paramref name="revision"/> does not belong to <paramref name="entityId"/>.
    /// </exception>
    public SemanticProperty(
        EntityId entityId,
        EntityRevision revision,
        SchemaId ownerSchema,
        string propertyKey,
        SemanticPropertyValueKind valueKind,
        string canonicalValue,
        int ordinal)
    {
        if (revision.EntityId != entityId)
        {
            throw new ArgumentException(
                "The revision must belong to the entity.",
                nameof(revision));
        }

        if (string.IsNullOrWhiteSpace(propertyKey))
        {
            throw new ArgumentException("The property key cannot be empty.", nameof(propertyKey));
        }

        if (string.IsNullOrWhiteSpace(canonicalValue))
        {
            throw new ArgumentException("The canonical value cannot be empty.", nameof(canonicalValue));
        }

        if (ordinal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "The ordinal cannot be negative.");
        }

        EntityId = entityId;
        Revision = revision;
        OwnerSchema = ownerSchema;
        PropertyKey = propertyKey;
        ValueKind = valueKind;
        CanonicalValue = canonicalValue;
        Ordinal = ordinal;
    }

    /// <summary>
    /// Gets the entity identifier that owns the property.
    /// </summary>
    public EntityId EntityId { get; }

    /// <summary>
    /// Gets the entity revision that produced the property.
    /// </summary>
    public EntityRevision Revision { get; }

    /// <summary>
    /// Gets the exact semantic schema that owns the property.
    /// </summary>
    public SchemaId OwnerSchema { get; }

    /// <summary>
    /// Gets the property key within the owning semantic value.
    /// </summary>
    public string PropertyKey { get; }

    /// <summary>
    /// Gets the canonical value kind.
    /// </summary>
    public SemanticPropertyValueKind ValueKind { get; }

    /// <summary>
    /// Gets the canonical string representation of the property value.
    /// </summary>
    public string CanonicalValue { get; }

    /// <summary>
    /// Gets the zero-based ordinal within the property set.
    /// </summary>
    public int Ordinal { get; }

    /// <summary>
    /// Determines whether the current property is equal to another property.
    /// </summary>
    /// <param name="other">The property to compare with the current value.</param>
    /// <returns><see langword="true"/> when the properties are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(SemanticProperty other)
    {
        return EntityId.Equals(other.EntityId)
               && Revision.Equals(other.Revision)
               && OwnerSchema.Equals(other.OwnerSchema)
               && string.Equals(PropertyKey, other.PropertyKey, StringComparison.Ordinal)
               && ValueKind == other.ValueKind
               && string.Equals(CanonicalValue, other.CanonicalValue, StringComparison.Ordinal)
               && Ordinal == other.Ordinal;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current property.
    /// </summary>
    /// <param name="obj">The object to compare with the current property.</param>
    /// <returns><see langword="true"/> when the specified object is an equal <see cref="SemanticProperty"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is SemanticProperty other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current property.
    /// </summary>
    /// <returns>A hash code for the current property.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            EntityId,
            Revision,
            OwnerSchema,
            StringComparer.Ordinal.GetHashCode(PropertyKey),
            ValueKind,
            StringComparer.Ordinal.GetHashCode(CanonicalValue),
            Ordinal);
    }

    /// <summary>
    /// Compares two semantic properties for equality.
    /// </summary>
    /// <param name="left">The first property.</param>
    /// <param name="right">The second property.</param>
    /// <returns><see langword="true"/> when the properties are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(SemanticProperty left, SemanticProperty right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two semantic properties for inequality.
    /// </summary>
    /// <param name="left">The first property.</param>
    /// <param name="right">The second property.</param>
    /// <returns><see langword="true"/> when the properties are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(SemanticProperty left, SemanticProperty right)
    {
        return !left.Equals(right);
    }
}

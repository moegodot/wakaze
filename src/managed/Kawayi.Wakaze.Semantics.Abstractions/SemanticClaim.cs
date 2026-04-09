using System.Collections.Immutable;
using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Represents the semantic state associated with a single entity revision basis.
/// </summary>
/// <remarks>
/// A semantic claim contains one primary semantic value and zero or more extension values
/// attached to the same entity. Extension values are facets of the same entity and do not
/// represent nested semantic claims for referenced entities.
/// </remarks>
public sealed class SemanticClaim
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticClaim"/> class with no extensions.
    /// </summary>
    /// <param name="basisRevision">The entity revision that the semantic state is based on.</param>
    /// <param name="primaryValue">The primary semantic value of the entity.</param>
    public SemanticClaim(
        EntityRevision basisRevision,
        ISemanticValue primaryValue)
        : this(
            basisRevision,
            primaryValue,
            ImmutableDictionary<TypeUri, ISemanticValue>.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticClaim"/> class.
    /// </summary>
    /// <param name="basisRevision">The entity revision that the semantic state is based on.</param>
    /// <param name="primaryValue">The primary semantic value of the entity.</param>
    /// <param name="extensions">Additional semantic facets attached to the same entity.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="primaryValue"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when an extension entry contains a <see langword="null"/> value,
    /// when an extension key does not match the value type,
    /// or when the extension set contains the primary value type.
    /// </exception>
    public SemanticClaim(
        EntityRevision basisRevision,
        ISemanticValue primaryValue,
        ImmutableDictionary<TypeUri, ISemanticValue> extensions)
    {
        ArgumentNullException.ThrowIfNull(primaryValue);

        BasisRevision = basisRevision;
        PrimaryValue = primaryValue;
        Extensions = ValidateExtensions(primaryValue, extensions);
    }

    /// <summary>
    /// Gets the entity revision that the semantic state is based on.
    /// </summary>
    public EntityRevision BasisRevision { get; }

    /// <summary>
    /// Gets the primary semantic value of the entity.
    /// </summary>
    public ISemanticValue PrimaryValue { get; }

    /// <summary>
    /// Gets the additional semantic facets attached to the same entity.
    /// </summary>
    public ImmutableDictionary<TypeUri, ISemanticValue> Extensions { get; }

    private static ImmutableDictionary<TypeUri, ISemanticValue> ValidateExtensions(
        ISemanticValue primaryValue,
        ImmutableDictionary<TypeUri, ISemanticValue> extensions)
    {
        ArgumentNullException.ThrowIfNull(extensions);

        foreach (var pair in extensions)
        {
            if (pair.Value is null)
            {
                throw new ArgumentException("Semantic extensions cannot contain null values.", nameof(extensions));
            }

            if (pair.Key != pair.Value.Type)
            {
                throw new ArgumentException(
                    "Semantic extension keys must match the semantic value type.",
                    nameof(extensions));
            }

            if (pair.Key == primaryValue.Type)
            {
                throw new ArgumentException(
                    "Semantic extensions cannot include the primary semantic value type.",
                    nameof(extensions));
            }
        }

        return extensions;
    }
}

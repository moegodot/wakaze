using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Represents a lightweight semantic reference to another entity.
/// </summary>
/// <param name="Target">The referenced entity identifier.</param>
/// <param name="ExpectedPrimaryType">
/// An optional expected primary semantic family for the referenced entity.
/// </param>
public readonly record struct SemanticEntityRef(
    EntityId Target,
    TypeUri? ExpectedPrimaryType = null);

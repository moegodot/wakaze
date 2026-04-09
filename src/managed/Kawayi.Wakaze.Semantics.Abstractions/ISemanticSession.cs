using System.Diagnostics.CodeAnalysis;
using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Represents an editable semantic session for a single entity.
/// </summary>
public interface ISemanticSession
{
    /// <summary>
    /// Gets the entity identifier being edited.
    /// </summary>
    EntityId EntityId { get; }

    /// <summary>
    /// Gets the revision that the session was opened against.
    /// </summary>
    EntityRevision BasisRevision { get; }

    /// <summary>
    /// Gets the current semantic state tracked by the session.
    /// </summary>
    SemanticClaim Current { get; }

    /// <summary>
    /// Attempts to retrieve a semantic value by type.
    /// </summary>
    /// <param name="type">The semantic type to locate.</param>
    /// <param name="value">
    /// When this method returns <see langword="true"/>, contains the matching semantic value;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> when a matching semantic value is present; otherwise, <see langword="false"/>.</returns>
    bool TryGet(
        TypeUri type,
        [NotNullWhen(true)] out ISemanticValue? value);

    /// <summary>
    /// Attempts to retrieve a semantic value by type and cast it to a specific semantic value type.
    /// </summary>
    /// <typeparam name="TValue">The semantic value type to retrieve.</typeparam>
    /// <param name="type">The semantic type to locate.</param>
    /// <param name="value">
    /// When this method returns <see langword="true"/>, contains the matching semantic value;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> when a matching semantic value is present; otherwise, <see langword="false"/>.</returns>
    bool TryGet<TValue>(
        TypeUri type,
        [NotNullWhen(true)] out TValue? value)
        where TValue : class, ISemanticValue;

    /// <summary>
    /// Replaces the current primary semantic value.
    /// </summary>
    /// <param name="value">The new primary semantic value.</param>
    void SetPrimary(ISemanticValue value);

    /// <summary>
    /// Adds or replaces an extension semantic value.
    /// </summary>
    /// <param name="value">The extension semantic value to add or replace.</param>
    /// <remarks>
    /// The value type identifies the extension slot and must not match the current primary value type.
    /// </remarks>
    void SetExtension(ISemanticValue value);

    /// <summary>
    /// Removes an extension semantic value by type.
    /// </summary>
    /// <param name="type">The extension semantic type to remove.</param>
    /// <returns>
    /// <see langword="true"/> when an extension was removed; otherwise, <see langword="false"/>.
    /// </returns>
    bool RemoveExtension(TypeUri type);

    /// <summary>
    /// Applies a semantic command to the session.
    /// </summary>
    /// <param name="command">The command to apply.</param>
    void Apply(ISemanticCommand command);
}

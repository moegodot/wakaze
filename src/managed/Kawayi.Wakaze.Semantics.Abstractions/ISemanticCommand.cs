namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Represents a command that mutates semantic state within a session.
/// </summary>
public interface ISemanticCommand
{
    /// <summary>
    /// Applies the command to the supplied session.
    /// </summary>
    /// <param name="session">The target session.</param>
    void Apply(ISemanticSession session);
}

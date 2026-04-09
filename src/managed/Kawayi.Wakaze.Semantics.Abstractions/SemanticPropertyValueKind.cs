namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Identifies the canonical value kind of an extracted semantic property.
/// </summary>
public enum SemanticPropertyValueKind : byte
{
    /// <summary>
    /// Identifies a canonical string value.
    /// </summary>
    String = 0,

    /// <summary>
    /// Identifies a canonical 64-bit integer value.
    /// </summary>
    Int64 = 1,

    /// <summary>
    /// Identifies a canonical decimal value.
    /// </summary>
    Decimal = 2,

    /// <summary>
    /// Identifies a canonical Boolean value.
    /// </summary>
    Boolean = 3,

    /// <summary>
    /// Identifies a canonical instant value.
    /// </summary>
    Instant = 4,

    /// <summary>
    /// Identifies a canonical JSON value.
    /// </summary>
    Json = 5,
}

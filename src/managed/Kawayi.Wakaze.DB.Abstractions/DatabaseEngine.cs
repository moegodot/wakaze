using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Identifies a database engine through a versioned schema identifier.
/// </summary>
public readonly struct DatabaseEngine : IEquatable<DatabaseEngine>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseEngine"/> struct from a versioned schema identifier.
    /// </summary>
    /// <param name="value">The engine schema identifier.</param>
    public DatabaseEngine(UriTypeSchema value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseEngine"/> struct from a versioned schema URI.
    /// </summary>
    /// <param name="value">The engine schema identifier.</param>
    public DatabaseEngine(Uri value)
        : this(new UriTypeSchema(value.AbsoluteUri))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseEngine"/> struct from a versioned schema URI string.
    /// </summary>
    /// <param name="value">The engine schema identifier.</param>
    public DatabaseEngine(string value)
        : this(new UriTypeSchema(value))
    {
    }

    /// <summary>
    /// Gets the exact schema identifier for the engine.
    /// </summary>
    public UriTypeSchema Value { get; }

    /// <summary>
    /// Determines whether the current engine identifier equals another identifier.
    /// </summary>
    /// <param name="other">The other engine identifier.</param>
    /// <returns><see langword="true"/> when both identifiers are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(DatabaseEngine other)
    {
        return Value.Equals(other.Value);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current engine identifier.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when the object is an equal engine identifier; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is DatabaseEngine other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current engine identifier.
    /// </summary>
    /// <returns>A hash code for the current engine identifier.</returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// Returns the absolute typed URI text of the current engine identifier.
    /// </summary>
    /// <returns>The absolute typed URI text.</returns>
    public override string ToString()
    {
        return Value.ToString();
    }

    /// <summary>
    /// Compares two engine identifiers for equality.
    /// </summary>
    /// <param name="left">The first engine identifier.</param>
    /// <param name="right">The second engine identifier.</param>
    /// <returns><see langword="true"/> when both identifiers are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(DatabaseEngine left, DatabaseEngine right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two engine identifiers for inequality.
    /// </summary>
    /// <param name="left">The first engine identifier.</param>
    /// <param name="right">The second engine identifier.</param>
    /// <returns><see langword="true"/> when the identifiers are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(DatabaseEngine left, DatabaseEngine right)
    {
        return !left.Equals(right);
    }

}

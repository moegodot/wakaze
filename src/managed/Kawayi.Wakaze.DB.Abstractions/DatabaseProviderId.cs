using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Identifies a database provider through a versioned schema identifier.
/// </summary>
public readonly struct DatabaseProviderId : IEquatable<DatabaseProviderId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseProviderId"/> struct from a versioned schema identifier.
    /// </summary>
    /// <param name="value">The provider schema identifier.</param>
    public DatabaseProviderId(UriTypeSchema value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseProviderId"/> struct from a versioned schema URI.
    /// </summary>
    /// <param name="value">The provider schema identifier.</param>
    public DatabaseProviderId(Uri value)
        : this(new UriTypeSchema(value.AbsoluteUri))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseProviderId"/> struct from a versioned schema URI string.
    /// </summary>
    /// <param name="value">The provider schema identifier.</param>
    public DatabaseProviderId(string value)
        : this(new UriTypeSchema(value))
    {
    }

    /// <summary>
    /// Gets the exact provider schema identifier.
    /// </summary>
    public UriTypeSchema Value { get; }

    /// <summary>
    /// Determines whether the current provider identifier equals another identifier.
    /// </summary>
    /// <param name="other">The other provider identifier.</param>
    /// <returns><see langword="true"/> when both identifiers are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(DatabaseProviderId other)
    {
        return Value.Equals(other.Value);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current provider identifier.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when the object is an equal provider identifier; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is DatabaseProviderId other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current provider identifier.
    /// </summary>
    /// <returns>A hash code for the current provider identifier.</returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// Returns the absolute typed URI text of the current provider identifier.
    /// </summary>
    /// <returns>The absolute typed URI text.</returns>
    public override string ToString()
    {
        return Value.ToString();
    }

    /// <summary>
    /// Compares two provider identifiers for equality.
    /// </summary>
    /// <param name="left">The first provider identifier.</param>
    /// <param name="right">The second provider identifier.</param>
    /// <returns><see langword="true"/> when both identifiers are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(DatabaseProviderId left, DatabaseProviderId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two provider identifiers for inequality.
    /// </summary>
    /// <param name="left">The first provider identifier.</param>
    /// <param name="right">The second provider identifier.</param>
    /// <returns><see langword="true"/> when the identifiers are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(DatabaseProviderId left, DatabaseProviderId right)
    {
        return !left.Equals(right);
    }

}

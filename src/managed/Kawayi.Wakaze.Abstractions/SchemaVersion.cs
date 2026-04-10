namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Represents a positive schema version number.
/// </summary>
public readonly struct SchemaVersion : IEquatable<SchemaVersion>, IComparable<SchemaVersion>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaVersion"/> struct.
    /// </summary>
    /// <param name="value">The positive schema version number.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is zero.
    /// </exception>
    public SchemaVersion(uint value)
    {
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "The schema version must be positive.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the schema version number.
    /// </summary>
    public uint Value { get; }

    /// <summary>
    /// Compares the current version with another version.
    /// </summary>
    /// <param name="other">The other version.</param>
    /// <returns>
    /// A signed integer that indicates the relative order of the versions.
    /// </returns>
    public int CompareTo(SchemaVersion other)
    {
        return Value.CompareTo(other.Value);
    }

    /// <summary>
    /// Determines whether the current version equals another version.
    /// </summary>
    /// <param name="other">The other version.</param>
    /// <returns><see langword="true"/> when both versions are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(SchemaVersion other)
    {
        return Value == other.Value;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current version.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when the object is an equal version; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is SchemaVersion other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current version.
    /// </summary>
    /// <returns>A hash code for the current version.</returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// Returns the canonical version segment text for the current version.
    /// </summary>
    /// <returns>The canonical version segment text.</returns>
    public override string ToString()
    {
        return $"v{Value}";
    }

    /// <summary>
    /// Compares two versions for equality.
    /// </summary>
    /// <param name="left">The first version.</param>
    /// <param name="right">The second version.</param>
    /// <returns><see langword="true"/> when both versions are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(SchemaVersion left, SchemaVersion right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two versions for inequality.
    /// </summary>
    /// <param name="left">The first version.</param>
    /// <param name="right">The second version.</param>
    /// <returns><see langword="true"/> when the versions are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(SchemaVersion left, SchemaVersion right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Compares two versions for ordering.
    /// </summary>
    /// <param name="left">The first version.</param>
    /// <param name="right">The second version.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> is less than <paramref name="right"/>.</returns>
    public static bool operator <(SchemaVersion left, SchemaVersion right)
    {
        return left.Value < right.Value;
    }

    /// <summary>
    /// Compares two versions for ordering.
    /// </summary>
    /// <param name="left">The first version.</param>
    /// <param name="right">The second version.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> is greater than <paramref name="right"/>.</returns>
    public static bool operator >(SchemaVersion left, SchemaVersion right)
    {
        return left.Value > right.Value;
    }

    internal static bool TryParseSegment(string value, out SchemaVersion version)
    {
        version = default;

        if (value.Length < 2 || value[0] != 'v')
        {
            return false;
        }

        var digits = value.AsSpan(1);
        if (digits.Length == 0)
        {
            return false;
        }

        foreach (var digit in digits)
        {
            if (!char.IsAsciiDigit(digit))
            {
                return false;
            }
        }

        if (digits.Length > 1 && digits[0] == '0')
        {
            return false;
        }

        if (!uint.TryParse(digits, out var parsed) || parsed == 0)
        {
            return false;
        }

        version = new SchemaVersion(parsed);
        return true;
    }
}

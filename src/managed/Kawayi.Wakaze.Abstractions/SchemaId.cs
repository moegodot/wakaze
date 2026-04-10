namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Represents an exact schema identifier.
/// </summary>
public readonly struct SchemaId : IEquatable<SchemaId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaId"/> struct.
    /// </summary>
    /// <param name="family">The versionless schema family identifier.</param>
    /// <param name="version">The exact schema version.</param>
    public SchemaId(SchemaFamily family, SchemaVersion version)
    {
        Family = family;
        Version = version;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaId"/> struct from a versioned schema identifier string.
    /// </summary>
    /// <param name="value">The versioned schema URI string.</param>
    public SchemaId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!TryParse(value, out var parsed))
        {
            throw new ArgumentException("The value is not a valid schema identifier.", nameof(value));
        }

        Family = parsed.Family;
        Version = parsed.Version;
    }

    /// <summary>
    /// Gets the versionless schema family identifier.
    /// </summary>
    public SchemaFamily Family { get; }

    /// <summary>
    /// Gets the exact schema version.
    /// </summary>
    public SchemaVersion Version { get; }

    /// <summary>
    /// Determines whether the current schema equals another schema.
    /// </summary>
    /// <param name="other">The other schema.</param>
    /// <returns><see langword="true"/> when both schemas are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(SchemaId other)
    {
        return Family.Equals(other.Family) && Version.Equals(other.Version);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current schema.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when the object is an equal schema; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is SchemaId other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current schema.
    /// </summary>
    /// <returns>A hash code for the current schema.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Family, Version);
    }

    /// <summary>
    /// Returns the canonical versioned URI text for the current schema.
    /// </summary>
    /// <returns>The canonical versioned URI text.</returns>
    public override string ToString()
    {
        return $"{Family}/{Version}";
    }

    /// <summary>
    /// Compares two schemas for equality.
    /// </summary>
    /// <param name="left">The first schema.</param>
    /// <param name="right">The second schema.</param>
    /// <returns><see langword="true"/> when both schemas are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(SchemaId left, SchemaId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two schemas for inequality.
    /// </summary>
    /// <param name="left">The first schema.</param>
    /// <param name="right">The second schema.</param>
    /// <returns><see langword="true"/> when the schemas are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(SchemaId left, SchemaId right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Attempts to parse a schema identifier from text.
    /// </summary>
    /// <param name="value">The URI string to parse.</param>
    /// <param name="schema">
    /// When this method returns <see langword="true"/>, contains the parsed schema identifier;
    /// otherwise, the default value.
    /// </param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out SchemaId schema)
    {
        schema = default;

        if (value is null)
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return TryParse(uri, out schema);
    }

    private static bool TryParse(Uri value, out SchemaId schema)
    {
        schema = default;

        try
        {
            SchemaFamily.ValidateFamilyUri(value);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var pathSegments = value.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        if (pathSegments.Length < 2)
        {
            return false;
        }

        var versionSegment = pathSegments[^1];
        if (!SchemaVersion.TryParseSegment(versionSegment, out var version))
        {
            return false;
        }

        var builder = new UriBuilder(value)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            Path = string.Join("/", pathSegments[..^1].Select(Uri.EscapeDataString))
        };

        var family = SchemaFamily.FromValidatedUri(builder.Uri);
        schema = new SchemaId(family, version);
        return true;
    }
}

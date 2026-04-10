namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Represents an exact versioned type schema identity.
/// </summary>
public readonly struct UriTypeSchema : IEquatable<UriTypeSchema>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UriTypeSchema"/> struct.
    /// </summary>
    /// <param name="typeUri">The versionless type family identifier.</param>
    /// <param name="version">The exact schema version.</param>
    public UriTypeSchema(TypeUri typeUri, TypeVersion version)
    {
        TypeUri = typeUri;
        Version = version;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UriTypeSchema"/> struct from a versioned URI string.
    /// </summary>
    /// <param name="value">The versioned schema URI string.</param>
    public UriTypeSchema(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!TryParse(value, out var parsed))
        {
            throw new ArgumentException("The value is not a valid versioned type schema URI.", nameof(value));
        }

        TypeUri = parsed.TypeUri;
        Version = parsed.Version;
    }

    /// <summary>
    /// Gets the versionless type family identifier.
    /// </summary>
    public TypeUri TypeUri { get; }

    /// <summary>
    /// Gets the exact schema version.
    /// </summary>
    public TypeVersion Version { get; }

    /// <summary>
    /// Determines whether the current schema equals another schema.
    /// </summary>
    /// <param name="other">The other schema.</param>
    /// <returns><see langword="true"/> when both schemas are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(UriTypeSchema other)
    {
        return TypeUri.Equals(other.TypeUri) && Version.Equals(other.Version);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current schema.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when the object is an equal schema; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is UriTypeSchema other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current schema.
    /// </summary>
    /// <returns>A hash code for the current schema.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(TypeUri, Version);
    }

    /// <summary>
    /// Returns the canonical versioned URI text for the current schema.
    /// </summary>
    /// <returns>The canonical versioned URI text.</returns>
    public override string ToString()
    {
        return $"{TypeUri}/{Version}";
    }

    /// <summary>
    /// Compares two schemas for equality.
    /// </summary>
    /// <param name="left">The first schema.</param>
    /// <param name="right">The second schema.</param>
    /// <returns><see langword="true"/> when both schemas are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(UriTypeSchema left, UriTypeSchema right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two schemas for inequality.
    /// </summary>
    /// <param name="left">The first schema.</param>
    /// <param name="right">The second schema.</param>
    /// <returns><see langword="true"/> when the schemas are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(UriTypeSchema left, UriTypeSchema right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Attempts to parse a versioned type schema URI from text.
    /// </summary>
    /// <param name="value">The URI string to parse.</param>
    /// <param name="schema">
    /// When this method returns <see langword="true"/>, contains the parsed versioned schema;
    /// otherwise, the default value.
    /// </param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out UriTypeSchema schema)
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

    private static bool TryParse(Uri value, out UriTypeSchema schema)
    {
        schema = default;

        try
        {
            TypeUri.ValidateFamilyUri(value);
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
        if (!TypeVersion.TryParseSegment(versionSegment, out var version))
        {
            return false;
        }

        var builder = new UriBuilder(value)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            Path = string.Join("/", pathSegments[..^1].Select(Uri.EscapeDataString))
        };

        var typeUri = TypeUri.FromValidatedUri(builder.Uri);
        schema = new UriTypeSchema(typeUri, version);
        return true;
    }
}

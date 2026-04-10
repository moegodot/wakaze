namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Represents a validated versionless schema family identifier.
/// </summary>
/// <remarks>
/// A schema family identifies a contract family such as <c>semantic://example.com/tag</c>.
/// Version information is modeled separately through <see cref="SchemaId"/>.
/// Query, fragment, explicit port, user info, and a trailing slash are not permitted.
/// </remarks>
public readonly struct SchemaFamily : IEquatable<SchemaFamily>
{
    private SchemaFamily(Uri value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaFamily"/> struct from a URI string.
    /// </summary>
    /// <param name="value">The URI string to parse and validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is not a valid absolute URI
    /// or does not satisfy schema family constraints.
    /// </exception>
    public SchemaFamily(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!TryParse(value, out var parsed))
            throw new ArgumentException("The value is not a valid schema family identifier.", nameof(value));

        Value = parsed.Value;
    }

    /// <summary>
    /// Gets the validated URI value.
    /// </summary>
    public Uri Value { get; }

    /// <summary>
    /// Determines whether the current value is equal to another schema family.
    /// </summary>
    /// <param name="other">The other schema family to compare with.</param>
    /// <returns><see langword="true"/> when both values are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(SchemaFamily other)
    {
        return EqualityComparer<Uri>.Default.Equals(Value, other.Value);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current schema family.
    /// </summary>
    /// <param name="obj">The object to compare with the current value.</param>
    /// <returns><see langword="true"/> when the specified object is an equal <see cref="SchemaFamily"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is SchemaFamily other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current schema family.
    /// </summary>
    /// <returns>A hash code for the current schema family.</returns>
    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Returns the absolute URI text for the current schema family.
    /// </summary>
    /// <returns>The absolute URI text.</returns>
    public override string ToString()
    {
        return Value?.AbsoluteUri ?? string.Empty;
    }

    /// <summary>
    /// Compares two schema families for equality.
    /// </summary>
    /// <param name="left">The first schema family.</param>
    /// <param name="right">The second schema family.</param>
    /// <returns><see langword="true"/> when the schema families are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(SchemaFamily left, SchemaFamily right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two schema families for inequality.
    /// </summary>
    /// <param name="left">The first schema family.</param>
    /// <param name="right">The second schema family.</param>
    /// <returns><see langword="true"/> when the schema families are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(SchemaFamily left, SchemaFamily right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Attempts to parse a schema family identifier from text.
    /// </summary>
    /// <param name="value">The URI string to parse.</param>
    /// <param name="family">
    /// When this method returns <see langword="true"/>, contains the parsed schema family;
    /// otherwise, the default value.
    /// </param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out SchemaFamily family)
    {
        family = default;

        if (value is null) return false;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;

        try
        {
            ValidateFamilyUri(uri);
        }
        catch (ArgumentException)
        {
            return false;
        }

        family = new SchemaFamily(uri);
        return true;
    }

    internal static SchemaFamily FromValidatedUri(Uri value)
    {
        return new SchemaFamily(value);
    }

    internal static void ValidateFamilyUri(Uri value)
    {
        if (!value.IsAbsoluteUri) throw new ArgumentException("The URI must be absolute.", nameof(value));

        if (string.IsNullOrWhiteSpace(value.Host))
            throw new ArgumentException("The URI must include a host name.", nameof(value));

        if (!string.IsNullOrEmpty(value.Query))
            throw new ArgumentException("The URI must not include a query string.", nameof(value));

        if (!string.IsNullOrEmpty(value.Fragment))
            throw new ArgumentException("The URI must not include a fragment.", nameof(value));

        if (!value.IsDefaultPort)
            throw new ArgumentException("The URI must not include an explicit port.", nameof(value));

        if (!string.IsNullOrEmpty(value.UserInfo))
            throw new ArgumentException("The URI must not include user info.", nameof(value));

        var absolutePath = value.AbsolutePath;
        if (string.IsNullOrEmpty(absolutePath) || absolutePath == "/")
            throw new ArgumentException("The URI must include at least one path segment.", nameof(value));

        if (absolutePath[^1] == '/')
            throw new ArgumentException("The URI path must not end with a slash.", nameof(value));

        var pathSegments = absolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        if (pathSegments.Length == 0)
            throw new ArgumentException("The URI must include at least one path segment.", nameof(value));
    }
}

namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Represents a validated versionless type family URI.
/// </summary>
/// <remarks>
/// A type family URI identifies a contract family such as <c>semantic://example.com/tag</c>.
/// Version information is modeled separately through <see cref="UriTypeSchema"/>.
/// Query, fragment, explicit port, user info, and a trailing slash are not permitted.
/// </remarks>
public readonly struct TypeUri : IEquatable<TypeUri>
{
    private TypeUri(Uri value)
    {
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeUri"/> struct from a URI string.
    /// </summary>
    /// <param name="value">The URI string to parse and validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is not a valid absolute URI
    /// or does not satisfy typed URI constraints.
    /// </exception>
    public TypeUri(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!TryParse(value, out var parsed))
            throw new ArgumentException("The value is not a valid type family URI.", nameof(value));

        Value = parsed.Value;
    }

    /// <summary>
    /// Gets the validated URI value.
    /// </summary>
    public Uri Value { get; }

    /// <summary>
    /// Determines whether the current value is equal to another typed URI.
    /// </summary>
    /// <param name="other">The other typed URI to compare with.</param>
    /// <returns><see langword="true"/> when both values are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(TypeUri other)
    {
        return EqualityComparer<Uri>.Default.Equals(Value, other.Value);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current typed URI.
    /// </summary>
    /// <param name="obj">The object to compare with the current value.</param>
    /// <returns><see langword="true"/> when the specified object is an equal <see cref="TypeUri"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is TypeUri other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current family URI.
    /// </summary>
    /// <returns>A hash code for the current family URI.</returns>
    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Returns the absolute URI text for the current family URI.
    /// </summary>
    /// <returns>The absolute URI text.</returns>
    public override string ToString()
    {
        return Value?.AbsoluteUri ?? string.Empty;
    }

    /// <summary>
    /// Compares two typed URIs for equality.
    /// </summary>
    /// <param name="left">The first typed URI.</param>
    /// <param name="right">The second typed URI.</param>
    /// <returns><see langword="true"/> when the typed URIs are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(TypeUri left, TypeUri right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two typed URIs for inequality.
    /// </summary>
    /// <param name="left">The first typed URI.</param>
    /// <param name="right">The second typed URI.</param>
    /// <returns><see langword="true"/> when the typed URIs are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(TypeUri left, TypeUri right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Attempts to parse a type family URI from text.
    /// </summary>
    /// <param name="value">The URI string to parse.</param>
    /// <param name="typeUri">
    /// When this method returns <see langword="true"/>, contains the parsed type family URI;
    /// otherwise, the default value.
    /// </param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out TypeUri typeUri)
    {
        typeUri = default;

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

        typeUri = new TypeUri(uri);
        return true;
    }

    internal static TypeUri FromValidatedUri(Uri value)
    {
        return new TypeUri(value);
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

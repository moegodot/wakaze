namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Represents a validated semantic type URI.
/// </summary>
/// <remarks>
/// Valid values must use the <c>type</c> scheme, include a host, include at least one path segment,
/// and end with a version segment in the form <c>v{uint}</c>.
/// Query, fragment, explicit port, and user info are not permitted.
/// </remarks>
public readonly struct TypeUri : IEquatable<TypeUri>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeUri"/> struct from an absolute <see cref="Uri"/>.
    /// </summary>
    /// <param name="value">The URI value to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not a valid typed URI.</exception>
    public TypeUri(Uri value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Validate(value);
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
        : this(ParseUri(value))
    {
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
    /// Returns a hash code for the current typed URI.
    /// </summary>
    /// <returns>A hash code for the current typed URI.</returns>
    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Returns the absolute URI text for the current typed URI.
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

    private static Uri ParseUri(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            throw new ArgumentException("The value is not a valid absolute URI.", nameof(value));

        return uri;
    }

    private static void Validate(Uri value)
    {
        if (!value.IsAbsoluteUri) throw new ArgumentException("The URI must be absolute.", nameof(value));

        if (!string.Equals(value.Scheme, "type", StringComparison.Ordinal))
            throw new ArgumentException("The URI scheme must be 'type'.", nameof(value));

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

        var versionSegment = pathSegments[^1];
        if (!TryValidateVersionSegment(versionSegment))
            throw new ArgumentException(
                "The final path segment must match 'v{uint}' without leading zeroes.",
                nameof(value));
    }

    private static bool TryValidateVersionSegment(string versionSegment)
    {
        if (versionSegment.Length < 2 || versionSegment[0] != 'v') return false;

        var digits = versionSegment.AsSpan(1);
        if (digits.Length == 0) return false;

        foreach (var digit in digits)
            if (!char.IsAsciiDigit(digit))
                return false;

        if (digits.Length > 1 && digits[0] == '0') return false;

        return uint.TryParse(digits, out _);
    }
}

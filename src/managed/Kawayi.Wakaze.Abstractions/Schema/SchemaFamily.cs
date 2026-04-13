using System.Diagnostics.CodeAnalysis;

namespace Kawayi.Wakaze.Abstractions.Schema;

/// <summary>
/// Represents a validated versionless schema family identifier.
/// </summary>
/// <remarks>
/// A schema family identifies a contract family such as <c>semantic://example.com/tag</c>.
/// Version information is modeled separately through <see cref="SchemaId"/>.
/// Query, fragment, explicit port, user info, and a trailing slash are not permitted.
/// </remarks>
public readonly struct SchemaFamily : IEquatable<SchemaFamily>, IParsable<SchemaFamily>
{
    public string Kind { get; }
    public string Authority { get; }
    public string Namespace { get; }


    public static bool TryCombineToUrl(string kind, string authority, string @namespace, out Uri? result)
    {
        return Uri.TryCreate($"{kind}{Uri.SchemeDelimiter}{authority}/{@namespace}", UriKind.Absolute, out result);
    }

    public Uri ToUri()
    {
        return new Uri(ToString(), UriKind.Absolute);
    }

    public SchemaFamily(string s)
    {
        var family = Parse(s, null);
        Kind = family.Kind;
        Authority = family.Authority;
        Namespace = family.Namespace;
    }

    public SchemaFamily(string kind, string authority, string @namespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);

        if (Uri.CheckSchemeName(kind))
            throw new ArgumentException("The kind is not a valid URI scheme name.", nameof(kind));
        if (UriHostNameType.Dns != Uri.CheckHostName(authority))
            throw new ArgumentException("The authority is not a valid host name or the UriHostNameType isn't Dns.",
                nameof(authority));
        if (@namespace.StartsWith('/') || @namespace.EndsWith('/'))
            throw new ArgumentException("The namespace cann't start or end with slash", nameof(@namespace));

        Kind = kind;
        Authority = authority;
        Namespace = @namespace;
    }

    /// <summary>
    /// Determines whether the current value is equal to another schema family.
    /// </summary>
    /// <param name="other">The other schema family to compare with.</param>
    /// <returns><see langword="true"/> when both values are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(SchemaFamily other)
    {
        return Kind == other.Kind && Authority == other.Authority && Namespace == other.Namespace;
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
        return HashCode.Combine(Kind, Authority, Namespace);
    }

    /// <summary>
    /// Returns the absolute URI text for the current schema family.
    /// </summary>
    /// <returns>The absolute URI text.</returns>
    public override string ToString()
    {
        return $"{Kind}{Uri.SchemeDelimiter}{Authority}/{Namespace}";
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
    public static bool TryParse(string? value, [NotNullWhen(true)] out SchemaFamily family,
        [NotNullWhen(false)] out string? errorMsg)
    {
        family = default;

        if (value is null)
        {
            errorMsg = "The argument value is null.";
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            errorMsg = "The argument value is not a valid absolute URI.";
            return false;
        }

        if (!TryValidateFamilyUri(uri, out family, out var error))
        {
            errorMsg = $"The value is not a valid schema family:{error}";
            return false;
        }

        errorMsg = null;
        return true;
    }

    private SchemaFamily(Uri value)
    {
        Kind = value.Scheme;
        Authority = value.Authority;
        Namespace = string.Join('/', value.AbsolutePath.Split('/')[0..^1]);
    }

    public static bool TryValidateFamilyUri(Uri value, [NotNullWhen(true)] out SchemaFamily family,
        [NotNullWhen(false)] out string? error)
    {
        family = default;
        if (!value.IsAbsoluteUri)
        {
            error = "The URI must be absolute.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(value.Host))
        {
            error = "The URI must include a host name.";
            return false;
        }

        if (!string.IsNullOrEmpty(value.Query))
        {
            error = "The URI must not include a query string.";
            return false;
        }

        if (!string.IsNullOrEmpty(value.Fragment))
        {
            error = "The URI must not include a fragment.";
            return false;
        }

        if (!value.IsDefaultPort)
        {
            error = "The URI must not include an explicit port.";
            return false;
        }

        if (!string.IsNullOrEmpty(value.UserInfo))
        {
            error = "The URI must not include user info.";
            return false;
        }

        var absolutePath = value.AbsolutePath;
        if (string.IsNullOrEmpty(absolutePath) || absolutePath == "/")
        {
            error = "The URI must include at least one path segment.";
            return false;
        }

        if (absolutePath[^1] == '/')
        {
            error = "The URI path must not end with a slash.";
            return false;
        }

        var pathSegments = absolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        if (pathSegments.Length == 0)
        {
            error = "The URI must include at least one path segment.";
            return false;
        }

        error = null;
        family = new SchemaFamily(value);
        return true;
    }

    public static SchemaFamily Parse(string s, IFormatProvider? _provider)
    {
        if (!TryParse(s, out var result, out var errMsg))
            throw new FormatException("The value is not a valid schema family.",
                new ArgumentException(errMsg, nameof(s)));
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? _provider,
        [MaybeNullWhen(false)] out SchemaFamily result)
    {
        return TryParse(s, out result, out _);
    }
}

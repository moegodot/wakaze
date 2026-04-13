using System.Diagnostics.CodeAnalysis;

namespace Kawayi.Wakaze.Abstractions.Schema;

/// <summary>
/// Represents an exact schema identifier.
/// </summary>
public readonly record struct SchemaId : IParsable<SchemaId>
{
    public SchemaId(SchemaFamily family, SchemaVersion version)
    {
        Family = family;
        Version = version;
    }

    public SchemaId(string s) : this(ParseArgument(s))
    {
    }

    private SchemaId(SchemaId value) : this(value.Family, value.Version)
    {
    }

    public SchemaFamily Family { get; }

    public SchemaVersion Version { get; }

    public override string ToString()
    {
        return $"{Family}/{Version}";
    }

    public static bool TryParse(string? s, [NotNullWhen(true)] out SchemaId result,
        [NotNullWhen(false)] out string? error)
    {
        if (s is null)
        {
            error = "The value is null.";
            result = default;
            return false;
        }

        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
        {
            error = "The value is not a valid absolute URI.";
            result = default;
            return false;
        }

        if (!TryValidateSchemaUri(uri, out var pathSegments, out error))
        {
            result = default;
            return false;
        }

        var verSeg = pathSegments[^1];

        if (string.IsNullOrWhiteSpace(verSeg))
        {
            error = "The version segment at the absolute path is null.";
            result = default;
            return false;
        }

        if (!verSeg.StartsWith('v'))
        {
            error = "The version segment at the absolute path must start with 'v'.";
            result = default;
            return false;
        }

        var versionText = verSeg[1..];
        if (versionText.Length > 1 && versionText[0] == '0')
        {
            error = "The version segment at the absolute path is not a valid version string.";
            result = default;
            return false;
        }

        if (!SchemaVersion.TryParse(versionText, null, out var ver))
        {
            error = "The version segment at the absolute path is not a valid version string.";
            result = default;
            return false;
        }

        var family = new SchemaFamily(
            uri.Scheme,
            uri.Authority,
            string.Join("/", pathSegments.Take(pathSegments.Length - 1)));

        result = new SchemaId(family, ver);
        error = null;
        return true;
    }

    public static SchemaId Parse(string s, IFormatProvider? provider)
    {
        if (!TryParse(s, out var result, out var error))
            throw new FormatException("the schema id format is invalid.", new ArgumentException(error, nameof(s)));

        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out SchemaId result)
    {
        return TryParse(s, out result, out _);
    }

    private static SchemaId ParseArgument(string s)
    {
        ArgumentNullException.ThrowIfNull(s);

        if (!TryParse(s, out var result, out var error))
            throw new ArgumentException(error, nameof(s));

        return result;
    }

    private static bool TryValidateSchemaUri(
        Uri uri,
        [NotNullWhen(true)] out string[]? pathSegments,
        [NotNullWhen(false)] out string? error)
    {
        pathSegments = null;

        if (!SchemaFamily.TryValidateFamilyUri(uri, out _, out error)) return false;

        pathSegments = SchemaFamily.GetPathSegments(uri.AbsolutePath);
        if (pathSegments.Length < 2)
        {
            error = "The version segment at the absolute path must start with 'v'.";
            pathSegments = null;
            return false;
        }

        return true;
    }
}

using System.Diagnostics.CodeAnalysis;

namespace Kawayi.Wakaze.Abstractions.Schema;

/// <summary>
/// Represents an exact schema identifier.
/// </summary>
public readonly record struct SchemaId(SchemaFamily Family, SchemaVersion Version) : IParsable<SchemaId>
{
    public SchemaId(string s) : this(Parse(s, null).Family, Parse(s, null).Version)
    {
    }

    public static bool TryParse(string s, [NotNullWhen(true)] out SchemaId result,
        [NotNullWhen(false)] out string? error)
    {
        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
        {
            error = "The value is not a valid absolute URI.";
            result = default;
            return false;
        }

        if (!SchemaFamily.TryValidateFamilyUri(uri, out var family, out error))
        {
            result = default;
            return false;
        }

        var verSeg = uri.AbsolutePath.Split('/')[^1];

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

        if (!SchemaVersion.TryParse(verSeg[1..], null, out var ver))
        {
            error = "The version segment at the absolute path is not a valid version string.";
            result = default;
            return false;
        }

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
        return TryParse(s, provider, out result);
    }
}

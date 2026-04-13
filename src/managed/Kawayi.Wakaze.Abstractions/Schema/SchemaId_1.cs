using System.Diagnostics.CodeAnalysis;

namespace Kawayi.Wakaze.Abstractions.Schema;

public readonly record struct SchemaId<T> : IParsable<SchemaId<T>> where T : ISchemaUriSchemeDefinition
{
    public readonly SchemaId Id;

    public SchemaFamily Family => Id.Family;
    public SchemaVersion Version => Id.Version;

    public SchemaId(SchemaId id)
    {
        if (id.Family.Kind != T.UriScheme)
            throw new ArgumentException($"Invalid schema id: {id.Family},expected {T.UriScheme}");
        Id = id;
    }

    public SchemaId(SchemaFamily family, SchemaVersion version) : this(new SchemaId(family, version))
    {
    }

    public SchemaId(string id)
    {
        Id = Parse(id, null).Id;
    }

    public static SchemaId<T> Parse(string s, IFormatProvider? provider)
    {
        var id = SchemaId.Parse(s, provider);
        return new SchemaId<T>(id);
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out SchemaId<T> result)
    {
        result = default;

        if (!SchemaId.TryParse(s, provider, out var id)) return false;

        if (id.Family.Kind != T.UriScheme) return false;

        result = new SchemaId<T>(id);
        return true;
    }

    public override string ToString()
    {
        return Id.ToString();
    }

    public static implicit operator SchemaId(SchemaId<T> d)
    {
        return d.Id;
    }

    public static explicit operator SchemaId<T>(SchemaId d)
    {
        return new SchemaId<T>(d);
    }
}

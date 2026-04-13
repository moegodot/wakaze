using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;

namespace Kawayi.Wakaze.Generator.Sample;

public sealed class SemanticScheme : ISchemaUriSchemeDefinition
{
    public static string UriScheme => "semantic";
}

public sealed class FooFamily : ISchemaFamilyDefinition<SemanticScheme>
{
    public static SchemaFamily Family => new("semantic://wakaze.dev/foo");
}

public sealed class BarFamily : ISchemaFamilyDefinition<SemanticScheme>
{
    public static SchemaFamily Family => new("semantic://wakaze.dev/bar");
}

[RegisterSchema]
public sealed class BarV1Schema : ISchemaDefinition<BarFamily, SemanticScheme>
{
    public static SchemaId Schema => new("semantic://wakaze.dev/bar/v1");

    public static IReadOnlyList<SchemaId> CompatibleTargets => [];

    public static IReadOnlyList<SchemaId> ProjectableTargets => [];
}

[RegisterSchema]
public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
{
    public static SchemaId Schema => new("semantic://wakaze.dev/foo/v1");

    public static IReadOnlyList<SchemaId> CompatibleTargets => [];

    public static IReadOnlyList<SchemaId> ProjectableTargets => [BarV1Schema.Schema];

    [ProjectTo(typeof(BarV1Schema))]
    internal static BarValue ProjectToBar(FooValue source)
    {
        return new BarValue(BarV1Schema.Schema, source.Value);
    }
}

public sealed record FooValue(SchemaId SchemaId, string Value) : ITypedObject;

public sealed record BarValue(SchemaId SchemaId, string Value) : ITypedObject;

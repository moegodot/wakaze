using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;

namespace Kawayi.Wakaze.Abstractions.Tests;

public sealed class SemanticScheme : ISchemaUriSchemeDefinition
{
    public static string UriScheme => "semantic";
}

public sealed class DatabaseScheme : ISchemaUriSchemeDefinition
{
    public static string UriScheme => "database";
}

public sealed class TagFamily : ISchemaFamilyDefinition<SemanticScheme>
{
    public static SchemaFamily Family => new("semantic://wakaze.dev/tag");
}

public sealed class OtherFamily : ISchemaFamilyDefinition<SemanticScheme>
{
    public static SchemaFamily Family => new("semantic://wakaze.dev/other");
}

public sealed class PostgreSqlFamily : ISchemaFamilyDefinition<DatabaseScheme>
{
    public static SchemaFamily Family => new("database://wakaze.dev/postgresql");
}

public sealed class TagV1Schema : ISchemaDefinition<TagFamily, SemanticScheme>
{
    public static SchemaId Schema => new("semantic://wakaze.dev/tag/v1");

    public static IReadOnlyList<SchemaId> CompatibleTargets => [];

    public static IReadOnlyList<SchemaId> ProjectableTargets => [];
}

public sealed class TagV2Schema : ISchemaDefinition<TagFamily, SemanticScheme>
{
    public static SchemaId Schema => new("semantic://wakaze.dev/tag/v2");

    public static IReadOnlyList<SchemaId> CompatibleTargets => [TagV1Schema.Schema];

    public static IReadOnlyList<SchemaId> ProjectableTargets => [TagV1Schema.Schema];
}

public sealed class TagV3Schema : ISchemaDefinition<TagFamily, SemanticScheme>
{
    public static SchemaId Schema => new("semantic://wakaze.dev/tag/v3");

    public static IReadOnlyList<SchemaId> CompatibleTargets => [TagV2Schema.Schema];

    public static IReadOnlyList<SchemaId> ProjectableTargets => [TagV2Schema.Schema];
}

public sealed class OtherV1Schema : ISchemaDefinition<OtherFamily, SemanticScheme>
{
    public static SchemaId Schema => new("semantic://wakaze.dev/other/v1");

    public static IReadOnlyList<SchemaId> CompatibleTargets => [];

    public static IReadOnlyList<SchemaId> ProjectableTargets => [];
}

public sealed class PostgreSqlV1Schema : ISchemaDefinition<PostgreSqlFamily, DatabaseScheme>
{
    public static SchemaId Schema => new("database://wakaze.dev/postgresql/v1");

    public static IReadOnlyList<SchemaId> CompatibleTargets => [];

    public static IReadOnlyList<SchemaId> ProjectableTargets => [];
}

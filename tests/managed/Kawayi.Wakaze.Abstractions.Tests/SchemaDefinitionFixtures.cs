using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public sealed class SemanticScheme : IUriSchemeDefinition
{
    public static string UriScheme => "semantic";
}

public sealed class DatabaseScheme : IUriSchemeDefinition
{
    public static string UriScheme => "database";
}

public sealed class TagFamily : ITypeFamilyDefinition<SemanticScheme>
{
    public static TypeUri TypeUri => new("semantic://wakaze.dev/tag");
}

public sealed class OtherFamily : ITypeFamilyDefinition<SemanticScheme>
{
    public static TypeUri TypeUri => new("semantic://wakaze.dev/other");
}

public sealed class PostgreSqlFamily : ITypeFamilyDefinition<DatabaseScheme>
{
    public static TypeUri TypeUri => new("database://wakaze.dev/postgresql");
}

public sealed class TagV1Schema : ISchemaDefinition<TagFamily, SemanticScheme>
{
    public static UriTypeSchema Schema => new("semantic://wakaze.dev/tag/v1");

    public static IReadOnlyList<UriTypeSchema> CompatibleTargets => [];

    public static IReadOnlyList<UriTypeSchema> ProjectableTargets => [];
}

public sealed class TagV2Schema : ISchemaDefinition<TagFamily, SemanticScheme>
{
    public static UriTypeSchema Schema => new("semantic://wakaze.dev/tag/v2");

    public static IReadOnlyList<UriTypeSchema> CompatibleTargets => [TagV1Schema.Schema];

    public static IReadOnlyList<UriTypeSchema> ProjectableTargets => [TagV1Schema.Schema];
}

public sealed class TagV3Schema : ISchemaDefinition<TagFamily, SemanticScheme>
{
    public static UriTypeSchema Schema => new("semantic://wakaze.dev/tag/v3");

    public static IReadOnlyList<UriTypeSchema> CompatibleTargets => [TagV2Schema.Schema];

    public static IReadOnlyList<UriTypeSchema> ProjectableTargets => [TagV2Schema.Schema];
}

public sealed class OtherV1Schema : ISchemaDefinition<OtherFamily, SemanticScheme>
{
    public static UriTypeSchema Schema => new("semantic://wakaze.dev/other/v1");

    public static IReadOnlyList<UriTypeSchema> CompatibleTargets => [];

    public static IReadOnlyList<UriTypeSchema> ProjectableTargets => [];
}

public sealed class PostgreSqlV1Schema : ISchemaDefinition<PostgreSqlFamily, DatabaseScheme>
{
    public static UriTypeSchema Schema => new("database://wakaze.dev/postgresql/v1");

    public static IReadOnlyList<UriTypeSchema> CompatibleTargets => [];

    public static IReadOnlyList<UriTypeSchema> ProjectableTargets => [];
}

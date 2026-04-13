namespace Kawayi.Wakaze.Analyzer.Tests;

public class RegisterSchemaMetadataConsistencyAnalyzerTests
{
    [Test]
    public async Task RegisterSchema_With_Consistent_Metadata_Does_Not_Report()
    {
        var source = """
                     using System.Collections.Generic;
                     using Kawayi.Wakaze.Abstractions;

                     namespace Demo;

                     public sealed class SemanticScheme : ISchemaUriSchemeDefinition
                     {
                         public static string UriScheme => "semantic";
                     }

                     public sealed class FooFamily : ISchemaFamilyDefinition<SemanticScheme>
                     {
                         public static SchemaFamily Family => new("semantic://wakaze.dev/foo");
                     }

                     [RegisterSchema]
                     public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
                     {
                         public static SchemaId Schema => new("semantic://wakaze.dev/foo/v1");
                         public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                         public static IReadOnlyList<SchemaId> ProjectableTargets => [];
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task RegisterSchema_With_Mismatched_Schema_Family_Reports_KWA0003()
    {
        var source = """
                     using System.Collections.Generic;
                     using Kawayi.Wakaze.Abstractions;

                     namespace Demo;

                     public sealed class SemanticScheme : ISchemaUriSchemeDefinition
                     {
                         public static string UriScheme => "semantic";
                     }

                     public sealed class FooFamily : ISchemaFamilyDefinition<SemanticScheme>
                     {
                         public static SchemaFamily Family => new("semantic://wakaze.dev/foo");
                     }

                     [RegisterSchema]
                     public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
                     {
                         public static SchemaId Schema => new("semantic://wakaze.dev/bar/v1");
                         public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                         public static IReadOnlyList<SchemaId> ProjectableTargets => [];
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(
            source,
            RegisterSchemaMetadataConsistencyAnalyzer.SchemaFamilyConsistencyRuleId);
    }

    [Test]
    public async Task RegisterSchema_With_Mismatched_Family_Scheme_Reports_KWA0004()
    {
        var source = """
                     using System.Collections.Generic;
                     using Kawayi.Wakaze.Abstractions;

                     namespace Demo;

                     public sealed class SemanticScheme : ISchemaUriSchemeDefinition
                     {
                         public static string UriScheme => "semantic";
                     }

                     public sealed class FooFamily : ISchemaFamilyDefinition<SemanticScheme>
                     {
                         public static SchemaFamily Family => new("database://wakaze.dev/foo");
                     }

                     [RegisterSchema]
                     public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
                     {
                         public static SchemaId Schema => new("database://wakaze.dev/foo/v1");
                         public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                         public static IReadOnlyList<SchemaId> ProjectableTargets => [];
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(
            source,
            RegisterSchemaMetadataConsistencyAnalyzer.FamilySchemeConsistencyRuleId);
    }

    [Test]
    public async Task RegisterSchema_With_Both_Mismatches_Reports_KWA0003_And_KWA0004()
    {
        var source = """
                     using System.Collections.Generic;
                     using Kawayi.Wakaze.Abstractions;

                     namespace Demo;

                     public sealed class SemanticScheme : ISchemaUriSchemeDefinition
                     {
                         public static string UriScheme => "semantic";
                     }

                     public sealed class FooFamily : ISchemaFamilyDefinition<SemanticScheme>
                     {
                         public static SchemaFamily Family => new("database://wakaze.dev/foo");
                     }

                     [RegisterSchema]
                     public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
                     {
                         public static SchemaId Schema => new("semantic://wakaze.dev/bar/v1");
                         public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                         public static IReadOnlyList<SchemaId> ProjectableTargets => [];
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(
            source,
            RegisterSchemaMetadataConsistencyAnalyzer.SchemaFamilyConsistencyRuleId,
            RegisterSchemaMetadataConsistencyAnalyzer.FamilySchemeConsistencyRuleId);
    }

    [Test]
    public async Task Unregistered_Schema_Does_Not_Report()
    {
        var source = """
                     using System.Collections.Generic;
                     using Kawayi.Wakaze.Abstractions;

                     namespace Demo;

                     public sealed class SemanticScheme : ISchemaUriSchemeDefinition
                     {
                         public static string UriScheme => "semantic";
                     }

                     public sealed class FooFamily : ISchemaFamilyDefinition<SemanticScheme>
                     {
                         public static SchemaFamily Family => new("database://wakaze.dev/foo");
                     }

                     public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
                     {
                         public static SchemaId Schema => new("semantic://wakaze.dev/bar/v1");
                         public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                         public static IReadOnlyList<SchemaId> ProjectableTargets => [];
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task RegisterSchema_Without_SchemaDefinition_Does_Not_Report_New_Rules()
    {
        var source = """
                     using System.Collections.Generic;
                     using Kawayi.Wakaze.Abstractions;

                     namespace Demo;

                     [RegisterSchema]
                     public sealed class BrokenSchema
                     {
                         public static SchemaId Schema => new("semantic://wakaze.dev/foo/v1");
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task NonConstant_Metadata_Does_Not_Report()
    {
        var source = """
                     using System.Collections.Generic;
                     using Kawayi.Wakaze.Abstractions;

                     namespace Demo;

                     public sealed class SemanticScheme : ISchemaUriSchemeDefinition
                     {
                         public static string UriScheme => GetScheme();

                         private static string GetScheme() => "semantic";
                     }

                     public sealed class FooFamily : ISchemaFamilyDefinition<SemanticScheme>
                     {
                         public static SchemaFamily Family => CreateFamily();

                         private static SchemaFamily CreateFamily() => new("database://wakaze.dev/foo");
                     }

                     [RegisterSchema]
                     public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
                     {
                         public static SchemaId Schema => CreateSchema();
                         public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                         public static IReadOnlyList<SchemaId> ProjectableTargets => [];

                         private static SchemaId CreateSchema() => new("semantic://wakaze.dev/bar/v1");
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(source);
    }

    [Test]
    public async Task TargetTyped_New_Metadata_Is_Analyzed()
    {
        var source = """
                     using System.Collections.Generic;
                     using Kawayi.Wakaze.Abstractions;

                     namespace Demo;

                     public sealed class SemanticScheme : ISchemaUriSchemeDefinition
                     {
                         public static string UriScheme => "semantic";
                     }

                     public sealed class FooFamily : ISchemaFamilyDefinition<SemanticScheme>
                     {
                         public static SchemaFamily Family => new("semantic://wakaze.dev/foo");
                     }

                     [RegisterSchema]
                     public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
                     {
                         public static SchemaId Schema => new("semantic://wakaze.dev/other/v1");
                         public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                         public static IReadOnlyList<SchemaId> ProjectableTargets => [];
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(
            source,
            RegisterSchemaMetadataConsistencyAnalyzer.SchemaFamilyConsistencyRuleId);
    }

    [Test]
    public async Task Invalid_String_Diagnostics_Can_Coexist_With_Metadata_Diagnostics()
    {
        var source = """
                     using System.Collections.Generic;
                     using Kawayi.Wakaze.Abstractions;

                     namespace Demo;

                     public sealed class SemanticScheme : ISchemaUriSchemeDefinition
                     {
                         public static string UriScheme => "semantic";
                     }

                     public sealed class FooFamily : ISchemaFamilyDefinition<SemanticScheme>
                     {
                         public static SchemaFamily Family => new("database://wakaze.dev/foo");
                     }

                     [RegisterSchema]
                     public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
                     {
                         public static SchemaId Schema => new("semantic://wakaze.dev/tag");
                         public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                         public static IReadOnlyList<SchemaId> ProjectableTargets => [];
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(
            source,
            SchemaStringConstructorAnalyzer.SchemaIdRuleId,
            RegisterSchemaMetadataConsistencyAnalyzer.FamilySchemeConsistencyRuleId);
    }

    [Test]
    public async Task EnableKWA0003_False_Suppresses_Only_KWA0003()
    {
        var source = """
                     using System.Collections.Generic;
                     using Kawayi.Wakaze.Abstractions;

                     namespace Demo;

                     public sealed class SemanticScheme : ISchemaUriSchemeDefinition
                     {
                         public static string UriScheme => "semantic";
                     }

                     public sealed class FooFamily : ISchemaFamilyDefinition<SemanticScheme>
                     {
                         public static SchemaFamily Family => new("database://wakaze.dev/foo");
                     }

                     [RegisterSchema]
                     public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
                     {
                         public static SchemaId Schema => new("semantic://wakaze.dev/bar/v1");
                         public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                         public static IReadOnlyList<SchemaId> ProjectableTargets => [];
                     }
                     """;

        await SchemaStringConstructorAnalyzerVerifier.VerifyAsync(
            source,
            new Dictionary<string, string>
            {
                ["EnableKWA0003"] = "false"
            },
            RegisterSchemaMetadataConsistencyAnalyzer.FamilySchemeConsistencyRuleId);
    }
}

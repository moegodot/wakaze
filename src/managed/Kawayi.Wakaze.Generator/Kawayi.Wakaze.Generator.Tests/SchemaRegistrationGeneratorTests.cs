namespace Kawayi.Wakaze.Generator.Tests;

public class SchemaRegistrationGeneratorTests
{
    [Test]
    public async Task RegisterSchema_Generates_ProjectLevel_Registrar()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
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
            """);

        await result.AssertNoCompilationErrorsAsync();
        await result.AssertDiagnosticIdsAsync();
        await result.AssertGeneratedSourceContainsAsync(
            "internal sealed partial class KawayiWakazeGeneratorTestsInputSchemaRegistration",
            "projector.RegisterSchema<global::Demo.FooV1Schema, global::Demo.FooFamily, global::Demo.SemanticScheme>();",
            "compatibility.Register<global::Demo.FooV1Schema, global::Demo.FooFamily, global::Demo.SemanticScheme>();");
    }

    [Test]
    public async Task MultipleSchemas_Still_Generate_One_Registrar()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
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

            public sealed class BarFamily : ISchemaFamilyDefinition<SemanticScheme>
            {
                public static SchemaFamily Family => new("semantic://wakaze.dev/bar");
            }

            [RegisterSchema]
            public sealed class FooV1Schema : ISchemaDefinition<FooFamily, SemanticScheme>
            {
                public static SchemaId Schema => new("semantic://wakaze.dev/foo/v1");
                public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                public static IReadOnlyList<SchemaId> ProjectableTargets => [];
            }

            [RegisterSchema]
            public sealed class BarV1Schema : ISchemaDefinition<BarFamily, SemanticScheme>
            {
                public static SchemaId Schema => new("semantic://wakaze.dev/bar/v1");
                public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                public static IReadOnlyList<SchemaId> ProjectableTargets => [];
            }
            """);

        await result.AssertNoCompilationErrorsAsync();
        await result.AssertDiagnosticIdsAsync();
        await Assert.That(result.GeneratedSources.Length).IsEqualTo(1);
    }

    [Test]
    public async Task ProjectTo_Method_Generates_Projector_Registration()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
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
            """);

        await result.AssertNoCompilationErrorsAsync();
        await result.AssertDiagnosticIdsAsync();
        await result.AssertGeneratedSourceContainsAsync(
            "private static global::Kawayi.Wakaze.Abstractions.ITypedObject Projector0",
            "global::Demo.FooV1Schema.ProjectToBar((global::Demo.FooValue)source);",
            "projector.Register<global::Demo.FooV1Schema, global::Demo.FooFamily, global::Demo.SemanticScheme, global::Demo.BarV1Schema, global::Demo.BarFamily, global::Demo.SemanticScheme>(Projector0);");
    }

    [Test]
    public async Task No_RegisterSchema_Does_Not_Generate_Registrar()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
            using Kawayi.Wakaze.Abstractions;

            namespace Demo;

            public sealed class SemanticScheme : ISchemaUriSchemeDefinition
            {
                public static string UriScheme => "semantic";
            }
            """);

        await result.AssertNoCompilationErrorsAsync();
        await result.AssertDiagnosticIdsAsync();
        await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Invalid_RegisterSchema_Type_Reports_WG0001()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
            using Kawayi.Wakaze.Abstractions;

            namespace Demo;

            [RegisterSchema]
            public sealed class BrokenSchema
            {
            }
            """);

        await result.AssertDiagnosticIdsAsync("WG0001");
    }

    [Test]
    public async Task ProjectTo_On_NonRegistered_Type_Reports_WG0002()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
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

            public sealed class BarFamily : ISchemaFamilyDefinition<SemanticScheme>
            {
                public static SchemaFamily Family => new("semantic://wakaze.dev/bar");
            }

            public sealed class BarV1Schema : ISchemaDefinition<BarFamily, SemanticScheme>
            {
                public static SchemaId Schema => new("semantic://wakaze.dev/bar/v1");
                public static IReadOnlyList<SchemaId> CompatibleTargets => [];
                public static IReadOnlyList<SchemaId> ProjectableTargets => [];
            }

            public sealed class Helper
            {
                [ProjectTo(typeof(BarV1Schema))]
                internal static FooValue Project(FooValue source) => source;
            }

            public sealed record FooValue(SchemaId SchemaId) : ITypedObject;
            """);

        await result.AssertDiagnosticIdsAsync("WG0002");
    }

    [Test]
    public async Task ProjectTo_With_Invalid_Target_Reports_WG0003()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
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

                [ProjectTo(typeof(string))]
                internal static FooValue Project(FooValue source) => source;
            }

            public sealed record FooValue(SchemaId SchemaId) : ITypedObject;
            """);

        await result.AssertDiagnosticIdsAsync("WG0003");
    }

    [Test]
    public async Task ProjectTo_Must_Be_Static_And_Have_One_Parameter()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
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

                [ProjectTo(typeof(FooV1Schema))]
                internal FooValue InstanceProject(FooValue source, FooValue other) => source;
            }

            public sealed record FooValue(SchemaId SchemaId) : ITypedObject;
            """);

        await result.AssertDiagnosticIdsAsync("WG0004");
    }

    [Test]
    public async Task ProjectTo_With_Two_Parameters_Reports_WG0005()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
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

                [ProjectTo(typeof(FooV1Schema))]
                internal static FooValue Project(FooValue source, FooValue other) => source;
            }

            public sealed record FooValue(SchemaId SchemaId) : ITypedObject;
            """);

        await result.AssertDiagnosticIdsAsync("WG0005");
    }

    [Test]
    public async Task ProjectTo_With_Void_Return_Reports_WG0006()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
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

                [ProjectTo(typeof(FooV1Schema))]
                internal static void Project(FooValue source)
                {
                }
            }

            public sealed record FooValue(SchemaId SchemaId) : ITypedObject;
            """);

        await result.AssertDiagnosticIdsAsync("WG0006");
    }

    [Test]
    public async Task ProjectTo_Parameter_And_Return_Must_Implement_ITypedObject()
    {
        var parameterResult = SchemaRegistrationGeneratorVerifier.Run("""
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

                [ProjectTo(typeof(FooV1Schema))]
                internal static FooValue Project(string source) => new FooValue(Schema);
            }

            public sealed record FooValue(SchemaId SchemaId) : ITypedObject;
            """);

        await parameterResult.AssertDiagnosticIdsAsync("WG0007");

        var returnResult = SchemaRegistrationGeneratorVerifier.Run("""
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

                [ProjectTo(typeof(FooV1Schema))]
                internal static string Project(FooValue source) => string.Empty;
            }

            public sealed record FooValue(SchemaId SchemaId) : ITypedObject;
            """);

        await returnResult.AssertDiagnosticIdsAsync("WG0008");
    }

    [Test]
    public async Task Private_ProjectTo_Method_Reports_WG0009()
    {
        var result = SchemaRegistrationGeneratorVerifier.Run("""
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

                [ProjectTo(typeof(FooV1Schema))]
                private static FooValue Project(FooValue source) => source;
            }

            public sealed record FooValue(SchemaId SchemaId) : ITypedObject;
            """);

        await result.AssertDiagnosticIdsAsync("WG0009");
    }
}

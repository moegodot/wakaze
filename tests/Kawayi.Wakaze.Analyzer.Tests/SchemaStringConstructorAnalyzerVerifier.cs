using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kawayi.Wakaze.Analyzer.Tests;

internal static class SchemaStringConstructorAnalyzerVerifier
{
    private const string AbstractionsStub = """
                                            using System;
                                            using System.Collections.Generic;

                                            namespace Kawayi.Wakaze.Abstractions;

                                            public readonly struct SchemaFamily
                                            {
                                                public SchemaFamily(string value)
                                                {
                                                }
                                            }

                                            public readonly struct SchemaId
                                            {
                                                public SchemaId(string value)
                                                {
                                                }
                                            }

                                            [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                                            public sealed class RegisterSchemaAttribute : Attribute
                                            {
                                            }

                                            public interface ISchemaDefinitionMarker
                                            {
                                                static abstract SchemaId Schema { get; }
                                            }

                                            public interface ISchemaUriSchemeDefinition
                                            {
                                                static abstract string UriScheme { get; }
                                            }

                                            public interface ISchemaFamilyDefinition<TScheme>
                                                where TScheme : ISchemaUriSchemeDefinition
                                            {
                                                static abstract SchemaFamily Family { get; }
                                            }

                                            public interface ISchemaDefinition<TFamily, TScheme> : ISchemaDefinitionMarker
                                                where TFamily : ISchemaFamilyDefinition<TScheme>
                                                where TScheme : ISchemaUriSchemeDefinition
                                            {
                                                static abstract IReadOnlyList<SchemaId> CompatibleTargets { get; }
                                                static abstract IReadOnlyList<SchemaId> ProjectableTargets { get; }
                                            }
                                            """;

    private const string EntryPointStub = """
                                          public static class AnalyzerTestEntryPoint
                                          {
                                              public static void Main()
                                              {
                                              }
                                          }
                                          """;

    public static async Task VerifyAsync(string source, params string[] expectedDiagnosticIds)
    {
        var compilation = CreateCompilation(source);
        var compilationDiagnostics = compilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (compilationDiagnostics.Length != 0)
            throw new Exception(
                "Expected analyzer test compilation to succeed, but got errors:" + Environment.NewLine +
                string.Join(Environment.NewLine,
                    compilationDiagnostics.Select(static diagnostic => diagnostic.ToString())));

        DiagnosticAnalyzer[] analyzers =
        [
            new SchemaStringConstructorAnalyzer(),
            new RegisterSchemaMetadataConsistencyAnalyzer()
        ];
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.CreateRange(analyzers))
            .GetAnalyzerDiagnosticsAsync();

        var actualDiagnosticIds = diagnostics
            .OrderBy(static diagnostic => diagnostic.Location.SourceSpan.Start)
            .Select(static diagnostic => diagnostic.Id)
            .ToArray();

        var expected = expectedDiagnosticIds.OrderBy(static id => id).ToArray();
        var actual = actualDiagnosticIds.OrderBy(static id => id).ToArray();

        if (!expected.SequenceEqual(actual))
            throw new Exception(
                $"Expected diagnostics [{string.Join(", ", expected)}] but got [{string.Join(", ", actual)}].");
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp13);
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(AbstractionsStub, parseOptions),
            CSharpSyntaxTree.ParseText(EntryPointStub, parseOptions),
            CSharpSyntaxTree.ParseText(source, parseOptions)
        };

        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                         ?.Split(Path.PathSeparator)
                         .Select(static path => MetadataReference.CreateFromFile(path))
                         .Cast<MetadataReference>()
                         .ToArray()
                         ?? throw new Exception("Trusted platform assemblies were not available.");

        return CSharpCompilation.Create(
            "SchemaStringConstructorAnalyzerTests",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}

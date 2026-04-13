using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kawayi.Wakaze.Analyzer.Tests;

internal static class SchemaStringConstructorAnalyzerVerifier
{
    private const string GlobalUsingsStub = """
                                            global using System;
                                            global using Kawayi.Wakaze.Abstractions;
                                            global using Kawayi.Wakaze.Abstractions.Schema;
                                            """;

    private const string AbstractionsStub = """
                                            using System;
                                            using System.Collections.Generic;

                                            namespace Kawayi.Wakaze.Abstractions
                                            {
                                                public interface ITypedObject
                                                {
                                                    Kawayi.Wakaze.Abstractions.Schema.SchemaId SchemaId { get; }
                                                }

                                                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
                                                public sealed class ProjectToAttribute(Type targetSchema) : Attribute
                                                {
                                                    public Type TargetSchema { get; } = targetSchema;
                                                }
                                            }

                                            namespace Kawayi.Wakaze.Abstractions.Schema
                                            {
                                                public readonly struct SchemaFamily
                                                {
                                                    public SchemaFamily(string value)
                                                    {
                                                    }

                                                    public static SchemaFamily Parse(string value, IFormatProvider? provider)
                                                    {
                                                        throw new NotSupportedException();
                                                    }

                                                    public static bool TryParse(string? value, IFormatProvider? provider, out SchemaFamily result)
                                                    {
                                                        result = default;
                                                        return false;
                                                    }
                                                }

                                                public readonly struct SchemaId
                                                {
                                                    public SchemaId(string value)
                                                    {
                                                    }

                                                    public static SchemaId Parse(string value, IFormatProvider? provider)
                                                    {
                                                        throw new NotSupportedException();
                                                    }

                                                    public static bool TryParse(string? value, IFormatProvider? provider, out SchemaId result)
                                                    {
                                                        result = default;
                                                        return false;
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

    public static Task VerifyAsync(string source, params string[] expectedDiagnosticIds)
    {
        return VerifyAsyncCore(source, includeXmlDocumentationAnalyzer: false, globalOptions: null, expectedDiagnosticIds);
    }

    public static Task VerifyAsync(
        string source,
        IReadOnlyDictionary<string, string> globalOptions,
        params string[] expectedDiagnosticIds)
    {
        return VerifyAsyncCore(source, includeXmlDocumentationAnalyzer: false, globalOptions, expectedDiagnosticIds);
    }

    public static Task VerifyXmlDocumentationAsync(string source, params string[] expectedDiagnosticIds)
    {
        return VerifyAsyncCore(source, includeXmlDocumentationAnalyzer: true, globalOptions: null, expectedDiagnosticIds);
    }

    public static Task VerifyXmlDocumentationAsync(
        string source,
        IReadOnlyDictionary<string, string> globalOptions,
        params string[] expectedDiagnosticIds)
    {
        return VerifyAsyncCore(source, includeXmlDocumentationAnalyzer: true, globalOptions, expectedDiagnosticIds);
    }

    private static async Task VerifyAsyncCore(
        string source,
        bool includeXmlDocumentationAnalyzer,
        IReadOnlyDictionary<string, string>? globalOptions,
        params string[] expectedDiagnosticIds)
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
        if (includeXmlDocumentationAnalyzer)
            analyzers = [.. analyzers, new XmlDocumentationCompletenessAnalyzer()];

        var analyzerOptions = new AnalyzerOptions(
            ImmutableArray<AdditionalText>.Empty,
            new TestAnalyzerConfigOptionsProvider(globalOptions));

        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.CreateRange(analyzers), analyzerOptions)
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
            CSharpSyntaxTree.ParseText(GlobalUsingsStub, parseOptions),
            CSharpSyntaxTree.ParseText(AbstractionsStub, parseOptions, path: "/tests/AnalyzerStubs/Abstractions.cs"),
            CSharpSyntaxTree.ParseText(EntryPointStub, parseOptions, path: "/tests/AnalyzerStubs/EntryPoint.cs"),
            CSharpSyntaxTree.ParseText(source, parseOptions, path: "/src/managed/AnalyzerTests/TestInput.cs")
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

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private static readonly TestAnalyzerConfigOptions EmptyOptions =
            new(ImmutableDictionary<string, string>.Empty);
        private readonly TestAnalyzerConfigOptions _globalOptions;

        public TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string>? globalOptions)
        {
            if (globalOptions is null || globalOptions.Count == 0)
            {
                _globalOptions = EmptyOptions;
                return;
            }

            var normalized = globalOptions.ToImmutableDictionary(
                static pair => "build_property." + pair.Key,
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);

            _globalOptions = new TestAnalyzerConfigOptions(normalized);
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return EmptyOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return EmptyOptions;
        }
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
        {
            _values = values;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _values.TryGetValue(key, out value!);
        }
    }
}

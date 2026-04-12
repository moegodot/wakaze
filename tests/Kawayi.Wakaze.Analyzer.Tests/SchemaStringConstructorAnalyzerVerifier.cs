using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kawayi.Wakaze.Analyzer.Tests;

internal static class SchemaStringConstructorAnalyzerVerifier
{
    private const string AbstractionsStub = """
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
        """;

    public static async Task VerifyAsync(string source, params string[] expectedDiagnosticIds)
    {
        var compilation = CreateCompilation(source);
        var compilationDiagnostics = compilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (compilationDiagnostics.Length != 0)
        {
            throw new Exception(
                "Expected analyzer test compilation to succeed, but got errors:" + Environment.NewLine +
                string.Join(Environment.NewLine, compilationDiagnostics.Select(static diagnostic => diagnostic.ToString())));
        }

        var analyzer = new SchemaStringConstructorAnalyzer();
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync();

        var actualDiagnosticIds = diagnostics
            .OrderBy(static diagnostic => diagnostic.Location.SourceSpan.Start)
            .Select(static diagnostic => diagnostic.Id)
            .ToArray();

        var expected = expectedDiagnosticIds.OrderBy(static id => id).ToArray();
        var actual = actualDiagnosticIds.OrderBy(static id => id).ToArray();

        if (!expected.SequenceEqual(actual))
        {
            throw new Exception(
                $"Expected diagnostics [{string.Join(", ", expected)}] but got [{string.Join(", ", actual)}].");
        }
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp13);
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(AbstractionsStub, parseOptions),
            CSharpSyntaxTree.ParseText(source, parseOptions)
        };

        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .Cast<MetadataReference>()
            .ToArray()
            ?? throw new Exception("Trusted platform assemblies were not available.");

        return CSharpCompilation.Create(
            assemblyName: "SchemaStringConstructorAnalyzerTests",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}

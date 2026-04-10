using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Kawayi.Wakaze.Generator.Tests;

internal static class SchemaRegistrationGeneratorVerifier
{
    private const string CommonUsings = """
        using System;
        using System.Collections.Generic;
        """;

    public static VerificationResult Run(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp13);
        var compilation = CreateCompilation(CommonUsings + Environment.NewLine + source, parseOptions);
        var generator = new SchemaRegistrationGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var driverDiagnostics);
        var runResult = driver.GetRunResult();
        var diagnostics = runResult.Results.Single().Diagnostics
            .OrderBy(static diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        var compilationErrors = outputCompilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .OrderBy(static diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        var generatedSources = runResult.Results.Single().GeneratedSources
            .Select(static sourceResult => sourceResult.SourceText.ToString())
            .ToImmutableArray();

        return new VerificationResult(diagnostics, compilationErrors, generatedSources, driverDiagnostics);
    }

    private static CSharpCompilation CreateCompilation(string source, CSharpParseOptions parseOptions)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = GetMetadataReferences();

        return CSharpCompilation.Create(
            assemblyName: "Kawayi.Wakaze.Generator.Tests.Input",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static ImmutableArray<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToList()
            ?? throw new InvalidOperationException("Trusted platform assemblies were not available.");

        trustedPlatformAssemblies.Add(MetadataReference.CreateFromFile(typeof(Kawayi.Wakaze.Abstractions.RegisterSchemaAttribute).Assembly.Location));
        return trustedPlatformAssemblies.Cast<MetadataReference>().ToImmutableArray();
    }
}

internal sealed record VerificationResult(
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<Diagnostic> CompilationErrors,
    ImmutableArray<string> GeneratedSources,
    ImmutableArray<Diagnostic> DriverDiagnostics)
{
    public async Task AssertNoCompilationErrorsAsync()
    {
        await Assert.That(CompilationErrors.Length).IsEqualTo(0);
        await Assert.That(DriverDiagnostics.Length).IsEqualTo(0);
    }

    public async Task AssertDiagnosticIdsAsync(params string[] expectedIds)
    {
        var actualIds = Diagnostics.Select(static diagnostic => diagnostic.Id).ToArray();
        Array.Sort(actualIds, StringComparer.Ordinal);

        var orderedExpectedIds = expectedIds.ToArray();
        Array.Sort(orderedExpectedIds, StringComparer.Ordinal);

        await Assert.That(actualIds).IsEquivalentTo(orderedExpectedIds);
    }

    public async Task AssertGeneratedSourceContainsAsync(params string[] fragments)
    {
        var combined = string.Join(Environment.NewLine, GeneratedSources);

        foreach (var fragment in fragments)
        {
            await Assert.That(combined.Contains(fragment, StringComparison.Ordinal)).IsTrue();
        }
    }
}

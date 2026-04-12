using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Kawayi.Wakaze.Analyzer;

/// <summary>
/// Reports invalid constant string values passed to schema identifier constructors.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SchemaStringConstructorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Identifies invalid constant strings passed to <c>SchemaFamily(string)</c>.
    /// </summary>
    public const string SchemaFamilyRuleId = "AB0001";

    /// <summary>
    /// Identifies invalid constant strings passed to <c>SchemaId(string)</c>.
    /// </summary>
    public const string SchemaIdRuleId = "AB0002";

    private const string Category = "Usage";
    private const string SchemaFamilyMetadataName = "Kawayi.Wakaze.Abstractions.SchemaFamily";
    private const string SchemaIdMetadataName = "Kawayi.Wakaze.Abstractions.SchemaId";
    private const string StringMetadataName = "System.String";

    internal static readonly DiagnosticDescriptor InvalidSchemaFamilyString = new(
        id: SchemaFamilyRuleId,
        title: "Invalid SchemaFamily constructor string",
        messageFormat: "The value is not a valid SchemaFamily identifier",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor InvalidSchemaIdString = new(
        id: SchemaIdRuleId,
        title: "Invalid SchemaId constructor string",
        messageFormat: "The value is not a valid SchemaId identifier",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [InvalidSchemaFamilyString, InvalidSchemaIdString];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var schemaFamilyType = compilationContext.Compilation.GetTypeByMetadataName(SchemaFamilyMetadataName);
            var schemaIdType = compilationContext.Compilation.GetTypeByMetadataName(SchemaIdMetadataName);

            if (schemaFamilyType is null && schemaIdType is null)
            {
                return;
            }

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeObjectCreation(operationContext, schemaFamilyType, schemaIdType),
                OperationKind.ObjectCreation);
        });
    }

    private static void AnalyzeObjectCreation(
        OperationAnalysisContext context,
        INamedTypeSymbol? schemaFamilyType,
        INamedTypeSymbol? schemaIdType)
    {
        var operation = (IObjectCreationOperation)context.Operation;
        var constructor = operation.Constructor;
        if (constructor is null || constructor.Parameters.Length != 1)
        {
            return;
        }

        if (constructor.Parameters[0].Type?.SpecialType != SpecialType.System_String)
        {
            return;
        }

        if (operation.Arguments.Length != 1)
        {
            return;
        }

        var targetType = constructor.ContainingType;
        var argument = operation.Arguments[0].Value;

        if (!TryGetConstantString(argument, out var value))
        {
            return;
        }

        if (schemaFamilyType is not null &&
            SymbolEqualityComparer.Default.Equals(targetType, schemaFamilyType) &&
            !SchemaStringValidator.IsValidSchemaFamily(value))
        {
            context.ReportDiagnostic(Diagnostic.Create(InvalidSchemaFamilyString, argument.Syntax.GetLocation()));
            return;
        }

        if (schemaIdType is not null &&
            SymbolEqualityComparer.Default.Equals(targetType, schemaIdType) &&
            !SchemaStringValidator.IsValidSchemaId(value))
        {
            context.ReportDiagnostic(Diagnostic.Create(InvalidSchemaIdString, argument.Syntax.GetLocation()));
        }
    }

    private static bool TryGetConstantString(IOperation operation, out string? value)
    {
        value = null;

        if (!operation.ConstantValue.HasValue)
        {
            return false;
        }

        if (operation.Type?.SpecialType != SpecialType.System_String &&
            operation.Type?.SpecialType != SpecialType.System_Object)
        {
            return false;
        }

        value = operation.ConstantValue.Value as string;
        return true;
    }
}

internal static class SchemaStringValidator
{
    public static bool IsValidSchemaFamily(string? value)
    {
        if (value is null)
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return IsValidSchemaFamily(uri);
    }

    public static bool IsValidSchemaId(string? value)
    {
        if (value is null)
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!IsValidSchemaFamily(uri))
        {
            return false;
        }

        var pathSegments = GetPathSegments(uri.AbsolutePath);
        if (pathSegments.Length < 2)
        {
            return false;
        }

        return IsValidVersionSegment(pathSegments[pathSegments.Length - 1]);
    }

    private static bool IsValidSchemaFamily(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        if (!uri.IsDefaultPort)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var absolutePath = uri.AbsolutePath;
        if (string.IsNullOrEmpty(absolutePath) || absolutePath == "/")
        {
            return false;
        }

        if (absolutePath[absolutePath.Length - 1] == '/')
        {
            return false;
        }

        return GetPathSegments(absolutePath).Length > 0;
    }

    private static string[] GetPathSegments(string absolutePath)
    {
        return absolutePath
            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(static segment => segment.Trim())
            .Where(static segment => segment.Length > 0)
            .Select(Uri.UnescapeDataString)
            .ToArray();
    }

    private static bool IsValidVersionSegment(string value)
    {
        if (value.Length < 2 || value[0] != 'v')
        {
            return false;
        }

        var digits = value.Substring(1);
        if (digits.Length == 0)
        {
            return false;
        }

        foreach (var digit in digits)
        {
            if (digit < '0' || digit > '9')
            {
                return false;
            }
        }

        if (digits.Length > 1 && digits[0] == '0')
        {
            return false;
        }

        uint parsed;
        return uint.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) && parsed != 0;
    }
}

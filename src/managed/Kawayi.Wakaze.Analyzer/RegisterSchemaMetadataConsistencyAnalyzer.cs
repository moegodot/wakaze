using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kawayi.Wakaze.Analyzer;

/// <summary>
/// Reports inconsistent schema metadata on registered schema definition types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RegisterSchemaMetadataConsistencyAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Identifies registered schemas whose <c>Schema</c> family does not match the declared schema family type.
    /// </summary>
    public const string SchemaFamilyConsistencyRuleId = "KWA0003";

    /// <summary>
    /// Identifies registered schemas whose declared family does not match the declared URI scheme type.
    /// </summary>
    public const string FamilySchemeConsistencyRuleId = "KWA0004";

    private const string Category = "Usage";
    private const string RegisterSchemaAttributeMetadataName = "Kawayi.Wakaze.Abstractions.RegisterSchemaAttribute";
    private const string SchemaDefinitionMetadataName = "Kawayi.Wakaze.Abstractions.ISchemaDefinition`2";
    private const string SchemaIdMetadataName = "Kawayi.Wakaze.Abstractions.SchemaId";
    private const string SchemaFamilyMetadataName = "Kawayi.Wakaze.Abstractions.SchemaFamily";

    internal static readonly DiagnosticDescriptor SchemaFamilyMismatch = new(
        SchemaFamilyConsistencyRuleId,
        "Registered schema must match declared family",
        "Schema '{0}' resolves to family '{1}', but the declared family is '{2}'",
        Category,
        DiagnosticSeverity.Warning,
        true);

    internal static readonly DiagnosticDescriptor FamilySchemeMismatch = new(
        FamilySchemeConsistencyRuleId,
        "Registered schema family must match declared scheme",
        "Family '{0}' resolves to scheme '{1}', but the declared scheme is '{2}'",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [SchemaFamilyMismatch, FamilySchemeMismatch];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var registerSchemaAttribute =
                compilationContext.Compilation.GetTypeByMetadataName(RegisterSchemaAttributeMetadataName);
            var schemaDefinitionInterface =
                compilationContext.Compilation.GetTypeByMetadataName(SchemaDefinitionMetadataName);

            if (registerSchemaAttribute is null ||
                schemaDefinitionInterface is null)
                return;

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(
                    symbolContext,
                    registerSchemaAttribute,
                    schemaDefinitionInterface),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        INamedTypeSymbol registerSchemaAttribute,
        INamedTypeSymbol schemaDefinitionInterface)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class) return;

        if (!HasAttribute(type, registerSchemaAttribute)) return;

        if (!TryGetSchemaDefinitionImplementation(type, schemaDefinitionInterface,
                out var schemaDefinitionImplementation)) return;

        var familyType = (INamedTypeSymbol)schemaDefinitionImplementation.TypeArguments[0];
        var schemeType = (INamedTypeSymbol)schemaDefinitionImplementation.TypeArguments[1];

        if (TryEvaluateSchemaFamily(type, out var schemaIdText, out var schemaFamilyText, out var schemaLocation) &&
            TryEvaluateFamily(familyType, out var declaredFamilyText, out _) &&
            !StringComparer.Ordinal.Equals(schemaFamilyText, declaredFamilyText))
            context.ReportDiagnostic(Diagnostic.Create(
                SchemaFamilyMismatch,
                schemaLocation,
                schemaIdText,
                schemaFamilyText,
                declaredFamilyText));

        if (TryEvaluateFamily(familyType, out var actualFamilyText, out var familyLocation) &&
            TryEvaluateUriScheme(schemeType, out var declaredScheme, out _) &&
            SchemaStringValidator.TryGetSchemaFamilyScheme(actualFamilyText, out var actualScheme) &&
            !StringComparer.OrdinalIgnoreCase.Equals(actualScheme, declaredScheme))
            context.ReportDiagnostic(Diagnostic.Create(
                FamilySchemeMismatch,
                familyLocation,
                actualFamilyText,
                actualScheme,
                declaredScheme));
    }

    private static bool HasAttribute(INamedTypeSymbol type, INamedTypeSymbol attributeType)
    {
        foreach (var attribute in type.GetAttributes())
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                return true;

        return false;
    }

    private static bool TryGetSchemaDefinitionImplementation(
        INamedTypeSymbol type,
        INamedTypeSymbol schemaDefinitionInterface,
        out INamedTypeSymbol implementation)
    {
        implementation = null!;

        foreach (var candidate in type.AllInterfaces)
            if (candidate.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, schemaDefinitionInterface))
            {
                implementation = candidate;
                return true;
            }

        return false;
    }

    private static bool TryEvaluateSchemaFamily(
        INamedTypeSymbol type,
        out string schemaIdText,
        out string familyText,
        out Location location)
    {
        schemaIdText = string.Empty;
        familyText = string.Empty;
        location = type.Locations.FirstOrDefault() ?? Location.None;

        if (!TryGetPropertyExpression(type, "Schema", out var expression, out location)) return false;

        if (!TryEvaluateSingleStringConstructorArgument(expression, SchemaIdMetadataName, out schemaIdText) ||
            !SchemaStringValidator.TryGetSchemaIdFamily(schemaIdText, out familyText))
            return false;

        return true;
    }

    private static bool TryEvaluateFamily(
        INamedTypeSymbol type,
        out string familyText,
        out Location location)
    {
        familyText = string.Empty;
        location = type.Locations.FirstOrDefault() ?? Location.None;

        if (!TryGetPropertyExpression(type, "Family", out var expression, out location)) return false;

        if (!TryEvaluateSingleStringConstructorArgument(expression, SchemaFamilyMetadataName, out familyText) ||
            !SchemaStringValidator.TryNormalizeSchemaFamily(familyText, out familyText))
            return false;

        return true;
    }

    private static bool TryEvaluateUriScheme(
        INamedTypeSymbol type,
        out string scheme,
        out Location location)
    {
        scheme = string.Empty;
        location = type.Locations.FirstOrDefault() ?? Location.None;

        if (!TryGetPropertyExpression(type, "UriScheme", out var expression, out location)) return false;

        if (!TryGetLiteralString(expression, out scheme)) return false;

        return true;
    }

    private static bool TryGetPropertyExpression(
        INamedTypeSymbol type,
        string propertyName,
        out ExpressionSyntax expression,
        out Location location)
    {
        foreach (var property in type.GetMembers(propertyName).OfType<IPropertySymbol>()
                     .Where(static property => property.IsStatic))
        foreach (var syntaxReference in property.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is not PropertyDeclarationSyntax syntax) continue;

            if (TryGetPropertyExpression(syntax, out expression))
            {
                location = expression.GetLocation();
                return true;
            }
        }

        expression = null!;
        location = type.Locations.FirstOrDefault() ?? Location.None;
        return false;
    }

    private static bool TryGetPropertyExpression(PropertyDeclarationSyntax syntax, out ExpressionSyntax expression)
    {
        if (syntax.ExpressionBody is not null)
        {
            expression = syntax.ExpressionBody.Expression;
            return true;
        }

        var getter = syntax.AccessorList?.Accessors
            .FirstOrDefault(static accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));

        if (getter?.ExpressionBody is not null)
        {
            expression = getter.ExpressionBody.Expression;
            return true;
        }

        if (getter?.Body?.Statements.Count == 1 &&
            getter.Body.Statements[0] is ReturnStatementSyntax returnStatement &&
            returnStatement.Expression is not null)
        {
            expression = returnStatement.Expression;
            return true;
        }

        expression = null!;
        return false;
    }

    private static bool TryEvaluateSingleStringConstructorArgument(
        ExpressionSyntax expression,
        string expectedMetadataName,
        out string value)
    {
        value = string.Empty;

        return expression switch
        {
            ObjectCreationExpressionSyntax creation
                when IsExpectedType(creation.Type, expectedMetadataName)
                => TryGetSingleLiteralStringArgument(creation.ArgumentList, out value),
            ImplicitObjectCreationExpressionSyntax creation
                => TryGetSingleLiteralStringArgument(creation.ArgumentList, out value),
            _ => false
        };
    }

    private static bool IsExpectedType(TypeSyntax type, string expectedMetadataName)
    {
        var simpleName = expectedMetadataName.Substring(expectedMetadataName.LastIndexOf('.') + 1);
        var text = type.ToString();
        return text == simpleName ||
               text == $"global::{expectedMetadataName}" ||
               text.EndsWith("." + simpleName, StringComparison.Ordinal);
    }

    private static bool TryGetSingleLiteralStringArgument(BaseArgumentListSyntax? argumentList, out string value)
    {
        value = string.Empty;

        if (argumentList?.Arguments.Count != 1) return false;

        return TryGetLiteralString(argumentList.Arguments[0].Expression, out value);
    }

    private static bool TryGetLiteralString(ExpressionSyntax expression, out string value)
    {
        value = string.Empty;

        if (expression is not LiteralExpressionSyntax literal ||
            !literal.IsKind(SyntaxKind.StringLiteralExpression))
            return false;

        value = literal.Token.ValueText;
        return true;
    }
}

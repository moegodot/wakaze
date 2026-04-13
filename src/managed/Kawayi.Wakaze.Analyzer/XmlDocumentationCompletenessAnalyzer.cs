using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Kawayi.Wakaze.Analyzer;

/// <summary>
/// Reports missing or incomplete XML documentation on externally visible public API symbols.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XmlDocumentationCompletenessAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Identifies public symbols that are missing a documentation entry such as <c>&lt;summary&gt;</c> or <c>&lt;inheritdoc/&gt;</c>.
    /// </summary>
    public const string MissingDocumentationRuleId = "KWA0005";

    /// <summary>
    /// Identifies public symbols whose XML documentation omits one or more required <c>&lt;param&gt;</c> tags.
    /// </summary>
    public const string MissingParamRuleId = "KWA0006";

    /// <summary>
    /// Identifies public symbols whose XML documentation omits one or more required <c>&lt;typeparam&gt;</c> tags.
    /// </summary>
    public const string MissingTypeParamRuleId = "KWA0007";

    /// <summary>
    /// Identifies public symbols whose XML documentation omits a required <c>&lt;returns&gt;</c> tag.
    /// </summary>
    public const string MissingReturnsRuleId = "KWA0008";

    /// <summary>
    /// Identifies public properties or indexers whose XML documentation omits a required <c>&lt;value&gt;</c> tag.
    /// </summary>
    public const string MissingValueRuleId = "KWA0009";

    /// <summary>
    /// Identifies public members whose bodies explicitly throw an exception but do not document it with <c>&lt;exception&gt;</c>.
    /// </summary>
    public const string MissingExceptionRuleId = "KWA0010";

    private const string Category = "Documentation";

    internal static readonly DiagnosticDescriptor MissingDocumentation = new(
        MissingDocumentationRuleId,
        "Public API symbol must declare XML documentation",
        "Public symbol '{0}' is missing XML documentation",
        Category,
        DiagnosticSeverity.Warning,
        true);

    internal static readonly DiagnosticDescriptor MissingParam = new(
        MissingParamRuleId,
        "XML documentation must cover all parameters",
        "Public symbol '{0}' is missing XML documentation for parameter '{1}'",
        Category,
        DiagnosticSeverity.Warning,
        true);

    internal static readonly DiagnosticDescriptor MissingTypeParam = new(
        MissingTypeParamRuleId,
        "XML documentation must cover all type parameters",
        "Public symbol '{0}' is missing XML documentation for type parameter '{1}'",
        Category,
        DiagnosticSeverity.Warning,
        true);

    internal static readonly DiagnosticDescriptor MissingReturns = new(
        MissingReturnsRuleId,
        "XML documentation must describe the return value",
        "Public symbol '{0}' is missing a <returns> XML documentation tag",
        Category,
        DiagnosticSeverity.Warning,
        true);

    internal static readonly DiagnosticDescriptor MissingValue = new(
        MissingValueRuleId,
        "XML documentation must describe the property value",
        "Public symbol '{0}' is missing a <value> XML documentation tag",
        Category,
        DiagnosticSeverity.Warning,
        true);

    internal static readonly DiagnosticDescriptor MissingException = new(
        MissingExceptionRuleId,
        "XML documentation must describe explicitly thrown exceptions",
        "Public symbol '{0}' is missing an <exception> XML documentation tag for '{1}'",
        Category,
        DiagnosticSeverity.Warning,
        true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        MissingDocumentation,
        MissingParam,
        MissingTypeParam,
        MissingReturns,
        MissingValue,
        MissingException
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var options = KwaAnalyzerOptions.Create(compilationContext.Options.AnalyzerConfigOptionsProvider);

            compilationContext.RegisterSymbolAction(symbolContext => AnalyzeNamedType(symbolContext, options), SymbolKind.NamedType);
            compilationContext.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, options), SymbolKind.Method);
            compilationContext.RegisterSymbolAction(symbolContext => AnalyzeProperty(symbolContext, options), SymbolKind.Property);
            compilationContext.RegisterSymbolAction(symbolContext => AnalyzeEvent(symbolContext, options), SymbolKind.Event);
            compilationContext.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, options), SymbolKind.Field);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, KwaAnalyzerOptions options)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (!ShouldAnalyze(symbol)) return;

        var documentation = DocumentationInfo.Create(symbol, context.Compilation, context.CancellationToken);
        ReportEntryDiagnostics(context, options, symbol, documentation);

        if (documentation.HasInheritdoc) return;

        foreach (var typeParameter in symbol.TypeParameters)
            ReportIfMissing(
                context,
                options,
                symbol,
                documentation.DocumentedTypeParameters.Contains(typeParameter.Name),
                MissingTypeParam,
                typeParameter.Name);

        if (symbol.TypeKind == TypeKind.Delegate &&
            symbol.DelegateInvokeMethod is { } invokeMethod)
        {
            foreach (var parameter in invokeMethod.Parameters)
                ReportIfMissing(
                    context,
                    options,
                    symbol,
                    documentation.DocumentedParameters.Contains(parameter.Name),
                    MissingParam,
                    parameter.Name);

            if (!invokeMethod.ReturnsVoid)
                ReportIfMissing(context, options, symbol, documentation.HasReturns, MissingReturns);
        }

        foreach (var parameterName in GetPrimaryConstructorParameterNames(symbol, context.CancellationToken))
            ReportIfMissing(
                context,
                options,
                symbol,
                documentation.DocumentedParameters.Contains(parameterName),
                MissingParam,
                parameterName);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, KwaAnalyzerOptions options)
    {
        var symbol = (IMethodSymbol)context.Symbol;
        if (!ShouldAnalyze(symbol) || !ShouldAnalyzeMethodKind(symbol)) return;

        var documentation = DocumentationInfo.Create(symbol, context.Compilation, context.CancellationToken);
        ReportEntryDiagnostics(context, options, symbol, documentation);

        if (documentation.HasInheritdoc) return;

        foreach (var parameter in symbol.Parameters)
            ReportIfMissing(
                context,
                options,
                symbol,
                documentation.DocumentedParameters.Contains(parameter.Name),
                MissingParam,
                parameter.Name);

        foreach (var typeParameter in symbol.TypeParameters)
            ReportIfMissing(
                context,
                options,
                symbol,
                documentation.DocumentedTypeParameters.Contains(typeParameter.Name),
                MissingTypeParam,
                typeParameter.Name);

        if (!symbol.ReturnsVoid)
            ReportIfMissing(context, options, symbol, documentation.HasReturns, MissingReturns);

        ReportExceptionDiagnostics(context, options, symbol, documentation);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, KwaAnalyzerOptions options)
    {
        var symbol = (IPropertySymbol)context.Symbol;
        if (!ShouldAnalyze(symbol) || symbol.IsImplicitlyDeclared) return;

        var documentation = DocumentationInfo.Create(symbol, context.Compilation, context.CancellationToken);
        ReportEntryDiagnostics(context, options, symbol, documentation);

        if (documentation.HasInheritdoc) return;

        foreach (var parameter in symbol.Parameters)
            ReportIfMissing(
                context,
                options,
                symbol,
                documentation.DocumentedParameters.Contains(parameter.Name),
                MissingParam,
                parameter.Name);

        ReportIfMissing(context, options, symbol, documentation.HasValue, MissingValue);
        ReportExceptionDiagnostics(context, options, symbol, documentation);
    }

    private static void AnalyzeEvent(SymbolAnalysisContext context, KwaAnalyzerOptions options)
    {
        var symbol = (IEventSymbol)context.Symbol;
        if (!ShouldAnalyze(symbol) || symbol.IsImplicitlyDeclared) return;

        var documentation = DocumentationInfo.Create(symbol, context.Compilation, context.CancellationToken);
        ReportEntryDiagnostics(context, options, symbol, documentation);

        if (documentation.HasInheritdoc) return;

        ReportExceptionDiagnostics(context, options, symbol, documentation);
    }

    private static void AnalyzeField(SymbolAnalysisContext context, KwaAnalyzerOptions options)
    {
        var symbol = (IFieldSymbol)context.Symbol;
        if (!ShouldAnalyze(symbol) ||
            symbol.IsImplicitlyDeclared ||
            symbol.AssociatedSymbol is not null)
            return;

        var documentation = DocumentationInfo.Create(symbol, context.Compilation, context.CancellationToken);
        ReportEntryDiagnostics(context, options, symbol, documentation);
    }

    private static bool ShouldAnalyze(ISymbol symbol)
    {
        if (symbol.Locations.All(static location => !location.IsInSource)) return false;
        if (!symbol.Locations.Any(IsTrackedSourceLocation)) return false;
        if (symbol.DeclaredAccessibility != Accessibility.Public) return false;

        for (var containingType = symbol.ContainingType;
             containingType is not null;
             containingType = containingType.ContainingType)
            if (containingType.DeclaredAccessibility != Accessibility.Public)
                return false;

        return true;
    }

    private static bool IsTrackedSourceLocation(Location location)
    {
        if (!location.IsInSource) return false;

        var filePath = location.SourceTree?.FilePath ?? location.GetLineSpan().Path;
        if (string.IsNullOrEmpty(filePath)) return false;

        return filePath.Contains("/src/managed/", StringComparison.OrdinalIgnoreCase) ||
               filePath.Contains("\\src\\managed\\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldAnalyzeMethodKind(IMethodSymbol symbol)
    {
        return symbol.MethodKind is MethodKind.Ordinary
            or MethodKind.Constructor
            or MethodKind.StaticConstructor
            or MethodKind.UserDefinedOperator
            or MethodKind.Conversion
            or MethodKind.DelegateInvoke;
    }

    private static void ReportEntryDiagnostics(
        SymbolAnalysisContext context,
        KwaAnalyzerOptions options,
        ISymbol symbol,
        DocumentationInfo documentation)
    {
        ReportIfMissing(
            context,
            options,
            symbol,
            documentation.HasSummary || documentation.HasInheritdoc,
            MissingDocumentation);
    }

    private static void ReportExceptionDiagnostics(
        SymbolAnalysisContext context,
        KwaAnalyzerOptions options,
        ISymbol symbol,
        DocumentationInfo documentation)
    {
        foreach (var exceptionType in documentation.GetExplicitlyThrownExceptions(symbol, context.Compilation,
                     context.CancellationToken))
        {
            if (documentation.DocumentedExceptions.Contains(exceptionType)) continue;
            if (!options.IsEnabled(MissingExceptionRuleId)) continue;

            context.ReportDiagnostic(Diagnostic.Create(
                MissingException,
                GetPrimaryLocation(symbol, context.CancellationToken),
                GetDisplayName(symbol),
                exceptionType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static void ReportIfMissing(
        SymbolAnalysisContext context,
        KwaAnalyzerOptions options,
        ISymbol symbol,
        bool satisfied,
        DiagnosticDescriptor descriptor,
        params object[] additionalArguments)
    {
        if (satisfied || !options.IsEnabled(descriptor.Id)) return;

        var arguments = new object[additionalArguments.Length + 1];
        arguments[0] = GetDisplayName(symbol);
        additionalArguments.CopyTo(arguments, 1);

        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            GetPrimaryLocation(symbol, context.CancellationToken),
            arguments));
    }

    private static string GetDisplayName(ISymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }

    private static Location GetPrimaryLocation(ISymbol symbol, CancellationToken cancellationToken)
    {
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax(cancellationToken);
            if (DocumentationInfo.IsGenerated(syntax.SyntaxTree, cancellationToken)) continue;
            return syntax.GetLocation();
        }

        return symbol.Locations.FirstOrDefault(static location => location.IsInSource) ?? Location.None;
    }

    private static IEnumerable<string> GetPrimaryConstructorParameterNames(
        INamedTypeSymbol symbol,
        CancellationToken cancellationToken)
    {
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is not RecordDeclarationSyntax recordDeclaration ||
                recordDeclaration.ParameterList is null ||
                DocumentationInfo.IsGenerated(recordDeclaration.SyntaxTree, cancellationToken))
                continue;

            foreach (var parameter in recordDeclaration.ParameterList.Parameters)
                yield return parameter.Identifier.ValueText;
        }
    }

    private sealed class DocumentationInfo
    {
        private DocumentationInfo(
            bool hasSummary,
            bool hasInheritdoc,
            bool hasReturns,
            bool hasValue,
            ImmutableHashSet<string> documentedParameters,
            ImmutableHashSet<string> documentedTypeParameters,
            ImmutableHashSet<INamedTypeSymbol> documentedExceptions)
        {
            HasSummary = hasSummary;
            HasInheritdoc = hasInheritdoc;
            HasReturns = hasReturns;
            HasValue = hasValue;
            DocumentedParameters = documentedParameters;
            DocumentedTypeParameters = documentedTypeParameters;
            DocumentedExceptions = documentedExceptions;
        }

        public bool HasSummary { get; }

        public bool HasInheritdoc { get; }

        public bool HasReturns { get; }

        public bool HasValue { get; }

        public ImmutableHashSet<string> DocumentedParameters { get; }

        public ImmutableHashSet<string> DocumentedTypeParameters { get; }

        public ImmutableHashSet<INamedTypeSymbol> DocumentedExceptions { get; }

        public static DocumentationInfo Create(
            ISymbol symbol,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            var xml = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(xml))
                return Empty;

            XElement root;
            try
            {
                root = XElement.Parse("<root>" + xml + "</root>");
            }
            catch
            {
                return Empty;
            }

            var documentedExceptions = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var exceptionElement in root.Descendants("exception"))
            {
                var cref = exceptionElement.Attribute("cref")?.Value;
                if (string.IsNullOrWhiteSpace(cref)) continue;

                foreach (var referencedSymbol in DocumentationCommentId.GetSymbolsForReferenceId(cref!, compilation))
                    if (referencedSymbol is INamedTypeSymbol namedType)
                        documentedExceptions.Add(namedType);
            }

            return new DocumentationInfo(
                root.Descendants("summary").Any(),
                root.Descendants("inheritdoc").Any(),
                root.Descendants("returns").Any(),
                root.Descendants("value").Any(),
                root.Descendants("param")
                    .Select(static element => element.Attribute("name")?.Value)
                    .Where(static name => !string.IsNullOrWhiteSpace(name))
                    .Cast<string>()
                    .ToImmutableHashSet(StringComparer.Ordinal),
                root.Descendants("typeparam")
                    .Select(static element => element.Attribute("name")?.Value)
                    .Where(static name => !string.IsNullOrWhiteSpace(name))
                    .Cast<string>()
                    .ToImmutableHashSet(StringComparer.Ordinal),
                documentedExceptions.ToImmutable());
        }

        public static bool IsGenerated(SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var filePath = syntaxTree.FilePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            var text = syntaxTree.GetText(cancellationToken).ToString();
            return text.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase);
        }

        public IEnumerable<INamedTypeSymbol> GetExplicitlyThrownExceptions(
            ISymbol symbol,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            var exceptions = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                var syntax = syntaxReference.GetSyntax(cancellationToken);
                if (IsGenerated(syntax.SyntaxTree, cancellationToken)) continue;

                var owner = GetBehaviorOwnerSyntax(syntax);
                if (owner is null) continue;

                var semanticModel = compilation.GetSemanticModel(owner.SyntaxTree);

                foreach (var throwExpression in owner.DescendantNodes(static node => !IsNestedFunctionLike(node))
                             .OfType<ThrowExpressionSyntax>())
                    if (TryGetExceptionType(semanticModel, throwExpression.Expression, cancellationToken,
                            out var exceptionType))
                        exceptions.Add(exceptionType);

                foreach (var throwStatement in owner.DescendantNodes(static node => !IsNestedFunctionLike(node))
                             .OfType<ThrowStatementSyntax>())
                {
                    if (throwStatement.Expression is null) continue;

                    if (TryGetExceptionType(semanticModel, throwStatement.Expression, cancellationToken,
                            out var exceptionType))
                        exceptions.Add(exceptionType);
                }

                foreach (var invocation in owner.DescendantNodes(static node => !IsNestedFunctionLike(node))
                             .OfType<InvocationExpressionSyntax>())
                {
                    if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
                        continue;
                    if (!method.IsStatic || !method.Name.StartsWith("ThrowIf", StringComparison.Ordinal)) continue;
                    if (method.ContainingType is null || !InheritsFromException(method.ContainingType)) continue;

                    exceptions.Add(method.ContainingType);
                }
            }

            return exceptions;
        }

        private static bool TryGetExceptionType(
            SemanticModel semanticModel,
            ExpressionSyntax expression,
            CancellationToken cancellationToken,
            out INamedTypeSymbol exceptionType)
        {
            exceptionType = null!;

            var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type as INamedTypeSymbol;
            if (type is null || !InheritsFromException(type)) return false;

            exceptionType = type;
            return true;
        }

        private static bool InheritsFromException(INamedTypeSymbol type)
        {
            for (var current = type; current is not null; current = current.BaseType)
                if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Exception")
                    return true;

            return false;
        }

        private static SyntaxNode? GetBehaviorOwnerSyntax(SyntaxNode syntax)
        {
            return syntax switch
            {
                BaseMethodDeclarationSyntax method => method,
                PropertyDeclarationSyntax property => property,
                IndexerDeclarationSyntax indexer => indexer,
                EventDeclarationSyntax @event => @event,
                VariableDeclaratorSyntax variableDeclarator => variableDeclarator.Parent?.Parent,
                _ => null
            };
        }

        private static bool IsNestedFunctionLike(SyntaxNode node)
        {
            return node is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax;
        }

        private static DocumentationInfo Empty { get; } = new(
            false,
            false,
            false,
            false,
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
            ImmutableHashSet<INamedTypeSymbol>.Empty.WithComparer(SymbolEqualityComparer.Default));
    }
}

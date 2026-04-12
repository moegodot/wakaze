using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Kawayi.Wakaze.Generator;

/// <summary>
/// Generates project-level schema registration classes from schema definition metadata.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SchemaRegistrationGenerator : IIncrementalGenerator
{
    private const string Category = "Usage";
    private const string RegisterSchemaAttributeMetadataName = "Kawayi.Wakaze.Abstractions.RegisterSchemaAttribute";
    private const string ProjectToAttributeMetadataName = "Kawayi.Wakaze.Abstractions.ProjectToAttribute";
    private const string SchemaDefinitionMetadataName = "Kawayi.Wakaze.Abstractions.ISchemaDefinition`2";
    private const string TypedObjectMetadataName = "Kawayi.Wakaze.Abstractions.ITypedObject";
    private const string GeneratedNamespace = "Kawayi.Wakaze.Generated";

    internal static readonly DiagnosticDescriptor RegisterSchemaMustImplementSchemaDefinition = new(
        id: "WG0001",
        title: "RegisterSchema type must implement ISchemaDefinition",
        messageFormat: "Type '{0}' is marked with RegisterSchemaAttribute but does not implement ISchemaDefinition<TFamily, TScheme>",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ProjectToMethodMustBeOnRegisteredSchema = new(
        id: "WG0002",
        title: "ProjectTo method must be declared on a registered schema",
        messageFormat: "Method '{0}' is marked with ProjectToAttribute but its containing type is not marked with RegisterSchemaAttribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ProjectToTargetMustImplementSchemaDefinition = new(
        id: "WG0003",
        title: "ProjectTo target must implement ISchemaDefinition",
        messageFormat: "ProjectTo target type '{0}' does not implement ISchemaDefinition<TFamily, TScheme>",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ProjectToMethodMustBeStatic = new(
        id: "WG0004",
        title: "ProjectTo method must be static",
        messageFormat: "ProjectTo method '{0}' must be static",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ProjectToMethodMustHaveOneParameter = new(
        id: "WG0005",
        title: "ProjectTo method must have exactly one parameter",
        messageFormat: "ProjectTo method '{0}' must declare exactly one parameter",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ProjectToMethodMustNotReturnVoid = new(
        id: "WG0006",
        title: "ProjectTo method must return a projected object",
        messageFormat: "ProjectTo method '{0}' must not return void",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ProjectToParameterMustImplementTypedObject = new(
        id: "WG0007",
        title: "ProjectTo parameter must implement ITypedObject",
        messageFormat: "ProjectTo method '{0}' parameter type '{1}' must implement ITypedObject",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ProjectToReturnTypeMustImplementTypedObject = new(
        id: "WG0008",
        title: "ProjectTo return type must implement ITypedObject",
        messageFormat: "ProjectTo method '{0}' return type '{1}' must implement ITypedObject",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ProjectToMethodMustBeAccessible = new(
        id: "WG0009",
        title: "ProjectTo method must be accessible",
        messageFormat: "ProjectTo method '{0}' must be public, internal, or protected internal to be callable from generated registration code",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var schemaCandidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            RegisterSchemaAttributeMetadataName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (attributeContext, _) => (INamedTypeSymbol)attributeContext.TargetSymbol);

        var projectorCandidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            ProjectToAttributeMetadataName,
            static (node, _) => node is MethodDeclarationSyntax,
            static (attributeContext, _) => (IMethodSymbol)attributeContext.TargetSymbol);

        var compilationAndInputs = context.CompilationProvider
            .Combine(schemaCandidates.Collect())
            .Combine(projectorCandidates.Collect());

        context.RegisterSourceOutput(compilationAndInputs, static (sourceContext, input) =>
        {
            var compilation = input.Left.Left;
            var schemaCandidatesArray = input.Left.Right;
            var projectorCandidatesArray = input.Right;

            Execute(sourceContext, compilation, schemaCandidatesArray, projectorCandidatesArray);
        });
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> schemaCandidates,
        ImmutableArray<IMethodSymbol> projectorCandidates)
    {
        var schemaDefinitionInterface = compilation.GetTypeByMetadataName(SchemaDefinitionMetadataName);
        var typedObjectInterface = compilation.GetTypeByMetadataName(TypedObjectMetadataName);

        if (schemaDefinitionInterface is null || typedObjectInterface is null)
        {
            return;
        }

        var distinctSchemaCandidates = schemaCandidates
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<INamedTypeSymbol>()
            .ToImmutableArray();
        var distinctProjectorCandidates = projectorCandidates
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<IMethodSymbol>()
            .ToImmutableArray();

        var registerSchemaTypes = new HashSet<INamedTypeSymbol>(distinctSchemaCandidates, SymbolEqualityComparer.Default);
        var validSchemas = new Dictionary<INamedTypeSymbol, SchemaDefinitionModel>(SymbolEqualityComparer.Default);

        foreach (var schemaType in distinctSchemaCandidates.OrderBy(static symbol => symbol.ToDisplayString(), StringComparer.Ordinal))
        {
            if (!TryGetSchemaDefinitionImplementation(schemaType, schemaDefinitionInterface, out var schemaInterface))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RegisterSchemaMustImplementSchemaDefinition,
                    schemaType.Locations.FirstOrDefault(),
                    schemaType.ToDisplayString()));
                continue;
            }

            validSchemas[schemaType] = new SchemaDefinitionModel(
                schemaType,
                (INamedTypeSymbol)schemaInterface.TypeArguments[0],
                (INamedTypeSymbol)schemaInterface.TypeArguments[1]);
        }

        var validProjectors = new List<ProjectorModel>();

        foreach (var projectorMethod in distinctProjectorCandidates.OrderBy(static symbol => symbol.ToDisplayString(), StringComparer.Ordinal))
        {
            if (!registerSchemaTypes.Contains(projectorMethod.ContainingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ProjectToMethodMustBeOnRegisteredSchema,
                    projectorMethod.Locations.FirstOrDefault(),
                    projectorMethod.ToDisplayString()));
                continue;
            }

            if (!validSchemas.TryGetValue(projectorMethod.ContainingType, out var sourceSchema))
            {
                continue;
            }

            if (!TryGetProjectToTarget(projectorMethod, out var targetSchemaType) ||
                targetSchemaType is null ||
                !TryGetSchemaDefinitionImplementation(targetSchemaType, schemaDefinitionInterface, out var targetSchemaInterface))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ProjectToTargetMustImplementSchemaDefinition,
                    projectorMethod.Locations.FirstOrDefault(),
                    targetSchemaType?.ToDisplayString() ?? "<unknown>"));
                continue;
            }

            if (!projectorMethod.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ProjectToMethodMustBeStatic,
                    projectorMethod.Locations.FirstOrDefault(),
                    projectorMethod.ToDisplayString()));
                continue;
            }

            if (projectorMethod.Parameters.Length != 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ProjectToMethodMustHaveOneParameter,
                    projectorMethod.Locations.FirstOrDefault(),
                    projectorMethod.ToDisplayString()));
                continue;
            }

            if (projectorMethod.ReturnsVoid)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ProjectToMethodMustNotReturnVoid,
                    projectorMethod.Locations.FirstOrDefault(),
                    projectorMethod.ToDisplayString()));
                continue;
            }

            if (!ImplementsInterface(projectorMethod.Parameters[0].Type, typedObjectInterface))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ProjectToParameterMustImplementTypedObject,
                    projectorMethod.Parameters[0].Locations.FirstOrDefault() ?? projectorMethod.Locations.FirstOrDefault(),
                    projectorMethod.ToDisplayString(),
                    projectorMethod.Parameters[0].Type.ToDisplayString()));
                continue;
            }

            if (!ImplementsInterface(projectorMethod.ReturnType, typedObjectInterface))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ProjectToReturnTypeMustImplementTypedObject,
                    projectorMethod.Locations.FirstOrDefault(),
                    projectorMethod.ToDisplayString(),
                    projectorMethod.ReturnType.ToDisplayString()));
                continue;
            }

            if (!IsMethodAccessibleFromRegistrar(projectorMethod))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ProjectToMethodMustBeAccessible,
                    projectorMethod.Locations.FirstOrDefault(),
                    projectorMethod.ToDisplayString()));
                continue;
            }

            validProjectors.Add(new ProjectorModel(
                projectorMethod,
                sourceSchema,
                new SchemaDefinitionModel(
                    targetSchemaType,
                    (INamedTypeSymbol)targetSchemaInterface.TypeArguments[0],
                    (INamedTypeSymbol)targetSchemaInterface.TypeArguments[1])));
        }

        if (validSchemas.Count == 0)
        {
            return;
        }

        var source = GenerateRegistrarSource(
            assemblyName: compilation.AssemblyName ?? "Generated",
            schemas: validSchemas.Values.OrderBy(static item => item.SchemaType.ToDisplayString(), StringComparer.Ordinal).ToImmutableArray(),
            projectors: validProjectors.OrderBy(static item => item.Method.ToDisplayString(), StringComparer.Ordinal).ToImmutableArray());

        var hintName = SanitizeIdentifier(compilation.AssemblyName ?? "Generated") + ".SchemaRegistration.g.cs";
        context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
    }

    private static bool TryGetSchemaDefinitionImplementation(
        INamedTypeSymbol type,
        INamedTypeSymbol schemaDefinitionInterface,
        out INamedTypeSymbol implementation)
    {
        implementation = null!;

        foreach (var candidate in type.AllInterfaces)
        {
            if (candidate.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, schemaDefinitionInterface))
            {
                implementation = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetProjectToTarget(IMethodSymbol method, out INamedTypeSymbol? targetSchemaType)
    {
        targetSchemaType = null;

        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != ProjectToAttributeMetadataName)
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length != 1)
            {
                return false;
            }

            targetSchemaType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
            return targetSchemaType is not null;
        }

        return false;
    }

    private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol interfaceType)
    {
        if (type is INamedTypeSymbol namedType &&
            SymbolEqualityComparer.Default.Equals(namedType, interfaceType))
        {
            return true;
        }

        foreach (var candidate in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, interfaceType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMethodAccessibleFromRegistrar(IMethodSymbol method)
    {
        return method.DeclaredAccessibility == Accessibility.Public ||
               method.DeclaredAccessibility == Accessibility.Internal ||
               method.DeclaredAccessibility == Accessibility.ProtectedOrInternal;
    }

    private static string GenerateRegistrarSource(
        string assemblyName,
        ImmutableArray<SchemaDefinitionModel> schemas,
        ImmutableArray<ProjectorModel> projectors)
    {
        var className = SanitizeIdentifier(assemblyName) + "SchemaRegistration";
        var builder = new StringBuilder();

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.Append("namespace ").Append(GeneratedNamespace).AppendLine(";");
        builder.AppendLine();
        builder.Append("internal sealed partial class ").Append(className)
            .AppendLine(" : global::Kawayi.Wakaze.Abstractions.ISchemaRegistration");
        builder.AppendLine("{");

        for (var index = 0; index < projectors.Length; index++)
        {
            AppendProjectorWrapper(builder, projectors[index], index);
            builder.AppendLine();
        }

        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public void Register(");
        builder.AppendLine("        global::Kawayi.Wakaze.Abstractions.SchemaCompatibilityGraph compatibility,");
        builder.AppendLine("        global::Kawayi.Wakaze.Abstractions.SchemaProjectorRegistry projector)");
        builder.AppendLine("    {");

        foreach (var schema in schemas)
        {
            builder.Append("        projector.RegisterSchema<")
                .Append(GetTypeName(schema.SchemaType)).Append(", ")
                .Append(GetTypeName(schema.FamilyType)).Append(", ")
                .Append(GetTypeName(schema.SchemeType)).AppendLine(">();");
        }

        if (schemas.Length != 0 && projectors.Length != 0)
        {
            builder.AppendLine();
        }

        foreach (var schema in schemas)
        {
            builder.Append("        compatibility.Register<")
                .Append(GetTypeName(schema.SchemaType)).Append(", ")
                .Append(GetTypeName(schema.FamilyType)).Append(", ")
                .Append(GetTypeName(schema.SchemeType)).AppendLine(">();");
        }

        if (schemas.Length != 0 && projectors.Length != 0)
        {
            builder.AppendLine();
        }

        for (var index = 0; index < projectors.Length; index++)
        {
            var projector = projectors[index];
            builder.Append("        projector.Register<")
                .Append(GetTypeName(projector.SourceSchema.SchemaType)).Append(", ")
                .Append(GetTypeName(projector.SourceSchema.FamilyType)).Append(", ")
                .Append(GetTypeName(projector.SourceSchema.SchemeType)).Append(", ")
                .Append(GetTypeName(projector.TargetSchema.SchemaType)).Append(", ")
                .Append(GetTypeName(projector.TargetSchema.FamilyType)).Append(", ")
                .Append(GetTypeName(projector.TargetSchema.SchemeType)).Append(">(Projector")
                .Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine(");");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static void AppendProjectorWrapper(StringBuilder builder, ProjectorModel projector, int index)
    {
        var parameterType = GetTypeName(projector.Method.Parameters[0].Type);
        var returnType = GetTypeName(projector.Method.ReturnType);
        var containingType = GetTypeName(projector.Method.ContainingType);

        builder.Append("    private static global::Kawayi.Wakaze.Abstractions.ITypedObject Projector")
            .Append(index.ToString(CultureInfo.InvariantCulture))
            .AppendLine("(global::Kawayi.Wakaze.Abstractions.ITypedObject source)");
        builder.AppendLine("    {");
        builder.Append("        return (global::Kawayi.Wakaze.Abstractions.ITypedObject)")
            .Append(containingType).Append('.').Append(projector.Method.Name).Append("((")
            .Append(parameterType).Append(")source);").AppendLine();
        builder.AppendLine("    }");
    }

    private static string GetTypeName(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
            }
        }

        if (builder.Length == 0)
        {
            builder.Append("Generated");
        }

        if (!char.IsLetter(builder[0]) && builder[0] != '_')
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    private sealed class SchemaDefinitionModel
    {
        public SchemaDefinitionModel(
            INamedTypeSymbol schemaType,
            INamedTypeSymbol familyType,
            INamedTypeSymbol schemeType)
        {
            SchemaType = schemaType;
            FamilyType = familyType;
            SchemeType = schemeType;
        }

        public INamedTypeSymbol SchemaType { get; }

        public INamedTypeSymbol FamilyType { get; }

        public INamedTypeSymbol SchemeType { get; }
    }

    private sealed class ProjectorModel
    {
        public ProjectorModel(
            IMethodSymbol method,
            SchemaDefinitionModel sourceSchema,
            SchemaDefinitionModel targetSchema)
        {
            Method = method;
            SourceSchema = sourceSchema;
            TargetSchema = targetSchema;
        }

        public IMethodSymbol Method { get; }

        public SchemaDefinitionModel SourceSchema { get; }

        public SchemaDefinitionModel TargetSchema { get; }
    }
}

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cspp.Results.Analyzers;

/// <summary>
/// Reports <c>Result&lt;T&gt;</c> where <c>T</c> is or derives from <c>Failure</c>. Such a type cannot tell its success
/// value apart from its failure, because both cases convert from a <c>Failure</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResultOfFailureAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CSPP0001";

    private const string ResultMetadataName = "Cspp.Results.Result`1";
    private const string FailureMetadataName = "Cspp.Results.Failure";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Result<T> value type must not be a Failure",
        messageFormat: "'{0}' cannot be the value type of Result<T> because it is or derives from Cspp.Results.Failure",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Result<T> uses Failure as its failure case, so a value type that is a Failure makes success and failure ambiguous.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? result = start.Compilation.GetTypeByMetadataName(ResultMetadataName);
            INamedTypeSymbol? failure = start.Compilation.GetTypeByMetadataName(FailureMetadataName);
            if (result is null || failure is null)
            {
                return;
            }

            start.RegisterSyntaxNodeAction(
                node => AnalyzeGenericName(node, result, failure),
                SyntaxKind.GenericName);
        });
    }

    private static void AnalyzeGenericName(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol result,
        INamedTypeSymbol failure)
    {
        var name = (GenericNameSyntax)context.Node;
        if (name.TypeArgumentList.Arguments.Count != 1)
        {
            return;
        }

        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken);
        if (symbolInfo.Symbol is not INamedTypeSymbol type
            || !SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, result))
        {
            return;
        }

        ITypeSymbol valueType = type.TypeArguments[0];
        if (IsOrDerivesFrom(valueType, failure))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                name.TypeArgumentList.Arguments[0].GetLocation(),
                valueType.ToDisplayString()));
        }
    }

    private static bool IsOrDerivesFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            return typeParameter.ConstraintTypes.Any(constraint => IsOrDerivesFrom(constraint, baseType));
        }

        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType))
            {
                return true;
            }
        }

        return false;
    }
}
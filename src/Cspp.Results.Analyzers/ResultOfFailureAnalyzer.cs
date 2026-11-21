using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cspp.Results.Analyzers;

/// <summary>
/// Reports <c>Result&lt;T&gt;</c> where <c>T</c> is or derives from <c>Failure</c>. Such a type cannot tell its success
/// value apart from its failure, because both cases convert from a <c>Failure</c>. Covers types written in source and
/// method calls whose type arguments make them return such a type, for example
/// <c>result.Map(_ =&gt; Failure.Unknown)</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResultOfFailureAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CSPP0001";

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
            if (ResultsTypes.From(start.Compilation) is not { } types)
            {
                return;
            }

            start.RegisterSyntaxNodeAction(node => AnalyzeGenericName(node, types), SyntaxKind.GenericName);
            start.RegisterOperationAction(operation => AnalyzeInvocation(operation, types), OperationKind.Invocation);
        });
    }

    private static void AnalyzeGenericName(SyntaxNodeAnalysisContext context, ResultsTypes types)
    {
        GenericNameSyntax name = (GenericNameSyntax)context.Node;
        if (name.TypeArgumentList.Arguments.Count != 1)
        {
            return;
        }

        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken);
        if (symbolInfo.Symbol is INamedTypeSymbol type && types.IsResultOfFailure(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                name.TypeArgumentList.Arguments[0].GetLocation(),
                type.TypeArguments[0].ToDisplayString()));
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, ResultsTypes types)
    {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;
        ITypeSymbol declaredReturnType = method.OriginalDefinition.ReturnType;

        // Only a return type that comes from a type argument (of the method, of its containing type, or of an extension
        // block), which is when substituting the type arguments changed it. A return type that substitution leaves alone
        // is written in source and reported there.
        if (!types.IsResultOfFailure(method.ReturnType)
            || SymbolEqualityComparer.Default.Equals(method.ReturnType, declaredReturnType))
        {
            return;
        }

        // A declaration in source that returns Result<TFailure> constrained to Failure is reported where it is written.
        // A declaration from metadata is never analyzed, so its calls are reported instead.
        if (types.IsResultOfFailure(declaredReturnType)
            && method.OriginalDefinition.Locations.Any(location => location.IsInSource))
        {
            return;
        }

        // With inferred type arguments, a Result<Failure> that flows in through the receiver or an argument was
        // reported where it was created, so the calls chained on it don't report the same mistake again. Type
        // arguments written out are the mistake itself, so they are always reported.
        SyntaxNode name = GetMethodName(invocation.Syntax);
        if (name is not GenericNameSyntax
            && ((invocation.Instance?.Type is { } instanceType && ContainsResultOfFailure(instanceType, types))
                || method.Parameters.Any(parameter => ContainsResultOfFailure(parameter.Type, types))))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            name is SimpleNameSyntax simpleName ? simpleName.Identifier.GetLocation() : name.GetLocation(),
            types.GetResultValueType(method.ReturnType)!.ToDisplayString()));
    }

    private static bool ContainsResultOfFailure(ITypeSymbol type, ResultsTypes types)
    {
        return type switch
        {
            INamedTypeSymbol named => types.IsResultOfFailure(named)
                || named.TypeArguments.Any(argument => ContainsResultOfFailure(argument, types)),
            IArrayTypeSymbol array => ContainsResultOfFailure(array.ElementType, types),
            _ => false
        };
    }

    // The node naming the called method, where the diagnostic goes. Calls without invocation syntax, such as LINQ query
    // clauses, use their whole syntax.
    private static SyntaxNode GetMethodName(SyntaxNode syntax)
    {
        return syntax is InvocationExpressionSyntax invocation
            ? invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
                _ => invocation.Expression
            }
            : syntax;
    }
}
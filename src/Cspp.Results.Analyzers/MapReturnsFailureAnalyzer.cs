using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cspp.Results.Analyzers;

/// <summary>
/// Reports a <c>Map</c> or <c>MapAsync</c> delegate that returns a <c>Failure</c> while the mapped value type is broader,
/// such as <c>object</c>. <c>Map</c> stores whatever the delegate returns as the success value, so the result would be a success
/// holding a failure. When the mapped value type is itself a <c>Failure</c>, <c>CSPP0001</c> reports the call instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MapReturnsFailureAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CSPP0002";

    private const string MapMethodName = "Map";
    private const string MapAsyncMethodName = "MapAsync";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Map must not return a Failure",
        messageFormat: "Map stores this '{0}' as a success value; to fail the step, use Bind and return the failure as a Result",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Map wraps the value its delegate returns in the success case. A Failure returned under a broader type such as object becomes a success holding a Failure, not a failure.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            if (ResultsTypes.From(start.Compilation) is not { ResultExtensions: { } extensions } types)
            {
                return;
            }

            start.RegisterOperationAction(
                operation => AnalyzeInvocation(operation, types, extensions),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        ResultsTypes types,
        INamedTypeSymbol extensions)
    {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;

        // A mapped value type that is itself a Failure is reported by CSPP0001.
        if (method.Name is not (MapMethodName or MapAsyncMethodName)
            || !IsDeclaredIn(method, extensions)
            || types.GetAwaitedResultValueType(method.ReturnType) is not { } valueType
            || types.IsFailure(valueType))
        {
            return;
        }

        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (argument.Value is not IDelegateCreationOperation delegateCreation)
            {
                continue;
            }

            foreach ((IOperation returned, ITypeSymbol type) in ReturnedFailures(delegateCreation.Target, types))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    returned.Syntax.GetLocation(),
                    type.ToDisplayString()));
            }
        }
    }

    private static bool IsDeclaredIn(IMethodSymbol method, INamedTypeSymbol type)
    {
        // Extension block members are declared in a type nested inside the static class.
        for (INamedTypeSymbol? current = method.ContainingType; current is not null; current = current.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, type))
            {
                return true;
            }
        }

        return false;
    }

    // Each returned Failure with its type. A method group has no type of its own, so it takes its method's return type.
    private static IEnumerable<(IOperation Returned, ITypeSymbol Type)> ReturnedFailures(
        IOperation target,
        ResultsTypes types)
    {
        switch (target)
        {
            case IAnonymousFunctionOperation lambda:
                return ReturnedValues(lambda.Body)
                    .SelectMany(value => FailureValues(value, types))
                    .Select(value => (value, value.Type!));
            case IMethodReferenceOperation methodReference when types.IsFailure(methodReference.Method.ReturnType):
                return [(methodReference, methodReference.Method.ReturnType)];
            default:
                return [];
        }
    }

    // The values of the return statements in a lambda body, skipping nested lambdas and local functions, whose returns
    // belong to them.
    private static IEnumerable<IOperation> ReturnedValues(IOperation operation)
    {
        foreach (IOperation child in operation.ChildOperations)
        {
            if (child is IAnonymousFunctionOperation or ILocalFunctionOperation)
            {
                continue;
            }

            if (child is IReturnOperation { ReturnedValue: { } value } && child.Kind == OperationKind.Return)
            {
                yield return value;
                continue;
            }

            foreach (IOperation nested in ReturnedValues(child))
            {
                yield return nested;
            }
        }
    }

    // The parts of a returned value that can each become the returned object: the value itself, or the branches of a
    // conditional, null-coalescing or switch expression, seen through casts that keep the same object.
    private static IEnumerable<IOperation> FailureValues(IOperation value, ResultsTypes types)
    {
        switch (value)
        {
            case IConversionOperation { Conversion.IsUserDefined: false } conversion:
                return FailureValues(conversion.Operand, types);
            case IConditionalOperation conditional:
                // A conditional expression, unlike an if statement, always has a false branch.
                return FailureValues(conditional.WhenTrue, types).Concat(FailureValues(conditional.WhenFalse!, types));
            case ICoalesceOperation coalesce:
                return FailureValues(coalesce.Value, types).Concat(FailureValues(coalesce.WhenNull, types));
            case ISwitchExpressionOperation switchExpression:
                return switchExpression.Arms.SelectMany(arm => FailureValues(arm.Value, types));
            default:
                return value.Type is not null && types.IsFailure(value.Type) ? [value] : [];
        }
    }
}
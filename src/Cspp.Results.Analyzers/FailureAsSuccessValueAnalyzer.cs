using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cspp.Results.Analyzers;

/// <summary>
/// Reports a <c>Failure</c> that becomes the success value of a <c>Result&lt;T&gt;</c> whose <c>T</c> is broader, such
/// as <c>object</c>: <c>Result&lt;object&gt; result = (object)Failure.Unknown;</c> or
/// <c>new Result&lt;object&gt;((object)failure)</c>. The cast hides the failure from the conversion, so the result is a
/// success holding a failure. Only values that are a <c>Failure</c> where the conversion is written are seen; one that
/// reaches it typed as <c>object</c> or as a type parameter is not.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FailureAsSuccessValueAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The ID of the diagnostic this analyzer reports, <c>CSPP0003</c>.
    /// </summary>
    public const string DiagnosticId = "CSPP0003";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "A Failure must not become a success value",
        messageFormat: "This '{0}' becomes the success value of '{1}'; convert the Failure itself, without a cast, to make a failed result",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A Failure cast to a broader type such as object converts to the success case of Result<T>, so the result is a success holding a Failure, not a failure.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
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

            start.RegisterOperationAction(operation => AnalyzeConversion(operation, types), OperationKind.Conversion);
            start.RegisterOperationAction(
                operation => AnalyzeObjectCreation(operation, types),
                OperationKind.ObjectCreation);
        });
    }

    // An implicit or explicit conversion to Result<T>, which calls the constructor of the case it converts to.
    private static void AnalyzeConversion(OperationAnalysisContext context, ResultsTypes types)
    {
        IConversionOperation conversion = (IConversionOperation)context.Operation;
        if (conversion.Conversion.MethodSymbol is IMethodSymbol constructor)
        {
            AnalyzeSuccessValue(context, types, constructor, conversion.Operand);
        }
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context, ResultsTypes types)
    {
        IObjectCreationOperation creation = (IObjectCreationOperation)context.Operation;
        if (creation.Constructor is { } constructor && creation.Arguments.Length == 1)
        {
            AnalyzeSuccessValue(context, types, constructor, creation.Arguments[0].Value);
        }
    }

    // Reports the failures in value when constructor is the success constructor of Result<T>, Result(T value). When T is
    // itself a Failure, CSPP0001 reports the type instead.
    private static void AnalyzeSuccessValue(
        OperationAnalysisContext context,
        ResultsTypes types,
        IMethodSymbol constructor,
        IOperation value)
    {
        if (constructor.MethodKind != MethodKind.Constructor
            || types.GetResultValueType(constructor.ContainingType) is not { } valueType
            || types.IsFailure(valueType)
            || !TakesValue(constructor))
        {
            return;
        }

        foreach (IOperation failure in FailureValues.Find(value, types))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                failure.Syntax.GetLocation(),
                failure.Type!.ToDisplayString(),
                constructor.ContainingType.ToDisplayString()));
        }
    }

    // Whether the constructor is Result(T value), the success case, rather than Result(Failure failure). Both take one
    // argument; the parameterless constructor of the struct never gets here, because it has no value to look at.
    private static bool TakesValue(IMethodSymbol constructor)
    {
        return constructor.OriginalDefinition.Parameters[0].Type is ITypeParameterSymbol;
    }
}
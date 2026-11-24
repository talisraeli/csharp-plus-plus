using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cspp.Results.Analyzers;

/// <summary>
/// Reports <c>Result&lt;T&gt;</c> where <c>T</c> is or derives from <c>Failure</c>. Such a type cannot tell its success
/// value apart from its failure, because both cases convert from a <c>Failure</c>. Covers types written in source, and
/// method calls, property reads and method groups whose type arguments make them return such a type, or a <c>Task</c>
/// or <c>ValueTask</c> of one, for example <c>result.Map(_ =&gt; Failure.Unknown)</c> or
/// <c>result.MapAsync(_ =&gt; GetFailureAsync())</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResultOfFailureAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The ID of the diagnostic this analyzer reports, <c>CSPP0001</c>.
    /// </summary>
    public const string DiagnosticId = "CSPP0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Result<T> value type must not be a Failure",
        messageFormat: "'{0}' cannot be the value type of Result<T> because it is or derives from Cspp.Results.Failure",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Result<T> uses Failure as its failure case, so a value type that is a Failure makes success and failure ambiguous.");

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

            start.RegisterSyntaxNodeAction(node => AnalyzeGenericName(node, types), SyntaxKind.GenericName);
            start.RegisterOperationAction(operation => AnalyzeInvocation(operation, types), OperationKind.Invocation);
            start.RegisterOperationAction(
                operation => AnalyzePropertyReference(operation, types),
                OperationKind.PropertyReference);
            start.RegisterOperationAction(
                operation => AnalyzeMethodReference(operation, types),
                OperationKind.MethodReference);
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

        AnalyzeMember(
            context,
            types,
            method,
            method.ReturnType,
            method.OriginalDefinition.ReturnType,
            invocation.Instance,
            method.Parameters,
            GetMemberName(invocation.Syntax));
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context, ResultsTypes types)
    {
        IPropertyReferenceOperation reference = (IPropertyReferenceOperation)context.Operation;
        IPropertySymbol property = reference.Property;

        AnalyzeMember(
            context,
            types,
            property,
            property.Type,
            property.OriginalDefinition.Type,
            reference.Instance,
            property.Parameters,
            GetMemberName(reference.Syntax));
    }

    // A method group, such as Wrap<Failure> turned into a delegate. Only type arguments written out are reported: inferred
    // ones come from the delegate type the group converts to, which already carries the Result<Failure> and is reported
    // where it is written or created. Calling the delegate is skipped too, because its type carries the Result<Failure>.
    private static void AnalyzeMethodReference(OperationAnalysisContext context, ResultsTypes types)
    {
        IMethodReferenceOperation reference = (IMethodReferenceOperation)context.Operation;
        SyntaxNode name = GetMemberName(reference.Syntax);
        if (name is not GenericNameSyntax)
        {
            return;
        }

        IMethodSymbol method = reference.Method;
        AnalyzeMember(
            context,
            types,
            method,
            method.ReturnType,
            method.OriginalDefinition.ReturnType,
            reference.Instance,
            method.Parameters,
            name);
    }

    // Reports a member whose type, the return type of a method or the type of a property, becomes a Result<Failure> (or
    // a task of one) through its type arguments.
    private static void AnalyzeMember(
        OperationAnalysisContext context,
        ResultsTypes types,
        ISymbol member,
        ITypeSymbol type,
        ITypeSymbol declaredType,
        IOperation? instance,
        ImmutableArray<IParameterSymbol> parameters,
        SyntaxNode name)
    {
        // Only a type that comes from a type argument (of a method, of its containing type, or of an extension block),
        // which is when substituting the type arguments changed it. A type that substitution leaves alone is written in
        // source and reported there. A task of a result counts, because awaiting it gives the result.
        if (!types.IsAwaitedResultOfFailure(type) || SymbolEqualityComparer.Default.Equals(type, declaredType))
        {
            return;
        }

        // A declaration in hand-written source that returns Result<TFailure> constrained to Failure is reported where it
        // is written. Generated code and metadata are never analyzed, so uses of their declarations are reported instead.
        if (types.IsAwaitedResultOfFailure(declaredType)
            && member.OriginalDefinition.Locations.Any(location => location.SourceTree is { } tree
                && !GeneratedCode.IsGenerated(tree, context.Options, context.CancellationToken)))
        {
            return;
        }

        // With inferred type arguments, a Result<Failure> that flows in through the receiver or an argument was
        // reported where it was created, so the uses chained on it don't report the same mistake again. Type
        // arguments written out are the mistake itself, so they are always reported.
        if (name is not GenericNameSyntax
            && ((instance?.Type is { } instanceType && ContainsResultOfFailure(instanceType, types))
                || parameters.Any(parameter => ContainsResultOfFailure(parameter.Type, types))))
        {
            return;
        }

        // A type argument written out that itself contains a Result<Failure>, such as Task.FromResult<Result<Failure>>,
        // is reported where it is written.
        if (name is GenericNameSyntax { TypeArgumentList.Arguments: var typeArguments }
            && typeArguments.Any(argument => ContainsResultOfFailure(
                context.Operation.SemanticModel!.GetTypeInfo(argument, context.CancellationToken).Type,
                types)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            name is SimpleNameSyntax simpleName ? simpleName.Identifier.GetLocation() : name.GetLocation(),
            types.GetAwaitedResultValueType(type)!.ToDisplayString()));
    }

    private static bool ContainsResultOfFailure(ITypeSymbol? type, ResultsTypes types)
    {
        return type switch
        {
            INamedTypeSymbol named => types.IsResultOfFailure(named)
                || named.TypeArguments.Any(argument => ContainsResultOfFailure(argument, types)),
            IArrayTypeSymbol array => ContainsResultOfFailure(array.ElementType, types),
            _ => false
        };
    }

    // The node naming the member, where the diagnostic goes. Uses without a name of their own, such as LINQ query clauses,
    // use their whole syntax.
    private static SyntaxNode GetMemberName(SyntaxNode syntax)
    {
        return syntax switch
        {
            InvocationExpressionSyntax invocation => GetMemberName(invocation.Expression),
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
            _ => syntax
        };
    }
}
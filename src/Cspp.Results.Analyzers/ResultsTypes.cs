using Microsoft.CodeAnalysis;

namespace Cspp.Results.Analyzers;

/// <summary>
/// The Cspp.Results types a compilation references, and the checks the analyzers make against them.
/// </summary>
internal sealed class ResultsTypes
{
    private const string ResultMetadataName = "Cspp.Results.Result`1";
    private const string FailureMetadataName = "Cspp.Results.Failure";
    private const string ResultExtensionsMetadataName = "Cspp.Results.ResultExtensions";
    private const string TaskMetadataName = "System.Threading.Tasks.Task`1";
    private const string ValueTaskMetadataName = "System.Threading.Tasks.ValueTask`1";

    private readonly INamedTypeSymbol? task;
    private readonly INamedTypeSymbol? valueTask;

    private ResultsTypes(
        INamedTypeSymbol result,
        INamedTypeSymbol failure,
        INamedTypeSymbol? resultExtensions,
        INamedTypeSymbol? task,
        INamedTypeSymbol? valueTask)
    {
        Result = result;
        Failure = failure;
        ResultExtensions = resultExtensions;
        this.task = task;
        this.valueTask = valueTask;
    }

    /// <summary>Gets <c>Result&lt;T&gt;</c>.</summary>
    public INamedTypeSymbol Result { get; }

    /// <summary>Gets <c>Failure</c>.</summary>
    public INamedTypeSymbol Failure { get; }

    /// <summary>Gets <c>ResultExtensions</c>, or <see langword="null" /> if the compilation doesn't reference it.</summary>
    public INamedTypeSymbol? ResultExtensions { get; }

    /// <summary>
    /// Resolves the Cspp.Results types of <paramref name="compilation" />.
    /// </summary>
    /// <param name="compilation">The compilation to look in.</param>
    /// <returns>
    /// The resolved types, or <see langword="null" /> if the compilation doesn't reference <c>Result&lt;T&gt;</c> and
    /// <c>Failure</c>.
    /// </returns>
    public static ResultsTypes? From(Compilation compilation)
    {
        INamedTypeSymbol? result = compilation.GetTypeByMetadataName(ResultMetadataName);
        INamedTypeSymbol? failure = compilation.GetTypeByMetadataName(FailureMetadataName);

        return result is null || failure is null
            ? null
            : new ResultsTypes(
                result,
                failure,
                compilation.GetTypeByMetadataName(ResultExtensionsMetadataName),
                compilation.GetTypeByMetadataName(TaskMetadataName),
                compilation.GetTypeByMetadataName(ValueTaskMetadataName));
    }

    /// <summary>
    /// Determines whether <paramref name="type" /> is <c>Failure</c>, derives from it, or is a type parameter
    /// constrained to it.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true" /> if <paramref name="type" /> is a failure type.</returns>
    public bool IsFailure(ITypeSymbol type)
    {
        return type.IsOrDerivesFrom(Failure);
    }

    /// <summary>
    /// Gets the value type <c>T</c> of <paramref name="type" /> if it is a <c>Result&lt;T&gt;</c>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>T</c>, or <see langword="null" /> if <paramref name="type" /> is not a <c>Result&lt;T&gt;</c>.</returns>
    public ITypeSymbol? GetResultValueType(ITypeSymbol type)
    {
        return type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, Result)
            ? named.TypeArguments[0]
            : null;
    }

    /// <summary>
    /// Gets the value type <c>T</c> of <paramref name="type" /> if it is a <c>Result&lt;T&gt;</c>, or a
    /// <c>Task&lt;Result&lt;T&gt;&gt;</c> or <c>ValueTask&lt;Result&lt;T&gt;&gt;</c> that completes with one.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>T</c>, or <see langword="null" /> if <paramref name="type" /> is none of these.</returns>
    public ITypeSymbol? GetAwaitedResultValueType(ITypeSymbol type)
    {
        return GetResultValueType(type)
            ?? (type is INamedTypeSymbol { TypeArguments.Length: 1 } named && IsTask(named.OriginalDefinition)
                ? GetResultValueType(named.TypeArguments[0])
                : null);
    }

    /// <summary>
    /// Determines whether <paramref name="type" /> is a <c>Result&lt;T&gt;</c>, or a task that completes with one, whose
    /// <c>T</c> is a failure type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true" /> if <paramref name="type" /> is or completes with a result of a failure type.</returns>
    public bool IsAwaitedResultOfFailure(ITypeSymbol type)
    {
        return GetAwaitedResultValueType(type) is { } valueType && IsFailure(valueType);
    }

    /// <summary>
    /// Determines whether <paramref name="type" /> is a <c>Result&lt;T&gt;</c> whose <c>T</c> is a failure type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true" /> if <paramref name="type" /> is a result of a failure type.</returns>
    public bool IsResultOfFailure(ITypeSymbol type)
    {
        return GetResultValueType(type) is { } valueType && IsFailure(valueType);
    }

    private bool IsTask(INamedTypeSymbol definition)
    {
        return SymbolEqualityComparer.Default.Equals(definition, task)
            || SymbolEqualityComparer.Default.Equals(definition, valueTask);
    }
}
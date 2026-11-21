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

    private ResultsTypes(INamedTypeSymbol result, INamedTypeSymbol failure, INamedTypeSymbol? resultExtensions)
    {
        Result = result;
        Failure = failure;
        ResultExtensions = resultExtensions;
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
            : new ResultsTypes(result, failure, compilation.GetTypeByMetadataName(ResultExtensionsMetadataName));
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
    /// Determines whether <paramref name="type" /> is a <c>Result&lt;T&gt;</c> whose <c>T</c> is a failure type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true" /> if <paramref name="type" /> is a result of a failure type.</returns>
    public bool IsResultOfFailure(ITypeSymbol type)
    {
        return GetResultValueType(type) is { } valueType && IsFailure(valueType);
    }
}
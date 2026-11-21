using Microsoft.CodeAnalysis;

namespace Cspp.Results.Analyzers;

internal static class TypeSymbolExtensions
{
    /// <summary>
    /// Determines whether <paramref name="type" /> is <paramref name="baseType" /> or derives from it. A type parameter
    /// counts when one of its constraints does.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="baseType">The base type to look for.</param>
    /// <returns><see langword="true" /> if <paramref name="type" /> is or derives from <paramref name="baseType" />.</returns>
    public static bool IsOrDerivesFrom(this ITypeSymbol type, INamedTypeSymbol baseType)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            return typeParameter.ConstraintTypes.Any(constraint => constraint.IsOrDerivesFrom(baseType));
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Cspp.Results.Analyzers;

/// <summary>
/// Finds the parts of a value that are statically a <c>Failure</c>.
/// </summary>
internal static class FailureValues
{
    /// <summary>
    /// Gets the parts of <paramref name="value" /> that can each become the resulting object and are statically a
    /// <c>Failure</c>: the value itself, or the branches of a conditional, null-coalescing or switch expression, seen
    /// through casts that keep the same object.
    /// </summary>
    /// <param name="value">The value to look into.</param>
    /// <param name="types">The Cspp.Results types of the compilation.</param>
    /// <returns>Each part whose type is a <c>Failure</c>.</returns>
    public static IEnumerable<IOperation> Find(IOperation value, ResultsTypes types)
    {
        switch (value)
        {
            // A cast with no method behind it, such as (object)failure, keeps the same object. A user-defined or union
            // conversion calls a method that makes a new value, such as a Result holding the failure.
            case IConversionOperation { Conversion: { IsUserDefined: false, MethodSymbol: null } } conversion:
                return Find(conversion.Operand, types);
            case IConditionalOperation conditional:
                // A conditional expression, unlike an if statement, always has a false branch.
                return Find(conditional.WhenTrue, types).Concat(Find(conditional.WhenFalse!, types));
            case ICoalesceOperation coalesce:
                return Find(coalesce.Value, types).Concat(Find(coalesce.WhenNull, types));
            case ISwitchExpressionOperation switchExpression:
                return switchExpression.Arms.SelectMany(arm => Find(arm.Value, types));
            default:
                return value.Type is not null && types.IsFailure(value.Type) ? [value] : [];
        }
    }
}
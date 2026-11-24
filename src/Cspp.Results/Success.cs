namespace Cspp.Results;

/// <summary>
/// Marks the success case of the non-generic <see cref="Result" />. It has a single instance, <c>Result.Success</c>.
/// </summary>
public sealed class Success
{
    internal static readonly Success Instance = new();

    private Success()
    {
    }
}
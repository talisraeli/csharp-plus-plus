namespace Cspp.Results;

internal enum ResultType : byte
{
    /// <summary>The result holds a <see cref="Results.Failure"/>. Zero value, so <c>default</c> results are failures.</summary>
    Failure = 0,

    /// <summary>The result holds a success case.</summary>
    Success = 1,
}
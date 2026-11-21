namespace Cspp.Results;

internal enum ResultType : byte
{
    /// <summary>The result holds a failure.</summary>
    Failure = 0,

    /// <summary>The result holds a success case.</summary>
    Success = 1
}
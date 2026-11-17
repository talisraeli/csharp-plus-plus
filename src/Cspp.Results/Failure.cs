namespace Cspp.Results;

public readonly record struct Failure
{
    [Obsolete("Use Error(string code, string message) constructor instead.", true)]
    public Failure()
    {
        throw new InvalidOperationException("None error provided.");
    }

    public Failure(Error error)
    {
        Error = error;
    }

    public Error Error { get; init; }
}
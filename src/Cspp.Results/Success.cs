namespace Cspp.Results;

public readonly record struct Success;

public readonly record struct Success<T>
{
    [Obsolete("Use Success<T>(T value) constructor instead.", true)]
    public Success()
    {
        throw new InvalidOperationException("None value provided.");
    }

    public Success(T value)
    {
        Value = value;
    }

    public T Value { get; init; }
}
using System.Runtime.CompilerServices;

namespace Cspp.Results;

[Union]
public readonly record struct Result : IUnion
{
    public static readonly Success Success = default;
    public static readonly Failure Failure = new(Error.Unknown);
    private readonly Error? error;
    private readonly ResultType type;

    public Result(Success success)
    {
        type = ResultType.Success;
    }

    public Result(Failure failure)
    {
        error = failure.Error;
        type = ResultType.Failure;
    }

    public object? Value => type == ResultType.Success ? default(Success) : new Failure(Error);

    public bool HasValue => true;

    private Error Error => error ?? Error.Unknown;

    public bool TryGetValue(out Success success)
    {
        success = default;
        return type == ResultType.Success;
    }

    public bool TryGetValue(out Failure failure)
    {
        failure = type == ResultType.Failure ? new Failure(Error) : default;
        return type == ResultType.Failure;
    }
}

[Union]
public readonly record struct Result<T> : IUnion
{
    private readonly T value;
    private readonly Error? error;
    private readonly ResultType type;

    public Result(Success<T> success)
    {
        value = success.Value;
        type = ResultType.Success;
    }

    public Result(Failure failure)
    {
        value = default!;
        error = failure.Error;
        type = ResultType.Failure;
    }

    public object? Value => type == ResultType.Success ? new Success<T>(value) : new Failure(Error);

    public bool HasValue => true;

    private Error Error => error ?? Error.Unknown;

    public bool TryGetValue(out Success<T> success)
    {
        success = type == ResultType.Success ? new Success<T>(value) : default;
        return type == ResultType.Success;
    }

    public bool TryGetValue(out Failure failure)
    {
        failure = type == ResultType.Failure ? new Failure(Error) : default;
        return type == ResultType.Failure;
    }
}
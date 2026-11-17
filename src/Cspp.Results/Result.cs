using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cspp.Results;

[Union]
public readonly record struct Result : IUnion
{
    private readonly Failure? failure;
    private readonly ResultType type;

    public Result(Success success)
    {
        type = ResultType.Success;
    }

    public Result(Failure failure)
    {
        this.failure = failure;
        type = ResultType.Failure;
    }

    public object? Value => IsSuccess ? Success.Instance : Failure;

    public bool HasValue => true;

    public bool IsSuccess => type == ResultType.Success;

    public bool IsFailure => type == ResultType.Failure;

    internal Failure Failure => failure ?? Failure.Uninitialized;

    public bool TryGetValue([NotNullWhen(true)] out Success? success)
    {
        success = IsSuccess ? Success.Instance : null;
        return IsSuccess;
    }

    public bool TryGetValue([NotNullWhen(true)] out Failure? failure)
    {
        failure = IsFailure ? Failure : null;
        return IsFailure;
    }
}

[Union]
public readonly record struct Result<T> : IUnion
{
    private readonly T value;
    private readonly Result state;

    public Result(T value)
    {
        this.value = value;
        state = Success.Instance;
    }

    public Result(Failure failure)
    {
        value = default!;
        state = failure;
    }

    public object? Value => IsSuccess ? value : state.Failure;

    public bool HasValue => true;

    public bool IsSuccess => state.IsSuccess;

    public bool IsFailure => state.IsFailure;

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = IsSuccess ? this.value : default;
        return IsSuccess;
    }

    public bool TryGetValue([NotNullWhen(true)] out Failure? failure)
    {
        return state.TryGetValue(out failure);
    }
}
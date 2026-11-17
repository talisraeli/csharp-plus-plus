using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cspp.Results;

[Union]
public readonly record struct Result : IUnion
{
    private readonly Error? error;
    private readonly ResultType type;

    public Result(Success success)
    {
        type = ResultType.Success;
    }

    public Result(Error error)
    {
        this.error = error;
        type = ResultType.Failure;
    }

    public object? Value => IsSuccess ? Success.Instance : Error;

    public bool HasValue => true;

    public bool IsSuccess => type == ResultType.Success;

    public bool IsFailure => type == ResultType.Failure;

    internal Error Error => error ?? Error.Uninitialized;

    public bool TryGetValue([NotNullWhen(true)] out Success? success)
    {
        success = IsSuccess ? Success.Instance : null;
        return IsSuccess;
    }

    public bool TryGetValue([NotNullWhen(true)] out Error? error)
    {
        error = IsFailure ? Error : null;
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

    public Result(Error error)
    {
        value = default!;
        state = error;
    }

    public object? Value => IsSuccess ? value : state.Error;

    public bool HasValue => true;

    public bool IsSuccess => state.IsSuccess;

    public bool IsFailure => state.IsFailure;

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = IsSuccess ? this.value : default;
        return IsSuccess;
    }

    public bool TryGetValue([NotNullWhen(true)] out Error? error)
    {
        return state.TryGetValue(out error);
    }
}